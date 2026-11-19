using Microsoft.DirectX.Direct3D;

namespace DaraletPlayer.Ui {
    /// <summary>The triangles of a far tree model that share one texture (FarTreeModels).</summary>
    internal sealed class FarTreePart {
        public Texture Texture;

        /// <summary>The first index in the model's index buffer, and how many triangles from there.</summary>
        public int StartIndex;

        public int Triangles;

        /// <summary>Cut out by alpha, as leaves are.</summary>
        public bool CutOut;
    }
}
