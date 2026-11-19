// Light from the game's own lights (Ui/DepthShading.Lamps.cs): up to eight point lights near the camera, read from the
// client's own lists, brightening what's round them per pixel. The light pass gives how much brighter a pixel gets (0
// none), blended as destination times (1 + source), so a surface keeps its own colour and is lit by the light's; the
// shadow pass before it, how much of the game's own light is left where something blocks a lamp (a character on the
// floor under it), multiplied in. Each light reaches as far as the game lets it, fading smoothly to nothing there; a
// surface lit from behind gets none. The two lamps nearest you have shadows, from cube shadow maps of the game's own
// world drawn from each lamp (Ui/DepthShading.LampShadows.cs): the first build marched through the depth buffer
// instead, which only knows what's on screen, and walls and furniture threw shadows that made no sense (seen in the
// game). Places come back from the depth as in shading.ps.hlsl. Compiled by Shaders/build.cmd.
sampler depthBuffer : register(s0);
samplerCUBE lampMap0 : register(s1);
samplerCUBE lampMap1 : register(s2);

float4 projection : register(c0);  // M33, M43, M11, M22
float4 settings : register(c1);    // strength, shadows (1 or 0), light count, mode (0 light, 1 shadow)
float4 lamps[16] : register(c2);   // per light: view-space place (xyz) and reach (m); colour (rgb) and casts shadows
float4 viewToWorld[3] : register(c18); // rows: a direction in view space to the device's world
float4 shadowed : register(c21);   // which lights have the two cube maps (-1 for none)

float ViewZ(float d) {
    return projection.y / (d - projection.x);
}

float3 ViewPlace(float2 uv, float z) {
    return float3((uv.x * 2 - 1) * z / projection.z, (1 - uv.y * 2) * z / projection.w, z);
}

// How much of a place a lamp sees (0 to 1): its distance from the lamp against the nearest the cube map saw that way,
// five samples spread a little across the way, so the edge is soft
float Seen(samplerCUBE map, float3 fromLamp, float distance, float reach, float facing) {
    float3 way = fromLamp / distance;
    float3 side = normalize(abs(way.y) < 0.9 ? cross(way, float3(0, 1, 0)) : cross(way, float3(1, 0, 0)));
    float3 other = cross(way, side);
    float spread = 0.012;
    // more allowance where the light grazes a surface (the ceiling's beams by a lamp), or the surface shadows itself
    // in stripes (seen in the game)
    float bias = 0.04 + distance * 0.03 + (1 - facing) * 0.25;
    float seen = 0;
    seen += step(distance - bias, texCUBElod(map, float4(way, 0)).r * reach);
    seen += step(distance - bias, texCUBElod(map, float4(way + side * spread, 0)).r * reach);
    seen += step(distance - bias, texCUBElod(map, float4(way - side * spread, 0)).r * reach);
    seen += step(distance - bias, texCUBElod(map, float4(way + other * spread, 0)).r * reach);
    seen += step(distance - bias, texCUBElod(map, float4(way - other * spread, 0)).r * reach);
    return seen / 5;
}

float4 main(float2 uv : TEXCOORD0) : COLOR {
    float d = tex2Dlod(depthBuffer, float4(uv, 0, 0)).r;

    // the normal from the place's screen derivatives, worked out before any branch (they need every pixel round)
    float z = ViewZ(min(d, 0.9999));
    float3 p = ViewPlace(uv, z);
    float3 n = normalize(cross(ddx(p), ddy(p)));
    float nothing = settings.w > 0.5 ? 1 : 0;

    if (d >= 0.99999) {
        return float4(nothing, nothing, nothing, 1);
    }

    if (dot(n, p) > 0) {
        n = -n;
    }

    float3 added = 0;
    float dark = 0;
    float firstSeen = 1;

    for (int i = 0; i < 8; i++) {
        if (i >= settings.z) {
            break;
        }

        float4 place = lamps[i * 2];
        float3 colour = lamps[i * 2 + 1].rgb;
        float3 toLight = place.xyz - p;
        float distance = length(toLight);

        if (distance < place.w && distance > 0.01) {
            toLight /= distance;
            float facing = saturate(dot(n, toLight));
            float reach = 1 - distance / place.w;
            float amount = facing * reach * reach;
            float seen = 1;

            if (settings.y > 0.5 && (i == shadowed.x || i == shadowed.y)) {
                // the way from the lamp to the place, in the device's world, as the cube map is laid out
                float3 v = p - place.xyz;
                float3 fromLamp = v.x * viewToWorld[0].xyz + v.y * viewToWorld[1].xyz + v.z * viewToWorld[2].xyz;
                seen = i == shadowed.x ? Seen(lampMap0, fromLamp, distance, place.w, facing)
                    : Seen(lampMap1, fromLamp, distance, place.w, facing);

                if (i == shadowed.x) {
                    firstSeen = seen;
                }

                // a shadow takes away some of the game's own light from it, which it gives everything in reach in
                // full: so by how near the light is, not the fade added light has
                if (lamps[i * 2 + 1].w > 0.5) {
                    dark = max(dark, (1 - seen) * facing * saturate(1.5 - 1.5 * distance / place.w));
                }
            }

            added += colour * amount * seen;
        }
    }

    // a test run's `depthview 4`: what the first shadowed lamp sees, white, and what's hidden from it, black
    if (settings.w > 1.5) {
        return float4(firstSeen, firstSeen, firstSeen, 1);
    }

    // the shadow pass: what's left of the light, multiplied in; the light pass: how much brighter
    if (settings.w > 0.5) {
        float left = 1 - dark * 0.55 * saturate(settings.x);
        return float4(left, left, left, 1);
    }

    return float4(added * settings.x, 1);
}
