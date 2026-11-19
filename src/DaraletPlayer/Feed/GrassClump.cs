namespace DaraletPlayer.Feed {
    /// <summary>One clump of grass or reeds (Feed/GrassLayout.cs), in its landblock's meters.</summary>
    public struct GrassClump {
        public GrassClump(float x, float y, float z, float yaw, float scale, int terrainType, bool reeds,
            float wildness = 0.5f) {
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
            Scale = scale;
            TerrainType = terrainType;
            Reeds = reeds;
            Wildness = wildness;
        }

        public float X { get; }

        public float Y { get; }

        /// <summary>The ground's height under it.</summary>
        public float Z { get; }

        public float Yaw { get; }

        public float Scale { get; }

        /// <summary>The terrain it grows from, whose texture colours it.</summary>
        public int TerrainType { get; }

        public bool Reeds { get; }

        /// <summary>0 kept (near a town) to 1 wild (Feed/Wildness.cs): which picture it's drawn with.</summary>
        public float Wildness { get; }
    }
}
