using System;
using System.Numerics;
using ImGuiNET;

namespace DaraletPlayer.Ui {
    /// <summary>
    /// Draws shapes laid out on a 16 by 16 grid into a square of any size on a draw list, in one colour: TabIcons'
    /// pictures. Line widths are in grid units too, at least a pixel.
    /// </summary>
    internal readonly struct IconPen {
        private const float Grid = 16;

        private readonly ImDrawListPtr drawList;
        private readonly Vector2 origin;
        private readonly float unit;
        private readonly uint color;

        public IconPen(ImDrawListPtr drawList, Vector2 centre, float size, uint color) {
            this.drawList = drawList;
            unit = size / Grid;
            origin = centre - new Vector2(size / 2);
            this.color = color;
        }

        private Vector2 At(float x, float y) {
            return origin + new Vector2(x, y) * unit;
        }

        private Vector2 At(Vector2 point) {
            return At(point.X, point.Y);
        }

        private float Width(float units) {
            return Math.Max(1, units * unit);
        }

        public void Line(float x1, float y1, float x2, float y2, float width) {
            drawList.AddLine(At(x1, y1), At(x2, y2), color, Width(width));
        }

        public void Disc(float x, float y, float radius) {
            drawList.AddCircleFilled(At(x, y), radius * unit, color, 0);
        }

        /// <summary>A circle's outline, radius to the middle of its line.</summary>
        public void Ring(float x, float y, float radius, float width) {
            drawList.AddCircle(At(x, y), radius * unit, color, 0, Width(width));
        }

        public void Box(float x1, float y1, float x2, float y2, float rounding, ImDrawFlags corners) {
            drawList.AddRectFilled(At(x1, y1), At(x2, y2), color, rounding * unit, corners);
        }

        /// <summary>A rectangle's outline, its line centred on the edges.</summary>
        public void Frame(float x1, float y1, float x2, float y2, float rounding, float width) {
            drawList.AddRect(At(x1, y1), At(x2, y2), color, rounding * unit, ImDrawFlags.RoundCornersAll, Width(width));
        }

        /// <summary>A filled convex shape through these points, in order.</summary>
        public void Fill(params Vector2[] points) {
            var placed = new Vector2[points.Length];

            for (var i = 0; i < points.Length; i++) {
                placed[i] = At(points[i]);
            }

            drawList.AddConvexPolyFilled(ref placed[0], placed.Length, color);
        }

        /// <summary>The upper half of a disc, its flat side at y (shoulders, a dome).</summary>
        public void TopHalfDisc(float x, float y, float radius) {
            drawList.PathArcTo(At(x, y), radius * unit, (float)Math.PI, (float)(2 * Math.PI), 16);
            drawList.PathFillConvex(color);
        }

        /// <summary>An arc's line, between two angles in radians (0 to the right, going clockwise on screen).</summary>
        public void Arc(float x, float y, float radius, double from, double to, float width) {
            drawList.PathArcTo(At(x, y), radius * unit, (float)from, (float)to, 16);
            drawList.PathStroke(color, ImDrawFlags.None, Width(width));
        }

        /// <summary>An ellipse's outline.</summary>
        public void Ellipse(float x, float y, float radiusX, float radiusY, float width) {
            const int Segments = 32;
            var points = new Vector2[Segments];

            for (var i = 0; i < Segments; i++) {
                var angle = i * 2 * Math.PI / Segments;
                points[i] = At(x + radiusX * (float)Math.Cos(angle), y + radiusY * (float)Math.Sin(angle));
            }

            drawList.AddPolyline(ref points[0], Segments, color, ImDrawFlags.Closed, Width(width));
        }
    }
}
