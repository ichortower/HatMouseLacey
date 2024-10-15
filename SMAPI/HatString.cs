using FashionSense.Framework.Interfaces.API;
using FSApi = FashionSense.Framework.Interfaces.API.IApi;
using StardewValley;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ichortower_HatMouseLacey
{
    internal class LCHatString
    {
        public static readonly string ReactionsAsset = $"Strings\\{HML.CPId}_HatReactions";

        /*
         * Returns a string identifying the hat currently worn by the given
         * Farmer.
         * Returns null if no hat could be detected.
         *
         * Supports vanilla hats (who.hat, string-integer ids from 0 to 93
         *       (1.5) and expected names (1.6))
         *   "SV|Hat Name"
         * modded hats (who.hat, any other id),
         *   "Hat Id"
         * and fashion sense hats (read from FS API)
         *   "FS|Hat Id"
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
            return id;
        }

        /*
         * Convert a hat string (as returned by the Get<X>HatString methods
         * above) into a key as it is used in the hat strings json.
         * This entails:
         *   remove spaces ' ', hyphens '-', and single quotes '''
         *   replace | with .
         */
        public static string KeyFromHatString(string hatstr)
        {
            var r = @"[ \-']";
            return Regex.Replace(hatstr, r, string.Empty)
                    .Replace("|", ".");
        }

        /*
         * Mutate specific hat strings into other ones.
         * Used to collapse all the pan hats into Copper Pan, and to similarly
         * map multiple variants (e.g. colorways) of the same modded hat into
         * one reaction.
         */
        public static string HatIdCollapse(string hatstr)
        {
            if (hatstr is null) {
                return hatstr;
            }
            if (HatCollapseMap.TryGetValue(hatstr, out string conv)) {
                return conv;
            }
            return hatstr;
        }

        public static Dictionary<string, string> HatCollapseMap = new() {
            // vanilla: collapse the pans
            {"SV|Steel Pan", "SV|Copper Pan"},
            {"SV|Gold Pan", "SV|Copper Pan"},
            {"SV|Iridium Pan", "SV|Copper Pan"},
            // Hats and Horns: colorable variants of static hats
            {"FS|PC.Hats/Hat/Seashell Tiara - Colorable",
                    "FS|PC.Hats/Hat/Seashell Tiara"},
            {"FS|PC.Hats/Hat/Witch Hat - Veiled Colorable",
                    "FS|PC.Hats/Hat/Witch Hat - Veiled Red"},
            {"FS|PC.Hats/Hat/Witch Hat - Veiled Colorable 2",
                    "FS|PC.Hats/Hat/Witch Hat - Veiled Red"},
            {"FS|PC.Hats/Hat/Wood Elf Antlers - Colorable",
                    "FS|PC.Hats/Hat/Wood Elf Antlers"},
            // Luny's Witch Hats: all three get the same reaction
            {"FS|Luny.WitchHats/Hat/Luny's Floral Witch Hat",
                    "FS|Luny.WitchHats/Hat/Luny's Basic Witch Hat"},
            {"FS|Luny.WitchHats/Hat/Luny's Floral Witch Hat With Trim",
                    "FS|Luny.WitchHats/Hat/Luny's Basic Witch Hat"},
        };

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
