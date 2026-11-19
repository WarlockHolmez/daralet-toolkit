using System;
using System.Numerics;
using ImGuiNET;

namespace DaraletPlayer.Ui {
    /// <summary>
    /// The tab pictures' shapes (TabIcons), each laid out on IconPen's 16 by 16 grid in one colour. Kept apart from the
    /// ImGui side so a throwaway tool can draw the same shapes to a picture to look at outside the game.
    /// </summary>
    internal static class TabIconShapes {
        /// <summary>Draws a picture with the pen, filling its square.</summary>
        public static void Draw(IconPen pen, TabIcon icon) {
            switch (icon) {
                case TabIcon.Gear:
                    Gear(pen);
                    break;
                case TabIcon.Globe:
                    Globe(pen);
                    break;
                case TabIcon.People:
                    People(pen);
                    break;
                case TabIcon.Axe:
                    Axe(pen);
                    break;
                case TabIcon.Person:
                    Person(pen);
                    break;
                case TabIcon.Quill:
                    Quill(pen);
                    break;
                case TabIcon.Backpack:
                    Backpack(pen);
                    break;
                case TabIcon.CoinPurse:
                    CoinPurse(pen);
                    break;
                case TabIcon.CrossedSwords:
                    CrossedSwords(pen);
                    break;
                case TabIcon.Medal:
                    Medal(pen);
                    break;
                case TabIcon.CaveEntrance:
                    CaveEntrance(pen);
                    break;
            }
        }

        private static void Gear(IconPen pen) {
            // a ring, its hole left empty, with eight teeth round it
            pen.Ring(8, 8, 3.9f, 2.9f);

            for (var i = 0; i < 8; i++) {
                var angle = i * Math.PI / 4;
                var (cos, sin) = ((float)Math.Cos(angle), (float)Math.Sin(angle));
                var (across, alongIn, alongOut, half) = (new Vector2(-sin, cos), 4.6f, 7.4f, 1.25f);
                var inner = new Vector2(8 + cos * alongIn, 8 + sin * alongIn);
                var outer = new Vector2(8 + cos * alongOut, 8 + sin * alongOut);
                pen.Fill(inner - across * (half + 0.3f), outer - across * half, outer + across * half,
                    inner + across * (half + 0.3f));
            }
        }

        private static void Globe(IconPen pen) {
            pen.Ellipse(8, 8, 6.6f, 6.6f, 1.3f);
            pen.Ellipse(8, 8, 2.8f, 6.6f, 1.1f);
            pen.Line(1.6f, 8, 14.4f, 8, 1.1f);
            pen.Line(2.6f, 4.6f, 13.4f, 4.6f, 1);
            pen.Line(2.6f, 11.4f, 13.4f, 11.4f, 1);
        }

        private static void People(IconPen pen) {
            // two behind, one in front
            pen.Disc(3.4f, 5.4f, 1.8f);
            pen.Disc(12.6f, 5.4f, 1.8f);
            pen.TopHalfDisc(3.2f, 12.4f, 3.1f);
            pen.TopHalfDisc(12.8f, 12.4f, 3.1f);
            pen.Disc(8, 6.2f, 2.5f);
            pen.TopHalfDisc(8, 15, 4.6f);
        }

        private static void Axe(IconPen pen) {
            // a double-bitted axe after the game's melee combat button (the user's call, November 2026; a sword and
            // shield before): the handle from the bottom left with a pommel, the head at the top right, a crescent
            // blade each side of it
            var along = new Vector2(0.7071f, -0.7071f);
            var across = new Vector2(0.7071f, 0.7071f);
            var head = new Vector2(10.2f, 5.8f);
            var butt = head - along * 11.2f;
            var top = head + along * 2.2f;
            pen.Line(butt.X, butt.Y, top.X, top.Y, 1.5f);
            pen.Disc(butt.X, butt.Y, 1.1f);

            foreach (var side in new[] { 1f, -1f }) {
                Vector2 At(float a, float b) => head + along * a + across * (b * side);
                pen.Fill(At(-1.1f, 0.5f), At(1.1f, 0.5f), At(3, 3.6f), At(1.6f, 4.6f), At(0, 4.9f), At(-1.6f, 4.6f),
                    At(-3, 3.6f));
            }
        }

        private static void Person(IconPen pen) {
            // a head and shoulders, a little gap between them
            pen.Disc(8, 5, 3.3f);
            pen.TopHalfDisc(8, 15.6f, 6.4f);
        }

        private static void Quill(IconPen pen) {
            // a feather leaning right, and its shaft down to the nib
            pen.Fill(new Vector2(4.8f, 11.2f), new Vector2(5, 8), new Vector2(7, 5), new Vector2(10.5f, 2.4f),
                new Vector2(15, 1), new Vector2(13.2f, 5), new Vector2(10.6f, 8.5f), new Vector2(7.6f, 10.6f));
            pen.Line(6, 10, 1.4f, 14.6f, 1.2f);
        }

        private static void Backpack(IconPen pen) {
            // after the game toolbar's backpack, picked from many drafts (the user's call, November 2026): a carrying
            // loop, the rounded flap high over the top, a pocket along the bottom, and a pouch each side, thin at the
            // top and flaring to a rounded bottom. A gap is left round each piece so they read apart in one colour; the
            // flap's lower edge is straight across, and a strap and buckle down the front were too busy at tab size
            pen.Arc(8, 3, 1.7f, Math.PI, 2 * Math.PI, 1.1f);
            pen.Fill(V(3.2f, 5.6f), V(4.2f, 3.4f), V(6, 2.8f), V(10, 2.8f), V(11.8f, 3.4f), V(12.8f, 5.6f),
                V(12.8f, 7.6f), V(3.2f, 7.6f));
            pen.Box(3.2f, 8.5f, 12.8f, 12.2f, 0, ImDrawFlags.None);
            pen.Box(3.2f, 13.1f, 12.8f, 15.2f, 1.2f, ImDrawFlags.RoundCornersBottom);

            foreach (var right in new[] { false, true }) {
                Vector2 P(float x, float y) => right ? V(16 - x, y) : V(x, y);
                var pouch = new[] {
                    P(1.9f, 9), P(2.7f, 9), P(2.7f, 14.3f), P(1.8f, 14.3f), P(1.3f, 13.8f), P(1.15f, 12.8f),
                };

                // the same turn either way round
                if (right) {
                    Array.Reverse(pouch);
                }

                pen.Fill(pouch);
            }
        }

        private static Vector2 V(float x, float y) => new Vector2(x, y);

        private static void CoinPurse(IconPen pen) {
            // a drawstring pouch: its gathered top flaring out, a gap where the string ties it, then the round body
            pen.Fill(new Vector2(4.4f, 1.4f), new Vector2(11.6f, 1.4f), new Vector2(9.6f, 4.3f), new Vector2(6.4f, 4.3f));
            pen.Fill(new Vector2(6.6f, 5.3f), new Vector2(9.4f, 5.3f), new Vector2(11.4f, 7.6f), new Vector2(4.6f, 7.6f));
            pen.Disc(8, 10.6f, 4.8f);
        }

        private static void CrossedSwords(IconPen pen) {
            Sword(pen, new Vector2(2.2f, 2.2f), new Vector2(11, 11), new Vector2(14.2f, 14.2f));
            Sword(pen, new Vector2(13.8f, 2.2f), new Vector2(5, 11), new Vector2(1.8f, 14.2f));
        }

        /// <summary>A sword from its tip to its guard, then its grip and pommel.</summary>
        private static void Sword(IconPen pen, Vector2 tip, Vector2 guard, Vector2 pommel) {
            var along = Vector2.Normalize(guard - tip);
            var across = new Vector2(-along.Y, along.X);
            pen.Line(tip.X, tip.Y, guard.X, guard.Y, 1.6f);
            pen.Line(guard.X - across.X * 2, guard.Y - across.Y * 2, guard.X + across.X * 2, guard.Y + across.Y * 2, 1.4f);
            pen.Line(guard.X, guard.Y, pommel.X, pommel.Y, 1.2f);
            pen.Disc(pommel.X, pommel.Y, 0.9f);
        }

        private static void Medal(IconPen pen) {
            // a ribbon's two straps meeting in a V, and the medal hanging from them
            pen.Fill(new Vector2(3.5f, 1), new Vector2(6.8f, 1), new Vector2(9.2f, 6.6f), new Vector2(6.6f, 7.6f));
            pen.Fill(new Vector2(9.2f, 1), new Vector2(12.5f, 1), new Vector2(9.4f, 7.6f), new Vector2(6.8f, 6.6f));
            pen.Ring(8, 11, 3.9f, 1.4f);
            pen.Disc(8, 11, 1.9f);
        }

        private static void CaveEntrance(IconPen pen) {
            // a stone arch on its pillars, steps going down inside, the ground
            pen.Arc(8, 8.6f, 4.6f, Math.PI, 2 * Math.PI, 2.4f);
            pen.Box(2.2f, 8.4f, 4.6f, 14.2f, 0, ImDrawFlags.None);
            pen.Box(11.4f, 8.4f, 13.8f, 14.2f, 0, ImDrawFlags.None);
            pen.Line(0.8f, 14.6f, 15.2f, 14.6f, 1.2f);
            pen.Line(6, 11.4f, 10, 11.4f, 1);
            pen.Line(6.6f, 13, 9.4f, 13, 1);
        }
    }
}
