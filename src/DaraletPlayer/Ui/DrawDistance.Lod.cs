using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DaraletPlayer.Feed;

namespace DaraletPlayer.Ui {
    /// <summary>
    /// Far scenery kept at its cheapest model (DrawDistanceOptions.LowDetailDistance), the user's answer to Detail's cost
    /// in frame rate (November 2026): raising the Degrade Distance keeps every object near and far at a finer model, while
    /// this only stops far ones vanishing. CPhysicsPart::UpdateViewerDistance (0x50EB00) and UpdateViewerDistance_
    /// (0x50EC70) ask GfxObjDegradeInfo::get_degrade (0x51EFC0, thiscall: the distance, then where to put the level and
    /// the mode) which of a part's models to draw, then skip the part when that model is null (`cmp [gfxobj + level*4],0`
    /// after the call): a tree's last level is an empty model, which is how the game hides it past a distance. Both calls
    /// are pointed here (AcClient.Hook, as SpellDrag); the game's answer stands unless its model is null and the part is
    /// within the distance wanted, when the nearest level down that has a model is drawn, with that level's mode
    /// (GfxObjInfo +4 in the degrade info's list at +0x3C, 20 bytes each). The level and mode are the part's own deg_level
    /// and deg_mode (+0x14, +0x18), so the part is the level's address less 0x14, and its models (gfxobj) at +0x20. Called
    /// for every part every frame, so it does nothing else.
    /// <para>
    /// The same hook once scaled the distance the game was asked about, for plainer models sooner (detail bands, model
    /// level distances, then a Detail falloff, November 2026), and that's gone: with this on, a tree whose only real
    /// model is its finest came back as it, so it changed nothing (CLAUDE.md). It's where a falloff to billboards would
    /// go. The game's own multiplier (deg_mul, 0x86731C, moved by its automatic degrades, 0x81FC78, else its Object
    /// Degrade preference, s_rUserSuppliedDegradeBias at 0x867318) applies as ever.
    /// </para>
    /// Untested in the game when written.
    /// </summary>
    internal sealed partial class DrawDistance {
        private const int PartLevel = 0x14;
        private const int PartModels = 0x20;
        private const int DegradeCount = 0x38;
        private const int DegradeList = 0x3C;
        private const int DegradeEntrySize = 20;
        private const int DegradeEntryMode = 4;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private unsafe delegate void GetDegradeCall(IntPtr info, float distance, int* level, int* mode);

        private static readonly List<AcClient.Hook> lodHooks = new List<AcClient.Hook>();

        // both kept for as long as the hooks are in, so they're never collected while the client can call them
        private static GetDegradeCall lodHook;
        private static GetDegradeCall gameGetDegrade;

        /// <summary>How far out, in meters, a part about to vanish keeps its cheapest model instead; 0 for none.</summary>
        private static volatile float lowDetailMeters;

        /// <summary>The hooks in while far scenery is wanted, out otherwise.</summary>
        private void ApplyLowDetail(int meters) {
            lowDetailMeters = meters;
            var wanted = meters > 0;

            if (wanted && lodHook == null) {
                HookGetDegrade();
            }
            else if (!wanted && lodHook != null) {
                UnhookGetDegrade();
            }
        }

        private static unsafe void HookGetDegrade() {
            var entry = (int)AcClient.Entrypoint.GfxObjDegradeInfo__get_degrade;
            gameGetDegrade = Marshal.GetDelegateForFunctionPointer<GetDegradeCall>((IntPtr)entry);
            lodHook = OnGetDegrade;

            foreach (var call in new[] {
                (int)AcClient.Call.GfxObjDegradeInfo__get_degrade_0x0050EC37,
                (int)AcClient.Call.GfxObjDegradeInfo__get_degrade_0x0050ECC4,
            }) {
                var hooked = new AcClient.Hook(entry, call);

                if (hooked.Setup(lodHook)) {
                    lodHooks.Add(hooked);
                }
                else {
                    PluginCore.LogFile($"[draw-distance] couldn't hook get_degrade at 0x{call:X8}");
                }
            }

            PluginCore.LogFile($"[draw-distance] far scenery kept at low detail ({lodHooks.Count} of 2 places)");
        }

        /// <summary>The game's calls put back. A hook left in after the plugin's gone would crash the game.</summary>
        private static void UnhookGetDegrade() {
            foreach (var hooked in lodHooks) {
                try {
                    hooked.Remove();
                }
                catch (Exception ex) {
                    PluginCore.LogFile("[draw-distance] " + ex);
                }
            }

            if (lodHooks.Count > 0) {
                PluginCore.LogFile("[draw-distance] far scenery left to the game again");
            }

            lodHooks.Clear();
            lodHook = null;
            lowDetailMeters = 0;
        }

        private static unsafe void OnGetDegrade(IntPtr info, float distance, int* level, int* mode) {
            gameGetDegrade(info, distance, level, mode);

            try {
                var meters = lowDetailMeters;

                if (meters <= 0 || info == IntPtr.Zero || RealDistance(level) > meters) {
                    return;
                }

                var models = *(IntPtr**)((byte*)level - PartLevel + PartModels);
                var count = *(int*)((byte*)info + DegradeCount);
                var chosen = *level;

                if (models == null || chosen <= 0 || chosen >= count || models[chosen] != IntPtr.Zero) {
                    return;
                }

                while (chosen > 0 && models[chosen] == IntPtr.Zero) {
                    chosen--;
                }

                if (models[chosen] == IntPtr.Zero) {
                    return;
                }

                var entries = *(byte**)((byte*)info + DegradeList);
                *level = chosen;
                *mode = *(int*)(entries + chosen * DegradeEntrySize + DegradeEntryMode);
            }
            catch {
                // nothing may escape into the game's drawing; the game's own answer stands
            }
        }

        /// <summary>
        /// The part's real distance from the camera, in meters: the distance get_degrade is given is that over the
        /// part's scale (UpdateViewerDistance's `fdiv [esi+2Ch]`, 0x50EC2B), so a big tree seemed nearer than it was.
        /// Both callers store the real one in the part's first field (CYpt, 0x50EB7E and 0x50EC7B) before asking.
        /// </summary>
        private static unsafe float RealDistance(int* level) {
            return Math.Abs(*(float*)((byte*)level - PartLevel));
        }

    }
}
