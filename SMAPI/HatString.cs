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
                    return GetFSHatString(pair.Value);
                }
            }
            return GetItemHatString(who.hat.Value);
        }

        /*
         * Returns a string identifying the given Fashion Sense appearance id.
         * Not complicated; just encapsulation.
         */
        public static string GetFSHatString(string appearanceId)
        {
            return $"FS|{appearanceId}";
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
         * This entails removing a bunch of special characters:
         *     -'()[] and spaces
         * and replacing | with .
         */
        public static string KeyFromHatString(string hatstr)
        {
            var r = @"[ \-'\(\)\[\]]";
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
            if (HatCollapseMap.Count == 0) {
                FillCollapseMap();
            }
            if (HatCollapseMap.TryGetValue(hatstr, out string conv)) {
                return conv;
            }
            return hatstr;
        }

        /*
         * HatCollapseMap is filled on demand by reading in the mapping data
         * from a file (only once, after which it remains filled).
         * The map uses your current hat as the key and the hat it should match
         * instead as the value, but the data file is in reverse order: the
         * final hat value is the key, and the value is a list of other hats
         * that map to it.
         * This reversal of order is to reduce duplication and make it easier
         * to understand and edit.
         */
        public static Dictionary<string, string> HatCollapseMap = new();

        private static void FillCollapseMap()
        {
            var dataMap = HML.ModHelper.Data.ReadJsonFile
                    <Dictionary<string, List<string>>>("data/hat-collapse-map.json");
            foreach (var entry in dataMap) {
                foreach (string s in entry.Value) {
                    _ = HatCollapseMap.TryAdd(s, entry.Key);
                }
            }
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
