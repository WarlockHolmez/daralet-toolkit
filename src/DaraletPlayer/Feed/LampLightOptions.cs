using System.Runtime.Serialization;

namespace DaraletPlayer.Feed {
    /// <summary>
    /// Light from the game's own lights, per pixel, with shadows (Ui/DepthShading.Lamps.cs), in settings.json as
    /// "lampLight". Off to start; an experiment (the user's call, November 2026: lighting and shadows from other than
    /// the sun).
    /// </summary>
    [DataContract]
    public class LampLightOptions {
        [DataMember(Name = "on")] public bool On;

        /// <summary>How bright, in percent.</summary>
        [DataMember(Name = "strength")] public int Strength;

        /// <summary>Whether walls and things block a lamp's light (a march through the depth buffer).</summary>
        [DataMember(Name = "shadows")] public bool Shadows;

        public LampLightOptions() {
            SetDefaults();
        }

        public int ClampedStrength => Strength < 0 ? 0 : Strength > 200 ? 200 : Strength;

        public string Summary() {
            return On ? $"lamps {ClampedStrength}%" + (Shadows ? " with shadows" : "") : "lamps off";
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context) {
            SetDefaults();
        }

        private void SetDefaults() {
            On = false;
            Strength = 60;
            Shadows = true;
        }
    }
}
