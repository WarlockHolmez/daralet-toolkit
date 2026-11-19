using Decal.Adapter;
using Decal.Adapter.Wrappers;

namespace DaraletPlayer.Ui {
    internal sealed partial class ToolbarPanel {
        /// <summary>
        /// The backpack takes an item dropped on it into the main pack, as the game's does. Not a pack: packs only go in
        /// pack slots.
        /// </summary>
        internal static void PutInMainPack(int id) {
            var item = CoreManager.Current.WorldFilter[id];
            var me = CoreManager.Current.CharacterFilter.Id;

            if (item == null || item.ObjectClass == ObjectClass.Container) {
                return;
            }

            var amount = StackSplit.Amount(id);

            if (amount > 0) {
                StackSplit.ToContainer(id, me, 0, amount);
                return;
            }

            if (item.Values(LongValueKey.Container, 0) == me && item.Values(LongValueKey.EquippedSlots, 0) == 0) {
                return;
            }

            CoreManager.Current.Actions.MoveItem(id, me, 0, false);
        }
    }
}
