using System;

namespace DaraletPlayer.Ui {
    /// <summary>One of the game's world draws, noted to be made again from the sun (ShadowCasters).</summary>
    internal sealed class ShadowDraw {
        public IntPtr Stream;
        public int Offset;
        public IntPtr Indices;
        public IntPtr Texture;
        public IntPtr Declaration;
        public bool AlphaTest;

        /// <summary>The World matrix, row by row, as Direct3D has it.</summary>
        public float[] World;

        public int Type;
        public int BaseVertex;
        public int MinIndex;
        public int Vertices;
        public int StartIndex;
        public int Count;
    }
}
