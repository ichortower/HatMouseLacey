using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using xTile;
using xTile.Layers;
using xTile.Tiles;

namespace ichortower_HatMouseLacey
{
    /*
     * This is a bucket to hold functions for compatibility with other mods.
     * If it's unwieldy in ModEntry, put it here.
     */
    internal class LCCompat
    {
        public static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            /*
             * EditMap patch for the forest map.
             * Doing it here in C# lets us check source tiles before updating
             * them, which makes the patch a little more arcane but hopefully
             * cuts down on other-mod-specific patches.
             *
             * The loop here checks the Back layer at specific locations and
             * maps tile index values to new ones, mostly to turn yucky grass
             * into nice grass or to remove fence holes.
             * It's extremely lucky that I can omit 1966 in the map to avoid
             * special-casing the new door location for SVR3.
             */
            if (e.Name.IsEquivalentTo("Maps/Forest")) {
                e.Edit(asset => {
                    var mapref = asset.AsMap().Data;
                    Layer back = mapref.GetLayer("Back");
                    //Layer buildings = mapref.GetLayer("Buildings");
                    //Layer front = mapref.GetLayer("Front");
                    var backList = new List<Vector2>() {
                        new Vector2(28, 99),
                        new Vector2(31, 96),
                        new Vector2(32, 98),
                        new Vector2(36, 98),
                        new Vector2(37, 98),
                        new Vector2(37, 96),
                        new Vector2(38, 96),
                        new Vector2(38, 95),
                        new Vector2(37, 93),
                        new Vector2(38, 93),
                        new Vector2(38, 92),
                        new Vector2(45, 98),
                        new Vector2(47, 97),
                        new Vector2(48, 97),
                        new Vector2(49, 99),
                    };
                    //var buildingsList = new List<Vector2>() {
                        //new Vector2(37, 93), new Vector2(38, 93)
                    //};
                    //var frontList = new List<Vector2>() {
                        //new Vector2(37, 92), new Vector2(38, 92)
                    //};
                    /* -1 in the value here means delete (null) */
                    var convertDict = new Dictionary<int, int>() {
                        {256, 175},
                        {400, 175},
                        {401, 175},
                        {1964, 175},
                        {1965, 175},
                        {329, 351},
                        {405, 999},
                        //{383, -1},
                        //{384, -1},
                        //{385, -1},
                        //{358, -1},
                        //{359, -1},
                        //{360, -1},
                    };
                    /* saved delegate here for if the other two layers are needed
                    Action<Vector2, Layer> mutate = delegate(Vector2 coords, Layer layer) */
                    foreach (var coords in backList) {
                        Tile t = back.Tiles[(int)coords.X, (int)coords.Y];
                        if (t is null) {
                            continue;
                        }
                        int target;
                        if (!convertDict.TryGetValue(t.TileIndex, out target)) {
                            continue;
                        }
                        /* delete if the value is -1, as above */
                        if (target == -1) {
                            back.Tiles[(int)coords.X, (int)coords.Y] = null;
                        }
                        else {
                            t.TileIndex = target;
                        }
                    };
                }, AssetEditPriority.Late);
            }
        }

        /*
         * Run the detection for other installed mods and their config.json
         * settings, to generate best guesses for which patches and palettes
         * to use.
         * This is run at save load, and also when our config is updated by
         * GMCM (except at the title screen).
         */
        public static void DetectModMatching()
        {
            if (Game1.gameMode == Game1.titleScreenGameMode) {
                return;
            }
            ModEntry.RecolorDetected = "Vanilla";
            ModEntry.InteriorDetected = "Vanilla";
            ModEntry.RetextureDetected = "Vanilla";

            Dictionary<string, string> recolorMods = new() {
                {"DaisyNiko.EarthyRecolour", "Earthy"},
                {"grapeponta.VibrantPastoralRecolor", "VPR"},
                {"Lita.StarblueValley", "Starblue"},
                {"Acerbicon.Recolor", "Wittily"},
            };
            foreach (var pair in recolorMods) {
                var modInfo = ModEntry.HELPER.ModRegistry.Get(pair.Key);
                if (modInfo != null) {
                    ModEntry.MONITOR.Log($"Found mod '{pair.Key}'. " +
                            $"Setting detected palette to '{pair.Value}'.",
                            LogLevel.Trace);
                    ModEntry.RecolorDetected = pair.Value;
                    break;
                }
            }

            /* interior recoloring is more complicated. each mod does it
             * differently, and wittily doesn't do it at all */
            Dictionary<string, string> interiorMods = new() {
                {"DaisyNiko.EarthyInteriors", "Earthy"},
                {"grapeponta.VibrantPastoralRecolor", "Town Interiors:true:VPR"},
                {"Lita.StarblueValley", "Interiors:true:Starblue"},
            };
            foreach (var pair in interiorMods) {
                var split = pair.Value.Split(":");
                var modInfo = ModEntry.HELPER.ModRegistry.Get(pair.Key);
                if (modInfo != null) {
                    if (split.Length == 1) {
                        ModEntry.MONITOR.Log($"Found mod '{pair.Key}'. " +
                                $"Setting detected interior palette to '{split[0]}'.",
                                LogLevel.Trace);
                        ModEntry.InteriorDetected = split[0];
                        break;
                    }
                    else if (split.Length == 3) {
                        var modPath = (string)modInfo.GetType()
                                .GetProperty("DirectoryPath").GetValue(modInfo);
                        var jConfig = JObject.Parse(File.ReadAllText(
                                Path.Combine(modPath, "config.json")));
                        var cvalue = jConfig.GetValue(split[0])
                                .Value<string>();
                        if (cvalue == split[1]) {
                            ModEntry.MONITOR.Log($"Found active mod '{pair.Key}'. " +
                                    $"Setting detected interior palette to '{split[2]}'.",
                                    LogLevel.Trace);
                            ModEntry.InteriorDetected = split[2];
                            break;
                        }
                    }
                    else {
                        ModEntry.MONITOR.Log("Found bad interior detection format: " +
                                $"'{pair.Key}' -> '{pair.Value}'. " +
                                "Expected 1 or 3 fields in value. Skipping.",
                                LogLevel.Warn);
                    }
                }
            }

            /* retextures work like interior recolors: only some use config
             * values. */
            Dictionary<string, string> retextureMods = new() {
                {"Gweniaczek.WayBackPT", "WaybackPT"},
                {"Elle.TownBuildings", "Hat Mouse House:true:ElleTown"},
                {"yri.ProjectYellogTownOverhaul",
                        "HatMouseHouseRestored:true:YriYellog"},
                {"yri.ProjectYellogTownOverhaulPerformance",
                        "HatMouseHouseRestored:true:YriYellog"},
                {"kaya.floralvalley", "FlowerValley"}
            };
            foreach (var pair in retextureMods) {
                var split = pair.Value.Split(":");
                var modInfo = ModEntry.HELPER.ModRegistry.Get(pair.Key);
                if (modInfo != null) {
                    if (split.Length == 1) {
                        ModEntry.MONITOR.Log($"Found mod '{pair.Key}'. " +
                                $"Setting detected retexture to '{split[0]}'.",
                                LogLevel.Trace);
                        ModEntry.RetextureDetected = split[0];
                        break;
                    }
                    else if (split.Length == 3) {
                        var modPath = (string)modInfo.GetType()
                                .GetProperty("DirectoryPath").GetValue(modInfo);
                        var jConfig = JObject.Parse(File.ReadAllText(
                                Path.Combine(modPath, "config.json")));
                        var cvalue = jConfig.GetValue(split[0])
                                .Value<string>();
                        if (cvalue == split[1]) {
                            ModEntry.MONITOR.Log($"Found active mod '{pair.Key}'. " +
                                    $"Setting detected retexture to '{split[2]}'.",
                                    LogLevel.Trace);
                            ModEntry.RetextureDetected = split[2];
                            break;
                        }
                    }
                    else {
                        ModEntry.MONITOR.Log("Found bad retexture detection format: " +
                                $"'{pair.Key}' -> '{pair.Value}'. " +
                                "Expected 1 or 3 fields in value. Skipping.",
                                LogLevel.Warn);
                    }
                }
            }
        }

    } // LCCompat

} // namespace
