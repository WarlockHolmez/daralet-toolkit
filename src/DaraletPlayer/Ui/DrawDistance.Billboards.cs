using System;
using System.Collections.Generic;
using System.Linq;
using DaraletPlayer.Feed;
using Decal.Adapter;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using UtilityBelt.Service;

namespace DaraletPlayer.Ui {
    /// <summary>
    /// Billboards for the scenery past the real scenery's radius (billboards stage 3; the user's call, November 2026:
    /// real scenery to 4-5 landblocks, billboards beyond, out to the landscape's edge).
    /// <para>
    /// Which: every landblock in the landscape (within its radius of yours) outside the full-detail ring, its scenery
    /// worked out by Feed/SceneryLayout.cs at the density in use, a few blocks a frame (the dat readers are only safe on
    /// the game's thread, and a few hundred blocks at once would stall it). What: each object a pair of crossed upright
    /// pictures (BillboardModels), turned by its own rotation and sized by its scale, so they're the same from every
    /// side and can sit in one vertex buffer (managed, PositionTextured), made again only when you cross a landblock or
    /// the settings change, one draw a model.
    /// </para>
    /// <para>
    /// Drawn on Decal's RenderFrame, which comes after the game's 3D world and before its interface, so the world's depth
    /// buffer is there: billboards behind a hill are hidden by it. The game's camera is the device's View and Projection
    /// as it left them, the world in your landblock's meters with up the middle number (x, height, y), as the combat
    /// numbers read it (Feed/CombatProjection.cs); a frame where the device holds the camera with two axes swapped (some
    /// spell effects) draws none. The device's state is captured before and put back after (a StateBlock): depth test
    /// and write on, alpha test at half (the pictures' cut-out), no blending or culling, lit by the game's own sun and
    /// ambient (DrawDistance.Lighting.cs), the texture
    /// times that light. A failure is logged once and billboards stop for the session. `[billboards]` lines in log.txt.
    /// </para>
    /// Untested in the game when written.
    /// </summary>
    internal sealed partial class DrawDistance {
        private const int BlocksAFrame = 6;

        // CustomVertex.PositionNormalTextured.Format, which the compiler can't read without Microsoft.VisualC (a C++ const)
        private const VertexFormats BillboardFormat =
            VertexFormats.Position | VertexFormats.Normal | VertexFormats.Texture1;

        private readonly BillboardModels billboardModels = new BillboardModels();
        private readonly Dictionary<uint, List<PlacedScenery>> blockScenery = new Dictionary<uint, List<PlacedScenery>>();
        private readonly Dictionary<uint, List<byte>> blockHeights = new Dictionary<uint, List<byte>>();
        private readonly Queue<uint> blocksToWorkOut = new Queue<uint>();
        private List<uint> billboardBlocks = new List<uint>();
        private int billboardDensity = -1;
        private string billboardKey;
        private uint billboardHome;
        private bool billboardsToBuild;
        private VertexBuffer billboardBuffer;
        private readonly List<(Texture Texture, int Start, int Triangles)> billboardBatches =
            new List<(Texture, int, int)>();
        private StateBlock savedState;
        private bool billboardsFailed;
        private int billboardCount;

        /// <summary>Once a second from Apply: which blocks want billboards now, and whether that's changed.</summary>
        private void UpdateBillboards(bool wanted, int landscape) {
            if (!wanted || billboardsFailed) {
                if (billboardBuffer != null) {
                    ClearBillboards();
                    PluginCore.LogFile("[billboards] off");
                }

                billboardKey = null;
                return;
            }

            var home = unchecked((uint)CoreManager.Current.Actions.Landcell) >> 16;

            // indoors (a dungeon's cells) there's no landscape to stand billboards on
            if ((unchecked((uint)CoreManager.Current.Actions.Landcell) & 0xFFFF) >= 0x100) {
                if (billboardBuffer != null) {
                    ClearBillboards();
                }

                billboardKey = null;
                return;
            }

            var key = $"{home:X4} {patchedRadius} {landscape} {appliedDensity}";

            if (key == billboardKey) {
                return;
            }

            billboardKey = key;

            if (billboardDensity != appliedDensity) {
                blockScenery.Clear();
                billboardDensity = appliedDensity;
            }

            var hx = (int)(home >> 8);
            var hy = (int)(home & 0xFF);
            var blocks = new List<uint>();

            for (var dx = -landscape; dx <= landscape; dx++) {
                for (var dy = -landscape; dy <= landscape; dy++) {
                    var ring = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    var x = hx + dx;
                    var y = hy + dy;

                    if (ring <= patchedRadius || x < 0 || y < 0 || x > 0xFE || y > 0xFE) {
                        continue;
                    }

                    blocks.Add((uint)(x << 8 | y));
                }
            }

            billboardBlocks = blocks;
            billboardHome = home;
            blocksToWorkOut.Clear();

            foreach (var block in blocks.Where(b => !blockScenery.ContainsKey(b))) {
                blocksToWorkOut.Enqueue(block);
            }

            billboardsToBuild = true;
            PluginCore.LogFile($"[billboards] {blocks.Count} landblocks from ring {patchedRadius + 1} to {landscape}, "
                + $"{blocksToWorkOut.Count} to work out");
        }

        /// <summary>Every frame: a few more blocks worked out, the buffer made once they all are, and the billboards
        /// drawn.</summary>
        private void BillboardFrame() {
            if (billboardKey == null || billboardsFailed) {
                return;
            }

            try {
                var portal = UBService.PortalDat;
                var cells = UBService.CellDat;

                if (portal == null || cells == null) {
                    return;
                }

                region = region ?? portal.ReadFromDat<ACE.DatLoader.FileTypes.RegionDesc>(RegionId);

                for (var i = 0; i < BlocksAFrame && blocksToWorkOut.Count > 0; i++) {
                    var block = blocksToWorkOut.Dequeue();
                    var id = block << 16;
                    var land = cells.ReadFromDat<ACE.DatLoader.FileTypes.CellLandblock>(id | 0xFFFF);
                    blockHeights[block] = land?.Height.ToList();
                    blockScenery[block] = land == null ? new List<PlacedScenery>()
                        : SceneryLayout.Place(id, land.Terrain, land.Height, region.LandDefs.LandHeightTable, ScenesFor,
                            sceneId => ObjectsOf(portal, sceneId), appliedDensity, BuildingCells(cells, id));
                }

                var device = UBService.Huds.D3Ddevice;

                if (device == null) {
                    return;
                }

                if (blocksToWorkOut.Count == 0 && billboardsToBuild) {
                    BuildBillboards(device, portal);
                }

                DrawBillboards(device);
            }
            catch (Exception ex) {
                billboardsFailed = true;
                PluginCore.LogFile("[billboards] stopped for the session: " + ex);
            }
        }

        /// <summary>The crossed pictures of every far object, grouped by model, into one vertex buffer.</summary>
        private void BuildBillboards(Device device, ACE.DatLoader.DatDatabase portal) {
            billboardsToBuild = false;
            var byModel = new Dictionary<BillboardModel, List<CustomVertex.PositionNormalTextured>>();
            var hx = (int)(billboardHome >> 8);
            var hy = (int)(billboardHome & 0xFF);
            var objects = 0;

            foreach (var block in billboardBlocks) {
                if (!blockScenery.TryGetValue(block, out var placed)) {
                    continue;
                }

                var offsetX = ((int)(block >> 8) - hx) * SceneryLayout.BlockSize;
                var offsetY = ((int)(block & 0xFF) - hy) * SceneryLayout.BlockSize;

                // stood on the ground as the game draws this block, in less detail the further out it is: on the true
                // ground, trees on a hump floated over the flattened ridge the game draws
                var ring = Math.Max(Math.Abs((int)(block >> 8) - hx), Math.Abs((int)(block & 0xFF) - hy));
                var step = SceneryLayout.DetailStep(ring, patchedRadius);
                blockHeights.TryGetValue(block, out var heights);

                foreach (var p in placed) {
                    var model = billboardModels.Get(p.Model, portal, device);

                    if (model == null) {
                        continue;
                    }

                    if (!byModel.TryGetValue(model, out var vertices)) {
                        vertices = new List<CustomVertex.PositionNormalTextured>();
                        byModel[model] = vertices;
                    }

                    var z = heights == null || step == 1 ? p.Z : SceneryLayout.Ground(block << 16, heights,
                        region.LandDefs.LandHeightTable, p.X, p.Y, step).Z;
                    AddCrossedQuads(vertices, model, offsetX + p.X, offsetY + p.Y, z, p.Yaw, p.Scale);
                    objects++;
                }
            }

            ClearBillboards();
            var all = byModel.SelectMany(kv => kv.Value).ToArray();

            if (all.Length == 0) {
                PluginCore.LogFile("[billboards] nothing to draw");
                return;
            }

            billboardBuffer = new VertexBuffer(typeof(CustomVertex.PositionNormalTextured), all.Length, device,
                Usage.WriteOnly,
                BillboardFormat, Pool.Managed);
            billboardBuffer.SetData(all, 0, LockFlags.None);
            var start = 0;

            foreach (var kv in byModel) {
                billboardBatches.Add((kv.Key.Texture, start, kv.Value.Count / 3));
                start += kv.Value.Count;
            }

            billboardCount = objects;
            PluginCore.LogFile($"[billboards] {objects} billboards in {billboardBatches.Count} models, "
                + $"{all.Length / 3} triangles, {FreeMegabytes()} MB free");
        }

        /// <summary>
        /// Two upright pictures through the object's place at right angles, turned by its yaw: world x and y in your
        /// landblock's meters, z the ground; the device's world is (x, height, y).
        /// </summary>
        private static void AddCrossedQuads(List<CustomVertex.PositionNormalTextured> vertices, BillboardModel model,
            float x,
            float y, float z, float yaw, float scale) {
            var half = model.Width * scale / 2;
            var bottom = z + model.Bottom * scale;
            var top = bottom + model.Height * scale;
            var middle = model.Middle * scale;
            var cos = (float)Math.Cos(yaw);
            var sin = (float)Math.Sin(yaw);

            foreach (var (dx, dy) in new[] { (cos, sin), (-sin, cos) }) {
                var cx = x + dx * middle;
                var cy = y + dy * middle;
                var lx = cx - dx * half;
                var ly = cy - dy * half;
                var rx = cx + dx * half;
                var ry = cy + dy * half;
                var bl = new CustomVertex.PositionNormalTextured(lx, bottom, ly, 0, 1, 0, 0, 1);
                var br = new CustomVertex.PositionNormalTextured(rx, bottom, ry, 0, 1, 0, 1, 1);
                var tl = new CustomVertex.PositionNormalTextured(lx, top, ly, 0, 1, 0, 0, 0);
                var tr = new CustomVertex.PositionNormalTextured(rx, top, ry, 0, 1, 0, 1, 0);
                vertices.Add(bl);
                vertices.Add(tl);
                vertices.Add(tr);
                vertices.Add(bl);
                vertices.Add(tr);
                vertices.Add(br);
            }
        }

        private void DrawBillboards(Device device) {
            if (billboardBuffer == null || billboardBatches.Count == 0) {
                return;
            }

            // the frame the world's drawn in is the landblock you're in: one crossed since the buffer was made waits
            if (unchecked((uint)CoreManager.Current.Actions.Landcell) >> 16 != billboardHome) {
                return;
            }

            if (CameraSwapped(device)) {
                return;
            }

            savedState = savedState ?? new StateBlock(device, StateBlockType.All);
            LogDeviceLights(device);
            savedState.Capture();

            try {
                device.Transform.World = Matrix.Identity;
                device.VertexFormat = BillboardFormat;
                device.SetStreamSource(0, billboardBuffer, 0);
                device.RenderState.ZBufferEnable = true;
                device.RenderState.ZBufferWriteEnable = true;
                device.RenderState.AlphaTestEnable = true;
                device.RenderState.ReferenceAlpha = 128;
                device.RenderState.AlphaFunction = Compare.GreaterEqual;
                device.RenderState.AlphaBlendEnable = false;
                device.RenderState.CullMode = Cull.None;
                // the game's sun and ambient left as they are (DrawDistance.Lighting.cs), on a white material, normals up
                device.RenderState.Lighting = true;
                device.RenderState.ColorVertex = false;
                device.RenderState.SpecularEnable = false;
                LightLikeTheGame(device);
                device.Material = new Material {
                    DiffuseColor = new ColorValue(1f, 1f, 1f), AmbientColor = new ColorValue(1f, 1f, 1f),
                };
                device.TextureState[0].ColorOperation = TextureOperation.Modulate;
                device.TextureState[0].ColorArgument2 = TextureArgument.Diffuse;
                device.TextureState[0].ColorArgument1 = TextureArgument.TextureColor;
                device.TextureState[0].AlphaOperation = TextureOperation.SelectArg1;
                device.TextureState[0].AlphaArgument1 = TextureArgument.TextureColor;
                device.TextureState[1].ColorOperation = TextureOperation.Disable;
                device.SamplerState[0].MinFilter = TextureFilter.Linear;
                device.SamplerState[0].MagFilter = TextureFilter.Linear;
                device.SamplerState[0].MipFilter = TextureFilter.Linear;

                foreach (var batch in billboardBatches) {
                    device.SetTexture(0, batch.Texture);
                    device.DrawPrimitives(PrimitiveType.TriangleList, batch.Start, batch.Triangles);
                }
            }
            finally {
                savedState.Apply();
            }
        }

        /// <summary>Whether the device holds the camera with its second and third axes swapped (CombatProjection).</summary>
        private static bool CameraSwapped(Device device) {
            var m = device.Transform.View;
            var view = new[] {
                m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44,
            };
            var actions = CoreManager.Current.Actions;
            return CombatProjection.IsSwapped(view, (float)actions.LocationX, (float)actions.LocationY,
                (float)actions.LocationZ);
        }

        private void ClearBillboards() {
            billboardBuffer?.Dispose();
            billboardBuffer = null;
            billboardBatches.Clear();
            billboardCount = 0;
        }

        private void DisposeBillboards() {
            ClearBillboards();
            savedState?.Dispose();
            savedState = null;
            billboardModels.Dispose();
        }

        /// <summary>For a test run's results: how many billboards are up.</summary>
        public string BillboardState => billboardsFailed ? "billboards stopped (see log.txt)"
            : billboardKey == null ? "no billboards" : $"{billboardCount} billboards"
                + (blocksToWorkOut.Count > 0 ? $", {blocksToWorkOut.Count} blocks still to work out" : "");
    }
}
