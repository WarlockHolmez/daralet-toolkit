using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace DaraletPlayer.Ui {
    /// <summary>
    /// Lamp shadows from cube shadow maps (the user's call, November 2026: the first lamp shadows, marched through the
    /// depth buffer, "look awkward, of the character and especially whatever is making that shadow on the wall": a march
    /// on screen only knows what's on screen, and guesses how thick it is). The two lamps nearest you that cast shadows
    /// get a cube map each, six 90-degree faces of 512 by 512, 32-bit float, every face the distance from the lamp to the
    /// nearest thing that way, over its reach (Shaders/lampshadow.vs.hlsl, lampshadow.ps.hlsl), drawn from the game's own
    /// world draws (ShadowCasters, the same the sun's maps use: the hooks are in while either wants them) placed within
    /// the lamp's reach and a margin, leaves cut out. Shaders/lamps.ps.hlsl compares a place's distance from the lamp with
    /// the map's that way. One depth surface is shared by every face. All in the default pool, let go before a reset.
    /// </summary>
    internal sealed partial class DepthShading {
        private const int LampMapSize = 512;
        private const int LampsWithShadows = 2;

        /// <summary>Draws placed this far past a lamp's reach are still made into its map: a big thing reaches in.</summary>
        private const float LampCasterMargin = 15;

        /// <summary>
        /// A lamp's light sits inside its own lantern, whose frame and glass then hid everything from it (seen in the game:
        /// the first cube maps left every pixel in a lamp's shadow). So nothing nearer the light than this counts (the
        /// faces' near plane), and a draw placed within LampOwnModel of it (the lantern itself) isn't made into its map.
        /// </summary>
        private const float LampNear = 0.35f;

        private const float LampOwnModel = 0.75f;

        private static readonly (CubeMapFace Face, Vector3 Way, Vector3 Up)[] CubeFaces = {
            (CubeMapFace.PositiveX, new Vector3(1, 0, 0), new Vector3(0, 1, 0)),
            (CubeMapFace.NegativeX, new Vector3(-1, 0, 0), new Vector3(0, 1, 0)),
            (CubeMapFace.PositiveY, new Vector3(0, 1, 0), new Vector3(0, 0, -1)),
            (CubeMapFace.NegativeY, new Vector3(0, -1, 0), new Vector3(0, 0, 1)),
            (CubeMapFace.PositiveZ, new Vector3(0, 0, 1), new Vector3(0, 1, 0)),
            (CubeMapFace.NegativeZ, new Vector3(0, 0, -1), new Vector3(0, 1, 0)),
        };

        private readonly CubeTexture[] lampCubes = new CubeTexture[LampsWithShadows];
        private Surface lampCubeDepth;
        private IntPtr lampShadowVertexShader;
        private IntPtr lampShadowPixelShader;
        private DateTime nextLampShadowReport = DateTime.MinValue;

        /// <summary>
        /// The cube maps of up to two of the chosen lights (their places in the device's world and reaches given), the
        /// ones that cast shadows nearest you; which of the chosen each map is for, -1 for none.
        /// </summary>
        private float[] DrawLampShadows(Device device, IList<(Vector3 Place, float Reach, bool Casts)> chosen,
            Vector3 you) {
            var which = new[] { -1f, -1f };
            var picked = chosen.Select((l, i) => (Light: l, Index: i))
                .Where(c => c.Light.Casts && c.Light.Reach >= 2)
                .OrderBy(c => (c.Light.Place - you).LengthSq())
                .Take(LampsWithShadows)
                .ToList();

            if (picked.Count == 0 || ShadowCasters.Count == 0) {
                return which;
            }

            EnsureLampCubes(device);
            var oldTarget = device.GetRenderTarget(0);
            var oldDepth = device.DepthStencilSurface;
            var made = 0;

            try {
                PrepareLampShadowDrawing(device);

                for (var k = 0; k < picked.Count; k++) {
                    made += DrawCube(device, lampCubes[k], picked[k].Light.Place, picked[k].Light.Reach);
                    which[k] = picked[k].Index;
                }
            }
            finally {
                device.SetRenderTarget(0, oldTarget);
                device.DepthStencilSurface = oldDepth;
                oldTarget?.Dispose();
                oldDepth?.Dispose();
            }

            if (DateTime.UtcNow >= nextLampShadowReport) {
                nextLampShadowReport = DateTime.UtcNow.AddSeconds(30);
                PluginCore.LogFile($"[lamps] {picked.Count} lamps with cube shadow maps, {made} draws made into them "
                    + $"this frame, of {ShadowCasters.Count} noted");
            }

            return which;
        }

        private void PrepareLampShadowDrawing(Device device) {
            device.DepthStencilSurface = lampCubeDepth;
            device.Viewport = new Viewport { X = 0, Y = 0, Width = LampMapSize, Height = LampMapSize, MaxZ = 1 };
            var state = device.RenderState;
            state.ZBufferEnable = true;
            state.ZBufferWriteEnable = true;
            state.ZBufferFunction = Compare.LessEqual;
            state.CullMode = Cull.None;
            state.AlphaBlendEnable = false;
            state.AlphaTestEnable = false;
            state.FogEnable = false;
            state.Lighting = false;
            state.StencilEnable = false;
            state.ScissorTestEnable = false;
            state.ColorWriteEnable = ColorWriteEnable.Red;
            device.SamplerState[0].MinFilter = TextureFilter.Point;
            device.SamplerState[0].MagFilter = TextureFilter.Point;
            device.SamplerState[0].MipFilter = TextureFilter.Point;
            device.SamplerState[0].AddressU = TextureAddress.Wrap;
            device.SamplerState[0].AddressV = TextureAddress.Wrap;
            DeviceCalls.UseShaders(device, lampShadowVertexShader, lampShadowPixelShader);
        }

        /// <summary>One lamp's six faces, from the draws placed within its reach; how many draws, over all six.</summary>
        private static unsafe int DrawCube(Device device, CubeTexture cube, Vector3 lamp, float reach) {
            var com = (IntPtr)device.UnmanagedComPointer;
            var projection = Matrix.PerspectiveFovLH((float)(Math.PI / 2), 1, LampNear, reach);
            var near = new List<ShadowDraw>();
            var limit = (reach + LampCasterMargin) * (reach + LampCasterMargin);

            for (var i = 0; i < ShadowCasters.Count; i++) {
                var draw = ShadowCasters.At(i);
                var w = draw.World;
                var dx = w[12] - lamp.X;
                var dy = w[13] - lamp.Y;
                var dz = w[14] - lamp.Z;

                var squared = dx * dx + dy * dy + dz * dz;

                if (squared <= limit && squared > LampOwnModel * LampOwnModel) {
                    near.Add(draw);
                }
            }

            var constants = new float[36];
            var cutOut = new float[4];
            var toFace = new float[16];
            var made = 0;

            foreach (var (face, way, up) in CubeFaces) {
                using (var surface = cube.GetCubeMapSurface(face, 0)) {
                    device.SetRenderTarget(0, surface);
                    device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, System.Drawing.Color.White, 1, 0);
                    var viewProjection = Matrix.LookAtLH(lamp, lamp + way, up) * projection;

                    foreach (var draw in near) {
                        var w = draw.World;
                        Multiply(w, viewProjection, toFace);
                        Array.Copy(toFace, 0, constants, 0, 16);
                        Array.Copy(w, 0, constants, 16, 16);
                        constants[32] = lamp.X;
                        constants[33] = lamp.Y;
                        constants[34] = lamp.Z;
                        constants[35] = 1 / reach;
                        DeviceCalls.VertexConstants(device, 0, constants);
                        cutOut[0] = draw.AlphaTest && draw.Texture != IntPtr.Zero ? 1 : 0;
                        DeviceCalls.PixelConstants(device, 0, cutOut);
                        ShadowCasters.GameSetDeclaration(com, draw.Declaration);
                        ShadowCasters.GameSetStream(com, 0, draw.Stream, draw.Offset, 44);
                        ShadowCasters.GameSetIndices(com, draw.Indices);
                        ShadowCasters.GameSetTexture(com, 0, draw.Texture);
                        ShadowCasters.GameDrawIndexed(com, draw.Type, draw.BaseVertex, draw.MinIndex, draw.Vertices,
                            draw.StartIndex, draw.Count);
                        made++;
                    }
                }
            }

            return made;
        }

        private void EnsureLampCubes(Device device) {
            if (lampShadowVertexShader == IntPtr.Zero) {
                lampShadowVertexShader = DeviceCalls.VertexShader(device, "lampshadow.vs.cso");
                lampShadowPixelShader = DeviceCalls.PixelShader(device, "lampshadow.ps.cso");
            }

            if (lampCubeDepth != null) {
                return;
            }

            for (var k = 0; k < LampsWithShadows; k++) {
                lampCubes[k] = new CubeTexture(device, LampMapSize, 1, Usage.RenderTarget, Format.R32F, Pool.Default);
            }

            lampCubeDepth = device.CreateDepthStencilSurface(LampMapSize, LampMapSize, DepthFormat.D24S8,
                MultiSampleType.None, 0, true);
            PluginCore.LogFile($"[lamps] cube shadow maps made, {LampsWithShadows} of {LampMapSize} a face");
        }

        private void TearDownLampShadows() {
            for (var k = 0; k < LampsWithShadows; k++) {
                lampCubes[k]?.Dispose();
                lampCubes[k] = null;
            }

            lampCubeDepth?.Dispose();
            lampCubeDepth = null;
        }

        private void ReleaseLampShadowShaders() {
            Release(lampShadowVertexShader);
            Release(lampShadowPixelShader);
            lampShadowVertexShader = IntPtr.Zero;
            lampShadowPixelShader = IntPtr.Zero;
        }
    }
}
