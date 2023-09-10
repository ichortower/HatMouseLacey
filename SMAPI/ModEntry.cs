using ContentPatcher;
using GenericModConfigMenu;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Enums;
using StardewValley;
using StardewValley.Characters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using xTile;
using xTile.Layers;
using xTile.Tiles;

namespace ichortower_HatMouseLacey
{

    public sealed class ModConfig
    {
        /*
         * AlwaysAdopt forces Lacey to adopt children no matter what the
         * farmer's sex is. The babies will be unmodified humans.
         * If false, Lacey can get pregnant with a male farmer. Any children
         * she has this way will be mice, like their mother (if the farmer is
         * female, children will be adopted humans).
         *
         * This does not affect any other spouses' children.
         *
         * default: true
         */
        public bool AlwaysAdopt { get; set; } = true;

        /*
         * DTF enables some suggestive dialogue lines (they are not explicit;
         * picture Haley's Winter Star dialogue, or sharing Emily's sleeping
         * bag).
         *
         * It will also slightly modify Lacey's 10-heart and 14-heart events.
         *
         * default: true
         */
        public bool DTF { get; set; } = true;
    }

    internal sealed class ModEntry : Mod
    {
        public static ModConfig Config = null!;

        public static IMonitor MONITOR;
        public static IModHelper HELPER;

        /*
         * Lacey's internal name. Please ensure that this matches her internal
         * name in the NPCDispositions file.
         */
        public static string LCInternalName = "HatMouseLacey";

        /*
         * Entry point.
         * Sets up the code patches and necessary event handlers.
         * OnSaveLoaded is intended to hotfix problems that can occur on saves
         * created before installing this mod.
         */
        public override void Entry(IModHelper helper)
        {
            ModEntry.MONITOR = this.Monitor;
            ModEntry.HELPER = helper;
            ModEntry.Config = helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
            //helper.Events.Specialized.LoadStageChanged += this.NewMapHandler;
            helper.ConsoleCommands.Add("lacey_map_repair", "\nReloads Forest map objects in the vicinity of Lacey's cabin,\nto fix the bushes in saves from before installation.\nYou shouldn't need to run this, but it's safe to do so.", this.LaceyMapRepair);
            helper.ConsoleCommands.Add("mousify_child", "\nSets or unsets mouse child status on one of your children.\nUse this if your config settings weren't right and you got the wrong children,\nor just to morph your kids for fun.\n\nUsage: mousify_child <name> <variant>\n    where <variant> is -1 (human), 0 (grey), or 1 (brown).", this.MousifyChild);

            /*
             * Apply Harmony patches by getting all the methods in Patcher
             * and going feral with reflection on them.
             * Was this a good idea, since the annotations feature exists? No.
             * But did it save me a lot of work? Also no.
             */
            var harmony = new Harmony(this.ModManifest.UniqueID);
            MethodInfo[] funcs = typeof(Patcher).GetMethods(
                    BindingFlags.Static | BindingFlags.Public);
            try {
                Assembly sdv = typeof(StardewValley.Game1).Assembly;
                foreach (var func in funcs) {
                    string[] split = func.Name.Split("__");
                    string[] parts = split[1].Split('_');
                    if (split.Length < 2 || parts.Length < 2) {
                        MONITOR.Log($"bad Patcher function name '{func.Name}'", LogLevel.Warn);
                        continue;
                    }
                    string fqn = "StardewValley." + split[0].Replace("_", ".");
                    Type t = sdv.GetType(fqn);
                    if (t is null) {
                        MONITOR.Log($"type not found: '{fqn}'", LogLevel.Warn);
                        continue;
                    }
                    List<Type> args = new List<Type>();
                    foreach (var p in func.GetParameters()) {
                        if (p.Name == "__instance" || p.Name == "__result") {
                            continue;
                        }
                        args.Add(p.ParameterType);
                    }
                    /* there are some null arguments here because Type.GetMethod
                     * tries to match an int overload instead of the BindingFlags
                     * one if we use three arguments */
                    MethodInfo m = t.GetMethod(parts[0],
                            BindingFlags.Static | BindingFlags.Instance |
                            BindingFlags.Public | BindingFlags.NonPublic,
                            null,
                            args.ToArray(),
                            null);
                    if (m is null) {
                        MONITOR.Log($"within type '{fqn}': method not found: " +
                                $"'{parts[0]}({string.Join(", ", args)})'",
                                LogLevel.Warn);
                        continue;
                    }
                    var hm = new HarmonyMethod(typeof(Patcher), func.Name);
                    if (parts[1] == "Prefix") {
                        harmony.Patch(original: m, prefix: hm);
                    }
                    else if (parts[1] == "Postfix") {
                        harmony.Patch(original: m, postfix: hm);
                    }
                    else {
                        MONITOR.Log($"Not applying unimplemented patch type '{parts[1]}'",
                                LogLevel.Warn);
                        continue;
                    }
                    MONITOR.Log($"Patched ({parts[1]}) {t.FullName}.{m.Name}", LogLevel.Trace);
                }
            }
            catch (Exception e) {
                MONITOR.Log($"Caught exception while applying Harmony patches:\n{e}",
                        LogLevel.Warn);
            }
        }

        /*
         * Reload the forest's large terrain features in the edited map region
         * (i.e. bushes). The MouseHouseExterior map moves and removes some,
         * but save data includes all bush locations and overrides the map data
         * when loading, so old saves don't see the updates. This function
         * forces in the new map data.
         * NB doesn't do trees; must update this function if those get moved
         */
        private void LaceyMapRepair(string command, string[] args)
        {
            this.Monitor.Log($"Reloading terrain features near Lacey's house", LogLevel.Trace);
            /* should match the dimensions of the exterior map in CP/assets/maps.json */
            var rect = new Microsoft.Xna.Framework.Rectangle(23, 89, 17, 11);
            GameLocation forest = Game1.getLocationFromName("Forest");
            if (forest is null || forest.map is null) {
                return;
            }
            Layer paths = forest.map.GetLayer("Paths");
            if (paths is null) {
                return;
            }
            /*
             * easiest "solution" is to simply Clear() the list and call
             * forest.loadObjects(), but that resets the whole map, which may
             * cause problems (other mods, beach shortcut, etc.), so do a
             * targeted sweep.
             *   forest.largeTerrainFeatures.Clear();
             *   forest.loadObjects();
             */
            var toRemove = new List<StardewValley.TerrainFeatures.LargeTerrainFeature>();
            foreach (var feature in forest.largeTerrainFeatures) {
                Vector2 pos = feature.tilePosition.Value;
                if (pos.X >= rect.X && pos.X <= rect.X+rect.Width &&
                        pos.Y >= rect.Y && pos.Y <= rect.Y+rect.Height) {
                    toRemove.Add(feature);
                }
            }
            foreach (var doomed in toRemove) {
                forest.largeTerrainFeatures.Remove(doomed);
            }
            for (int x = rect.X; x < rect.X+rect.Width; ++x) {
                for (int y = rect.Y; y < rect.Y+rect.Height; ++y) {
                    Tile t = paths.Tiles[x, y];
                    if (t is null) {
                        continue;
                    }
                    if (t.TileIndex >= 24 && t.TileIndex <= 26) {
                        forest.largeTerrainFeatures.Add(
                                new StardewValley.TerrainFeatures.Bush(
                                new Vector2(x,y), 26 - t.TileIndex, forest));
                    }
                }
            }
        }

        private void MousifyChild(string command, string[] args)
        {
            if (args.Length < 2) {
                this.Monitor.Log($"Usage: mousify_child <name> <variant>", LogLevel.Warn);
                return;
            }
            if (Game1.player == null) {
                return;
            }
            Child child = null;
            try {
                foreach (var ch in Game1.player.getChildren()) {
                    if (ch.Name.Equals(args[0])) {
                        child = ch;
                        break;
                    }
                }
            }
            catch {}
            if (child == null) {
                this.Monitor.Log($"Could not find your child named '{args[0]}'.", LogLevel.Warn);
                return;
            }
            string variant = args[1];
            if (variant != "-1" && variant != "0" && variant != "1") {
                this.Monitor.Log($"Unrecognized variant '{variant}'. Using 0 instead.", LogLevel.Warn);
                variant = "0";
            }
            child.modData[$"{LCInternalName}/ChildVariant"] = variant;
            child.reloadSprite();
        }

        /*
         * Fixes several issues that occur when loading old saves with this
         * mod newly installed.
         *  1. Lacey's schedule doesn't load
         *  2. The Forest map edits that move the bushes don't take
         * The first one only occurs once. The second is persistent, but once
         * we run the fix function, the results are permanent, as long as the
         * player sleeps and saves the game.
         */
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            NPC Lacey = Game1.getCharacterFromName(LCInternalName);
            if (Lacey.Schedule is null) {
                this.Monitor.Log($"Regenerating Lacey's schedule", LogLevel.Trace);
                Lacey.Schedule = Lacey.getSchedule(Game1.dayOfMonth);
                Lacey.checkSchedule(Game1.timeOfDay);
            }

            /* this is checking for a specific bush that should be gone */
            GameLocation forest = Game1.getLocationFromName("Forest");
            if (forest != null) {
                bool doClean = false;
                foreach (var feature in forest.largeTerrainFeatures) {
                    Vector2 pos = feature.tilePosition.Value;
                    if (pos.X == 29 && pos.Y == 96) {
                        doClean = true;
                        break;
                    }
                }
                if (doClean) {
                    LaceyMapRepair("", null);
                }
            }
        }

        /*
         * Register Content Patcher tokens (for config mirroring).
         * Register GMCM entries.
         * Load the custom .ogg music tracks into the soundBank.
         */
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var cpapi = this.Helper.ModRegistry.GetApi<IContentPatcherAPI>(
                    "Pathoschild.ContentPatcher");
            cpapi.RegisterToken(this.ModManifest, "AlwaysAdopt", () => {
                return new[] {$"{Config.AlwaysAdopt}"};
            });
            cpapi.RegisterToken(this.ModManifest, "DTF", () => {
                return new[] {$"{Config.DTF}"};
            });
            this.Monitor.Log($"Registered Content Patcher tokens for config options",
                    LogLevel.Trace);

            var cmapi = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(
                    "spacechase0.GenericModConfigMenu");
            if (cmapi != null) {
                cmapi.Register(
                    mod: this.ModManifest,
                    reset: () => ModEntry.Config = new ModConfig(),
                    save: () => this.Helper.WriteConfig(ModEntry.Config)
                );
                cmapi.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => "AlwaysAdopt",
                    tooltip: () => this.Helper.Translation.Get("gmcm.alwaysadopt.tooltip"),
                    getValue: () => ModEntry.Config.AlwaysAdopt,
                    setValue: value => ModEntry.Config.AlwaysAdopt = value
                );
                cmapi.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => "DTF",
                    tooltip: () => this.Helper.Translation.Get("gmcm.dtf.tooltip"),
                    getValue: () => ModEntry.Config.DTF,
                    setValue: value => ModEntry.Config.DTF = value
                );
                this.Monitor.Log($"Registered Generic Mod Config Menu entries",
                        LogLevel.Trace);
            }

            Dictionary<string, string> songs = new Dictionary<string, string>(){
                    {"HML_Confession", "Confession.ogg"},
                    {"HML_Lonely", "Lonely.ogg"},
                    {"HML_Upbeat", "Upbeat.ogg"},
            };
            Thread t = new Thread((ThreadStart)delegate {
                var l = new LCMusicLoader();
                foreach (var song in songs) {
                    var path = Path.Combine(this.Helper.DirectoryPath, "assets", song.Value);
                    l.LoadOggSong(song.Key, path);
                }
            });
            t.Start();
        }

        /*
         * Clear out savefile-specific cached data.
         * Stop the event background ticker, just in case it's running.
         */
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            LCSaveData.ClearCache();
            LCEventCommands.stopTicker();
        }

        /* Unused since CP handles this
        private void NewMapHandler(object sender, LoadStageChangedEventArgs e)
        {
            if (e.NewStage == LoadStage.SaveAddedLocations ||
                    e.NewStage == LoadStage.CreatedInitialLocations) {
                Game1.locations.Add(new GameLocation("Maps\\MouseHouse", "MouseHouse"));
            }
        }
        */

    }
}
