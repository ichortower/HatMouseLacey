using FashionSense.Framework.Interfaces.API;
using FSApi = FashionSense.Framework.Interfaces.API.IApi;
using JsonAssets;
using StardewValley;
using System.Collections.Generic;

namespace ichortower_HatMouseLacey
{
    internal class LCHatString
    {
        /*
         * Returns a string identifying the hat currently worn by the given
         * Farmer.
         * Returns null if no hat could be detected.
         *
         * Supports vanilla hats (who.hat, string-integer ids from 0 to 93
         *       (1.5) and expected names (1.6))
         *   "SV|Hat Name"
         * modded hats (who.hat, any other id),
         *   "MOD|Hat Name"
         * and fashion sense hats (read from FS API)
         *   "FS|Hat Name"
         */
        public static string GetCurrentHatString(Farmer who)
        {
            /* check FS first, since it overrides physical hats */
            var fsapi = HML.ModHelper.ModRegistry.GetApi<FSApi>(
                    "PeacefulEnd.FashionSense");
            if (fsapi != null) {
                var pair = fsapi.GetCurrentAppearanceId(FSApi.Type.Hat, who);
                if (pair.Key) {
                    return $"FS|{pair.Value}";
                }
            }
            return GetItemHatString(who.hat.Value);
        }

        /*
         * Returns a string identifying the given Hat item.
         * Returns null if the hat is null.
         */
        public static string GetItemHatString(StardewValley.Objects.Hat h)
        {
            if (h is null) {
                return null;
            }
            string id = h.ItemId;
            // pre-1.6 hats have stringified integer IDs, 0-93.
            if (int.TryParse(id, out int vanillaId)) {
                if (vanillaId <= 93) {
                    return $"SV|{h.Name}";
                }
            }
            // 1.6 hats have name ids in the list below
            if (Hats_16.Contains(id)) {
                return $"SV|{h.Name}";
            }
            return $"MOD|{id}";
        }

        public static HashSet<string> Hats_16 = new() {
            "AbigailsBow",
            "TricornHat",
            "JojaCap",
            "LaurelWreathCrown",
            "GilsHat",
            "BlueBow",
            "DarkVelvetBow",
            "MummyMask",
            "BucketHat",
            "SquidHat",
            "SportsCap",
            "RedFez",
            "RaccoonHat",
            "SteelPanHat",
            "GoldPanHat",
            "IridiumPanHat",
            "MysteryHat",
            "DarkBallcap",
            "LeprechuanHat", // this is correct. vanilla data has this typo
            "JunimoHat",
            "PaperHat",
            "PageboyCap",
            "JesterHat",
            "BlueRibbon",
            "GovernorsHat",
            "WhiteBow",
            "SpaceHelmet",
            "InfinityCrown",
        };
    }
}
