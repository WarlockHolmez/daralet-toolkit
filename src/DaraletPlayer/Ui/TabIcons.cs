using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace DaraletPlayer.Ui {
    /// <summary>
    /// Tabs that show a picture instead of (or before) their name: the main window's, which crowded a narrow window as
    /// words, and Social's Find a group. The pictures are the plugin's own, drawn with ImGui's shapes in the text's colour
    /// at the font's size, so they follow the theme and the Text size and stay sharp. ImGui sizes a tab by its label, so
    /// the label is spaces as wide as the picture (and a fixed id after ###), and the picture goes over them.
    /// </summary>
    internal static class TabIcons {
        /// <summary>The picture's size, in font heights.</summary>
        private const float IconScale = 1.15f;

        /// <summary>Strings of spaces, by how many.</summary>
        private static readonly Dictionary<int, string> spaces = new Dictionary<int, string>();

        /// <summary>
        /// A tab showing only its picture, with its name (and, while tooltips are allowed, what it holds) in its tooltip.
        /// Returns true while it's the open tab; call EndTabItem then.
        /// </summary>
        public static bool TabItem(TabIcon icon, string name, string tip, bool select = false) {
            var size = ImGui.GetFontSize() * IconScale;
            var open = Widgets.TabItem(Spaces(size) + "###" + name, null, select);
            var hovered = ImGui.IsItemHovered();
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();

            // a tab squeezed narrower than its picture (a narrow window, big text) draws it smaller
            size = Math.Min(size, Math.Min(max.X - min.X - 2, max.Y - min.Y - 2));
            DrawInTab(icon, min, max, (min + max) / 2, size, open || hovered);
            NameTip(name, tip, hovered);
            return open;
        }

        /// <summary>A tab showing its picture before its name. Returns true while it's the open tab.</summary>
        public static bool TabItemWithName(TabIcon icon, string name, string tip, bool select = false) {
            var size = ImGui.GetFontSize() * IconScale;
            var gap = ImGui.GetStyle().ItemInnerSpacing.X;
            var open = Widgets.TabItem(Spaces(size + gap) + name + "###" + name, tip, select);
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();
            var centre = new Vector2(min.X + ImGui.GetStyle().FramePadding.X + size / 2, (min.Y + max.Y) / 2);

            DrawInTab(icon, min, max, centre, Math.Min(size, max.Y - min.Y - 2), true);
            return open;
        }

        /// <summary>
        /// The tooltip of a tab that shows no name: its name always, then what it holds while tooltips are allowed.
        /// </summary>
        private static void NameTip(string name, string tip, bool hovered) {
            if (!hovered) {
                return;
            }

            Widgets.BeginTooltip(always: true);
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 28);
            ImGui.TextUnformatted(name);

            if (!string.IsNullOrEmpty(tip) && !Widgets.TooltipsHidden()) {
                ImGui.Separator();
                ImGui.TextUnformatted(tip);
            }

            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }

        private static void DrawInTab(TabIcon icon, Vector2 min, Vector2 max, Vector2 centre, float size, bool lit) {
            if (size <= 2) {
                return;
            }

            // ImGui's own label colour; tabs not open or under the mouse a little fainter, so the open one stands out.
            // Fainter is a solid colour part way to the tab's, not the text see-through: a picture's pieces overlap (the
            // axe's handle through its blades), and see-through, each overlap showed brighter
            var color = lit ? ImGui.GetColorU32(ImGuiCol.Text) : ImGui.GetColorU32(Faint());
            var drawList = ImGui.GetWindowDrawList();
            drawList.PushClipRect(min, max, true);
            TabIconShapes.Draw(new IconPen(drawList, centre, size, color), icon);
            drawList.PopClipRect();
        }

        /// <summary>
        /// The text's colour 70% of the way from a tab's background (the tab colour over the window's), and solid.
        /// </summary>
        private static Vector4 Faint() {
            var colors = ImGui.GetStyle().Colors;
            var text = colors[(int)ImGuiCol.Text];
            var tab = colors[(int)ImGuiCol.Tab];
            var window = colors[(int)ImGuiCol.WindowBg];
            var under = Vector4.Lerp(window, tab, tab.W);
            var faint = Vector4.Lerp(under, text, 0.7f);
            return new Vector4(faint.X, faint.Y, faint.Z, text.W);
        }

        /// <summary>Spaces at least that wide in the font now.</summary>
        private static string Spaces(float width) {
            var space = Math.Max(1, ImGui.CalcTextSize(" ").X);
            var count = (int)Math.Ceiling(width / space);

            if (!spaces.TryGetValue(count, out var text)) {
                text = new string(' ', count);
                spaces[count] = text;
            }

            return text;
        }
    }
}
