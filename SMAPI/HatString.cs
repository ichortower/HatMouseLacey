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
         * HatCollapseMap is filled on demand by copying in the data from the
         * private map, which is set up differently in order to reduce
         * duplication and make it easier to read and edit.
         * This one maps the hat you are wearing to the "main" hat it should
         * check instead.
         */
        public static Dictionary<string, string> HatCollapseMap = new();

        private static void FillCollapseMap()
        {
            foreach (var entry in _PrivateMap) {
                foreach (string s in entry.Value) {
                    _ = HatCollapseMap.TryAdd(s, entry.Key);
                }
            }
        }

        /*
         * This one uses the "main" hat as the key, and a list of hats that
         * should map to it as the value.
         */
        private static Dictionary<string, List<string>> _PrivateMap = new() {
            { "SV|Copper Pan", new() {
                    "SV|Steel Pan",
                    "SV|Gold Pan",
                    "SV|Iridium Pan", } },
            // Hats and Horns
            { "FS|PC.Hats/Hat/Seashell Tiara", new() {
                    "FS|PC.Hats/Hat/Seashell Tiara - Colorable", } },
            { "FS|PC.Hats/Hat/Witch Hat - Veiled Red", new() {
                    "FS|PC.Hats/Hat/Witch Hat - Veiled Colorable",
                    "FS|PC.Hats/Hat/Witch Hat - Veiled Colorable 2", } },
            { "FS|PC.Hats/Hat/Wood Elf Antlers", new() {
                    "FS|PC.Hats/Hat/Wood Elf Antlers - Colorable", } },
            { "FS|PC.Hats/Hat/Coral Headdress - Flame", new() {
                    "FS|PC.Hats/Hat/Coral Headdress - Bubblegum", } },
            { "FS|PC.Hats/Hat/Golden Laurel", new() {
                    "FS|PC.Hats/Hat/Small Laurel", } },
            { "FS|PC.Hats/Hat/Pillbox Hat - Angled", new() {
                    "FS|PC.Hats/Hat/Pillbox Hat - Round", } },
            // Luny's Witch Hats
            { "FS|Luny.WitchHats/Hat/Luny's Basic Witch Hat", new() {
                    "FS|Luny.WitchHats/Hat/Luny's Floral Witch Hat",
                    "FS|Luny.WitchHats/Hat/Luny's Floral Witch Hat With Trim", } },
            // The Toppest Hats
            { "FS|TeaLovingLad.TheToppestHats/Hat/Toppest Hat", new() {
                    "FS|TeaLovingLad.TheToppestHats/Hat/Toppest Hat Alt",
                    "FS|TeaLovingLad.TheToppestHats/Hat/Toppest Hat Prismatic",
                    "FS|TeaLovingLad.TheToppestHats/Hat/Toppest Hat Alt Prismatic",
                    "FS|TeaLovingLad.TheToppestHats/Hat/Lesser Hat",
                    "FS|TeaLovingLad.TheToppestHats/Hat/Lesser Hat Alt",
                    "FS|TeaLovingLad.TheToppestHats/Hat/Lesser Hat Prismatic",
                    "FS|TeaLovingLad.TheToppestHats/Hat/Lesser Hat Alt Prismatic",
                    "FS|TeaLovingLad.TheToppestHats/Hat/Topper Hat",
                    "FS|TeaLovingLad.TheToppestHats/Hat/Topper Hat Alt",
                    "FS|TeaLovingLad.TheToppestHats/Hat/Topper Hat Prismatic",
                    "FS|TeaLovingLad.TheToppestHats/Hat/Topper Hat Alt Prismatic", } },
            { "FS|TeaLovingLad.TheToppestHats/Hat/Reality-Defying Top Hat", new() {
                    "FS|TeaLovingLad.TheToppestHats/Hat/Reality-Defying Top Hat Alt",
                    "FS|TeaLovingLad.TheToppestHats/Hat/Reality-Defying Top Hat Prismatic",
                    "FS|TeaLovingLad.TheToppestHats/Hat/Reality-Defying Top Hat Alt Prismatic", } },
            // Kailey's Seasonal Hats (CP/FS)
            { "KaileyStardew.SeasonalHatsCP_BeretBlack", new() {
                    "KaileyStardew.SeasonalHatsCP_BeretGreen",
                    "KaileyStardew.SeasonalHatsCP_BeretMustard",
                    "KaileyStardew.SeasonalHatsCP_BeretRed",
                    "KaileyStardew.SeasonalHatsCP_BeretTeal",
                    "FS|kailey.seasonalhats/Hat/Beret (Black)",
                    "FS|kailey.seasonalhats/Hat/Beret (Green)",
                    "FS|kailey.seasonalhats/Hat/Beret (Mustard)",
                    "FS|kailey.seasonalhats/Hat/Beret (Red)",
                    "FS|kailey.seasonalhats/Hat/Beret (Teal)",
                    "FS|kailey.seasonalhats/Hat/Beret (Colourable)",
                    "FS|kailey.seasonalhats/Hat/Beret (Prismatic)", } },
            { "KaileyStardew.SeasonalHatsCP_FlowerCrownBlue", new() {
                    "KaileyStardew.SeasonalHatsCP_FlowerCrownBlackWhite",
                    "KaileyStardew.SeasonalHatsCP_FlowerCrownGray",
                    "KaileyStardew.SeasonalHatsCP_FlowerCrownGreen",
                    "KaileyStardew.SeasonalHatsCP_FlowerCrownOrange",
                    "KaileyStardew.SeasonalHatsCP_FlowerCrownPink",
                    "KaileyStardew.SeasonalHatsCP_FlowerCrownPurple",
                    "KaileyStardew.SeasonalHatsCP_FlowerCrownRed",
                    "KaileyStardew.SeasonalHatsCP_FlowerCrownWhitePurple",
                    "KaileyStardew.SeasonalHatsCP_FlowerCrownYellow",
                    "FS|kailey.seasonalhats/Hat/Flower Crown (Black White)",
                    "FS|kailey.seasonalhats/Hat/Flower Crown (Blue)",
                    "FS|kailey.seasonalhats/Hat/Flower Crown (Gray)",
                    "FS|kailey.seasonalhats/Hat/Flower Crown (Green)",
                    "FS|kailey.seasonalhats/Hat/Flower Crown (Orange)",
                    "FS|kailey.seasonalhats/Hat/Flower Crown (Pink)",
                    "FS|kailey.seasonalhats/Hat/Flower Crown (Purple)",
                    "FS|kailey.seasonalhats/Hat/Flower Crown (Red)",
                    "FS|kailey.seasonalhats/Hat/Flower Crown (White Purple)",
                    "FS|kailey.seasonalhats/Hat/Flower Crown (Yellow)",
                    "FS|kailey.seasonalhats/Hat/Flower Crown (Colourable 1)",
                    "FS|kailey.seasonalhats/Hat/Flower Crown (Colourable 2)",
                    "FS|kailey.seasonalhats/Hat/Flower Crown (Prismatic 1)",
                    "FS|kailey.seasonalhats/Hat/Flower Crown (Prismatic 2)", } },
            { "KaileyStardew.SeasonalHatsCP_FlowerCrownBird", new() {
                    "FS|kailey.seasonalhats/Hat/Flower Crown (Bird)", } },
            { "KaileyStardew.SeasonalHatsCP_StrawHatBeekeeper", new() {
                    "FS|kailey.seasonalhats/Hat/Straw Hat (Beekeeper)", } },
            { "KaileyStardew.SeasonalHatsCP_StrawHatBlue", new() {
                    "FS|kailey.seasonalhats/Hat/Straw Hat (Blue)",
                    "FS|kailey.seasonalhats/Hat/Straw Hat (Colourable)",
                    "FS|kailey.seasonalhats/Hat/Straw Hat (Prismatic)", } },
            { "KaileyStardew.SeasonalHatsCP_StrawHatFlowers", new() {
                    "FS|kailey.seasonalhats/Hat/Straw Hat (Flowers)",
                    "FS|kailey.seasonalhats/Hat/Straw Hat (Colourable Flowers)",
                    "FS|kailey.seasonalhats/Hat/Straw Hat (Prismatic Flowers)", } },
            { "KaileyStardew.SeasonalHatsCP_StrawHatSunflower", new() {
                    "FS|kailey.seasonalhats/Hat/Straw Hat (Sunflower)", } },
            { "KaileyStardew.SeasonalHatsCP_TrapperHat", new() {
                    "FS|kailey.seasonalhats/Hat/Trapper Hat",
                    "FS|kailey.seasonalhats/Hat/Trapper Hat (Colourable)",
                    "FS|kailey.seasonalhats/Hat/Trapper Hat (Prismatic)", } },
            { "KaileyStardew.SeasonalHatsCP_WinterBeanieBlue", new() {
                    "FS|kailey.seasonalhats/Hat/Winter Beanie (Blue)",
                    "FS|kailey.seasonalhats/Hat/Winter Beanie (Colourable)",
                    "FS|kailey.seasonalhats/Hat/Winter Beanie (Prismatic)", } },
            { "KaileyStardew.SeasonalHatsCP_WinterBeaniePink", new() {
                    "FS|kailey.seasonalhats/Hat/Winter Beanie (Pink)",
                    "FS|kailey.seasonalhats/Hat/Winter Beanie (Colourable 2)",
                    "FS|kailey.seasonalhats/Hat/Winter Beanie (Prismatic 2)", } },
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
