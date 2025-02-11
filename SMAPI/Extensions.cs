using StardewValley;
using StardewValley.Menus;
using System;
using System.Reflection;

using BCApi = Leclair.Stardew.BetterCrafting.ModAPI;

namespace ichortower_HatMouseLacey
{
    public static class Extensions
    {
        internal const string bcm = "Leclair.Stardew.BetterCrafting.Menus.BetterCraftingPage";
        internal static bool BCMissing = false;
        internal static BCApi _bcapi = null;
        internal static FieldInfo HeldItemField = null;

        public static StardewValley.Item HeldItem(this IClickableMenu menu)
        {
            IClickableMenu m = menu;
            while (m.GetChildMenu() != null) {
                m = m.GetChildMenu();
            }
            if (m is GameMenu gm) {
                if (gm.currentTab == GameMenu.craftingTab) {
                    if (gm.pages[GameMenu.craftingTab] is CraftingPage p) {
                        return p.heldItem;
                    }
                    if (TryGetBetterCraftingHeldItem(gm.pages[GameMenu.craftingTab],
                                out StardewValley.Item i)) {
                        return i;
                    }
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

        internal static bool TryGetBetterCraftingHeldItem(IClickableMenu menu,
                out StardewValley.Item ret)
        {
            ret = null;
            if (BCMissing) {
                return false;
            }
            if (_bcapi == null) {
                _bcapi = HML.ModHelper.ModRegistry.GetApi<BCApi>(
                        "leclair.bettercrafting");
                if (_bcapi is null) {
                    BCMissing = true;
                    return false;
                }
            }
            Type BetterCraftingMenuType = _bcapi.GetMenuType();
            if (HeldItemField == null) {
                HeldItemField = BetterCraftingMenuType.GetField("HeldItem",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                if (HeldItemField == null) {
                    BCMissing = true;
                    return false;
                }
            }
            if (menu.GetType() != BetterCraftingMenuType) {
                return false;
            }
#nullable enable
            StardewValley.Item? it = (StardewValley.Item?)HeldItemField.GetValue(menu);
            if (it is StardewValley.Item thing) {
                ret = thing;
                return true;
            }
#nullable disable
            return false;
        }
    }
}
