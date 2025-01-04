using StardewValley;
using StardewValley.Menus;

namespace ichortower_HatMouseLacey
{
    public static class Extensions
    {
        public static StardewValley.Item HeldItem(this IClickableMenu menu)
        {
            IClickableMenu m = menu;
            while (m.GetChildMenu() != null) {
                m = m.GetChildMenu();
            }
            if (m is GameMenu gm) {
                if (gm.currentTab == GameMenu.craftingTab) {
                    return (gm.pages[GameMenu.craftingTab] as CraftingPage).heldItem;
                }
                return Game1.player.CursorSlotItem;
            }
            if (menu is ShopMenu sm) {
                return (StardewValley.Item)sm.heldItem;
            }
            if (menu is CraftingPage cp) {
                return cp.heldItem;
            }
            if (menu is MenuWithInventory mwi) {
                return mwi.heldItem;
            }
            if (menu is JunimoNoteMenu jnm) {
                return jnm.heldItem;
            }
            return null;
        }
    }
}
