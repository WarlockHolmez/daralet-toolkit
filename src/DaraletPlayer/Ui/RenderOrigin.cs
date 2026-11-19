using Decal.Adapter;
using DaraletPlayer.Feed;
using Microsoft.DirectX;

namespace DaraletPlayer.Ui {
    /// <summary>
    /// Which landblock the device's world is measured from: the camera's, not your character's (seen in the game,
    /// November 2026: a step across a landblock's edge, with the camera behind you in the one before, moved the shadows'
    /// maps and the far scenery 192 meters). The game keeps the camera's Position at 0x81FF10 (its cell at +4, its origin
    /// at +0x3C, the same numbers as the device's camera, with height and north swapped as the device has them), and
    /// draws its world in meters from that cell's landblock's south-west corner: so does everything the plugin draws in
    /// it (billboards, 3D trees, grass, the shadow maps, the water map, the lamps), and your character's place is put in
    /// that frame (Player).
    /// </summary>
    internal static unsafe class RenderOrigin {
        private const int CameraCell = 0x81FF10 + 4;

        /// <summary>The camera's landblock (0xXXYY), or your character's while the game has no camera.</summary>
        public static uint Landblock {
            get {
                var cell = *(uint*)CameraCell;
                return cell != 0 ? cell >> 16 : unchecked((uint)CoreManager.Current.Actions.Landcell) >> 16;
            }
        }

        /// <summary>Your character's place in the device's world (east, height, north).</summary>
        public static Vector3 Player() {
            var actions = CoreManager.Current.Actions;
            var (east, north) = InFrame(unchecked((uint)actions.Landcell) >> 16, (float)actions.LocationX,
                (float)actions.LocationY);
            return new Vector3(east, (float)actions.LocationZ, north);
        }

        /// <summary>A place in a landblock's own meters, in the device's world's (east, north).</summary>
        public static (float East, float North) InFrame(uint landblock, float x, float y) {
            var origin = Landblock;
            return (x + ((int)(landblock >> 8) - (int)(origin >> 8)) * SceneryLayout.BlockSize,
                y + ((int)(landblock & 0xFF) - (int)(origin & 0xFF)) * SceneryLayout.BlockSize);
        }
    }
}
