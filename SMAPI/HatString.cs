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
         * Supports vanilla hats (who.hat, 0 <= id <= 93),
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
            if (who.hat.Value != null) {
                string hat = who.hat.Value.ItemId;
                // pre-1.6 hats have stringified integer IDs, 0-93.
                if (int.TryParse(hat, out int vanillaId)) {
                    if (vanillaId <= 93) {
                        return $"SV|{who.hat.Value.Name}";
                    }
                }
                // 1.6 hats have names and are in a list below
                if (Hats_16.Contains(hat)) {
                    return $"SV|{who.hat.Value.Name}";
                }
                return $"MOD|{hat}";
            }
            return null;
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
