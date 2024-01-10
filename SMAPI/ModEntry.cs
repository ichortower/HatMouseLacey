using ContentPatcher;
using GenericModConfigMenu;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Enums;
using StardewValley;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;
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

        /*
         * RecolorPalette is for detecting or setting which recolor mod is in
         * use. This ultimately controls which palette for exterior assets
         * (mouse house, cursors storefront) will be loaded.
         * 'Auto' (the default) means use the detected result.
         */
        public Palette RecolorPalette = Palette.Auto;

        /*
         * InteriorPalette is just like RecolorPalette, but for interiors.
         * Only affects one asset currently (the interior tilesheet).
         */
        public Palette InteriorPalette = Palette.Auto;

        /*
         * MatchRetexture is like RecolorPalette, but controls which version of
         * the exterior patches is applied (this loads different assets for the
         * house and storefront, instead of just different colors).
         */
         public Retexture MatchRetexture = Retexture.Auto;
    }

    public enum Palette {
        Auto,
        Vanilla,
        Earthy,
        VPR,
        Starblue,
        Wittily,
    }

    public enum Retexture {
        Auto,
        Vanilla,
        WaybackPT,
        ElleTown,
        YriYellog,
        FlowerValley
    }

    internal sealed class ModEntry : Mod
    {
        public static ModConfig Config = null!;

        /*
         * Controls whether to enable some CP edits for compatibility with the
         * Stardew Valley Reimagined 3 mod.
         * Automatically detected at save load time.
         */
        public static bool CompatSVR3Forest = false;
        /*
         * Suggests which recolor palette to use in certain image assets.
         * Automatically set at save load time.
         */
        public static string RecolorDetected = "Vanilla";
        /*
         * Suggests which recolor palette to use in the interior tilesheet.
         * Automatically set at save load time.
         */
        public static string InteriorDetected = "Vanilla";
        /*
         * Suggests which retexture mod to match for building exteriors.
         * Automatically set at save load time.
         */
        public static string RetextureDetected = "Vanilla";

        /*
         * Set to true when GMCM saves our config during gameplay and any of
         * the appearance settings have been changed.
         * When this happens, we run the console command `patch update` to
         * force a context update.
         */
        public static bool ConfigForcePatchUpdate = false;


        /*
         * Entry point.
         * Sets up the code patches and necessary event handlers.
         * OnSaveLoaded is intended to hotfix problems that can occur on saves
         * created before installing this mod.
         */
        public override void Entry(IModHelper helper)
        {
            HML.Monitor = Monitor;
            HML.Manifest = ModManifest;
            HML.ModHelper = helper;
            ModEntry.Config = helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
            helper.Events.Specialized.LoadStageChanged += this.OnLoadStageChanged;
            helper.Events.Content.AssetRequested += LCCompat.OnAssetRequested;
            helper.ConsoleCommands.Add("lacey_map_repair", "\nReloads Forest map objects in the vicinity of Lacey's cabin,\nto fix the bushes in saves from before installation.\nYou shouldn't need to run this, but it's safe to do so.", this.LaceyMapRepair);
            helper.ConsoleCommands.Add("mousify_child", "\nSets or unsets mouse child status on one of your children.\nUse this if your config settings weren't right and you got the wrong children,\nor just to morph your kids for fun.\n\nUsage: mousify_child <name> <variant>\n    where <variant> is -1 (human), 0 (grey), or 1 (brown).", this.MousifyChild);
            helper.ConsoleCommands.Add("hat_string", "\nprints hat string to console", this.GetHatString);

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
                    if (split.Length < 3) {
                        Log.Warn($"bad Patcher function name '{func.Name}'");
                        continue;
                    }
                    Type t;
                    string fqn;
                    string[] nested = split[0].Split("_nest_");
                    if (nested.Length > 1) {
                        fqn = "StardewValley." + nested[0].Replace("_", ".");
                        t = sdv.GetType(fqn);
                        for (int i = 1; i < nested.Length; ++i) {
                            t = t.GetNestedType(nested[i]);
                            fqn += "+" + nested[i];
                        }
                    }
                    else {
                        fqn = "StardewValley." + split[0].Replace("_", ".");
                        t = sdv.GetType(fqn);
                    }
                    if (t is null) {
                        Log.Warn($"type not found: '{fqn}'");
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
                    MethodInfo m = t.GetMethod(split[1],
                            BindingFlags.Static | BindingFlags.Instance |
                            BindingFlags.Public | BindingFlags.NonPublic,
                            null,
                            args.ToArray(),
                            null);
                    if (m is null) {
                        Log.Warn($"within type '{fqn}': method not found: " +
                                $"'{split[1]}({string.Join(", ", args)})'");
                        continue;
                    }
                    var hm = new HarmonyMethod(typeof(Patcher), func.Name);
                    if (split[2] == "Prefix") {
                        harmony.Patch(original: m, prefix: hm);
                    }
                    else if (split[2] == "Postfix") {
                        harmony.Patch(original: m, postfix: hm);
                    }
                    else {
                        Log.Warn($"Not applying unimplemented patch type '{split[2]}'");
                        continue;
                    }
                    Log.Trace($"Patched ({split[2]}) {t.FullName}.{m.Name}");
                }
            }
            catch (Exception e) {
                Log.Warn($"Caught exception while applying Harmony patches:\n{e}");
            }
        }

        /*
         * Reset terrain features (grass, trees, bushes) around Lacey's cabin
         * by reloading them from the (patched) map data.
         * This is to make sure the save file reflects the final map, even on
         * older saves.
         */
        private void LaceyMapRepair(string command, string[] args)
        {
            Log.Trace($"Reloading terrain features near Lacey's house");
            /* This is the rectangle to reset. It should include every tile
             * that we hit with terrain-feature map patches. */
            var rect = new Microsoft.Xna.Framework.Rectangle(25, 89, 15, 11);
            GameLocation forest = Game1.getLocationFromName("Forest");
            if (forest is null || forest.map is null) {
                return;
            }
            Layer paths = forest.map.GetLayer("Paths");
            if (paths is null) {
                return;
            }
            // forest.largeTerrainFeatures is the bushes
            var largeToRemove = new List<LargeTerrainFeature>();
            foreach (var feature in forest.largeTerrainFeatures) {
                Vector2 pos = feature.Tile;
                if (pos.X >= rect.X && pos.X <= rect.X+rect.Width &&
                        pos.Y >= rect.Y && pos.Y <= rect.Y+rect.Height) {
                    largeToRemove.Add(feature);
                }
            }
            foreach (var doomed in largeToRemove) {
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
            // forest.terrainFeatures includes grass and trees
            var smallToRemove = new List<Vector2>();
            foreach (var feature in forest.terrainFeatures.Pairs) {
                Vector2 pos = feature.Key;
                if ((feature.Value is Grass || feature.Value is Tree) &&
                        pos.X >= rect.X && pos.X <= rect.X+rect.Width &&
                        pos.Y >= rect.Y && pos.Y <= rect.Y+rect.Height) {
                    smallToRemove.Add(pos);
                }
            }
            foreach (var doomed in smallToRemove) {
                forest.terrainFeatures.Remove(doomed);
            }
            for (int x = rect.X; x < rect.X+rect.Width; ++x) {
                for (int y = rect.Y; y < rect.Y+rect.Height; ++y) {
                    Tile t = paths.Tiles[x, y];
                    if (t is null) {
                        continue;
                    }
                    if (t.TileIndex >= 9 && t.TileIndex <= 11) {
                        int treeType = t.TileIndex - 8 +
                                (Game1.currentSeason.Equals("winter") && t.TileIndex < 11 ? 3 : 0);
                        forest.terrainFeatures.Add(new Vector2(x,y),
                                new Tree($"{treeType}", 5));
                    }
                    else if (t.TileIndex == 12) {
                        forest.terrainFeatures.Add(new Vector2(x,y),
                                new Tree("6", 5));
                    }
                    else if (t.TileIndex == 31 || t.TileIndex == 32) {
                        forest.terrainFeatures.Add(new Vector2(x,y),
                                new Tree($"{40 - t.TileIndex}", 5));
                    }
                    else if (t.TileIndex == 22) {
                        forest.terrainFeatures.Add(new Vector2(x,y),
                                new Grass(1, 3));
                    }
                }
            }
        }

        private void MousifyChild(string command, string[] args)
        {
            if (args.Length < 2) {
                Log.Warn($"Usage: mousify_child <name> <variant>");
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
                Log.Warn($"Could not find your child named '{args[0]}'.");
                return;
            }
            string variant = args[1];
            if (variant != "-1" && variant != "0" && variant != "1") {
                Log.Warn($"Unrecognized variant '{variant}'. Using 0 instead.");
                variant = "0";
            }
            child.modData[$"{HML.CPId}/ChildVariant"] = variant;
            child.reloadSprite();
        }

        private void GetHatString(string command, string[] args)
        {
            Log.Warn($"'{LCHatString.GetCurrentHatString(Game1.player)}'");
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            /*
             * Sometimes, Lacey's schedule can fail to load when this mod is
             * newly installed. This will rebuild it when that happens.
             * In practice, on new installs the map repair function will run,
             * and if that's necessary we rebuild Lacey's schedule immediately,
             * meaning this won't do anything.
             */
            NPC Lacey = Game1.getCharacterFromName(HML.LaceyInternalName);
            if (Lacey != null && Lacey.Schedule is null && !Lacey.isMarried()) {
                Log.Trace($"Regenerating Lacey's schedule");
                Lacey.TryLoadSchedule();
            }
        }

        /*
         * Register Content Patcher tokens (for config mirroring).
         * Register GMCM entries.
         */
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            LCEventCommands.Register();
            var cpapi = this.Helper.ModRegistry.GetApi<IContentPatcherAPI>(
                    "Pathoschild.ContentPatcher");
            cpapi.RegisterToken(this.ModManifest, "AlwaysAdopt", () => {
                return new[] {$"{Config.AlwaysAdopt}"};
            });
            cpapi.RegisterToken(this.ModManifest, "DTF", () => {
                return new[] {$"{Config.DTF}"};
            });
            cpapi.RegisterToken(this.ModManifest, "RecolorConfig", () => {
                return new[] {$"{Config.RecolorPalette.ToString()}"};
            });
            cpapi.RegisterToken(this.ModManifest, "InteriorConfig", () => {
                return new[] {$"{Config.InteriorPalette.ToString()}"};
            });
            cpapi.RegisterToken(this.ModManifest, "RetextureConfig", () => {
                return new[] {$"{Config.MatchRetexture.ToString()}"};
            });
            cpapi.RegisterToken(this.ModManifest, "RecolorDetected", () => {
                return new[] {ModEntry.RecolorDetected};
            });
            cpapi.RegisterToken(this.ModManifest, "InteriorDetected", () => {
                return new[] {ModEntry.InteriorDetected};
            });
            cpapi.RegisterToken(this.ModManifest, "RetextureDetected", () => {
                return new[] {ModEntry.RetextureDetected};
            });
            cpapi.RegisterToken(this.ModManifest, "SVRThreeForest", () => {
                return new[] {$"{ModEntry.CompatSVR3Forest}"};
            });
            Log.Trace($"Registered Content Patcher tokens for config options");

            var cmapi = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(
                    "spacechase0.GenericModConfigMenu");
            if (cmapi != null) {
                cmapi.Register(
                    mod: this.ModManifest,
                    reset: () => ModEntry.Config = new ModConfig(),
                    save: () => {
                        this.Helper.WriteConfig(ModEntry.Config);
                        if (Game1.gameMode != Game1.titleScreenGameMode) {
                            LCCompat.DetectModMatching();
                            if (ConfigForcePatchUpdate) {
                                LCCompat.QueueConsoleCommand.Value("patch update");
                            }
                        }
                        ConfigForcePatchUpdate = false;
                    }
                );
                cmapi.AddSectionTitle(
                    mod: this.ModManifest,
                    text: () => this.Helper.Translation.Get("gmcm.contentsection.text"),
                    tooltip: null
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
                cmapi.AddSectionTitle(
                    mod: this.ModManifest,
                    text: () => this.Helper.Translation.Get("gmcm.appearancesection.text"),
                    tooltip: null
                );
                string[] colorNames = Enum.GetNames<Palette>();
                cmapi.AddTextOption(
                    mod: this.ModManifest,
                    name: () => "RecolorPalette",
                    tooltip: () => this.Helper.Translation.Get("gmcm.recolorpalette.tooltip"),
                    allowedValues: colorNames,
                    getValue: () => Config.RecolorPalette.ToString(),
                    setValue: value => {
                        var v = (Palette)Enum.Parse(typeof(Palette), value);
                        if (Config.RecolorPalette != v) {
                            ConfigForcePatchUpdate = true;
                        }
                        Config.RecolorPalette = v;
                    }
                );
                List<string> trimmed = new List<string>(colorNames);
                trimmed.Remove("Wittily");
                cmapi.AddTextOption(
                    mod: this.ModManifest,
                    name: () => "InteriorPalette",
                    tooltip: () => this.Helper.Translation.Get("gmcm.interiorpalette.tooltip"),
                    allowedValues: trimmed.ToArray(),
                    getValue: () => Config.InteriorPalette.ToString(),
                    setValue: value => {
                        var v = (Palette)Enum.Parse(typeof(Palette), value);
                        if (Config.InteriorPalette != v) {
                            ConfigForcePatchUpdate = true;
                        }
                        Config.InteriorPalette = v;
                    }
                );
                cmapi.AddTextOption(
                    mod: this.ModManifest,
                    name: () => "MatchRetexture",
                    tooltip: () => this.Helper.Translation.Get("gmcm.matchretexture.tooltip"),
                    allowedValues: Enum.GetNames<Retexture>(),
                    getValue: () => Config.MatchRetexture.ToString(),
                    setValue: value => {
                        var v = (Retexture)Enum.Parse(typeof(Retexture), value);
                        if (Config.MatchRetexture != v) {
                            ConfigForcePatchUpdate = true;
                        }
                        Config.MatchRetexture = v;
                    }
                );
                Log.Trace($"Registered Generic Mod Config Menu entries");
            }
        }

        /*
         * Clear out savefile-specific cached data.
         * Stop the event background ticker, just in case it's running.
         */
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            LCModData.ClearCache();
            LCEventCommands.stopTicker();
        }

        /*
         * Special stuff which has to run during the save load for technical
         * reasons (typically to preempt loading the maps to completion).
         *   - Snarf other mod data and set CP tokens
         *   - Migrate 1.5 Lacey data to 1.6
         *   - Run the map repair function if needed
         */
        private void OnLoadStageChanged(object sender, LoadStageChangedEventArgs e)
        {
            // This early stage is suitable for checking the status of other
            // mods and enabling the appropriate compatibility patches (by
            // setting tokens for the CP pack to use).
            if (e.NewStage == LoadStage.CreatedInitialLocations ||
                    e.NewStage == LoadStage.SaveLoadedBasicInfo) {
                try {
                    var modInfo = HML.ModHelper.ModRegistry.Get("DaisyNiko.SVR3");
                    var modPath = (string)modInfo.GetType().GetProperty("DirectoryPath")
                        .GetValue(modInfo);
                    var jConfig = JObject.Parse(File.ReadAllText(Path.Combine(modPath, "config.json")));
                    var forest = jConfig.GetValue("Forest").Value<string>();
                    ModEntry.CompatSVR3Forest = (forest == "on");
                }
                catch {
                    ModEntry.CompatSVR3Forest = false;
                }

                LCCompat.DetectModMatching();
            }
            // Migrate 1.5 Lacey data to the new internal names for 1.6.
            // Naturally, this applies only when loading existing saves, and
            // not when creating new ones.
            if (e.NewStage == LoadStage.SaveLoadedBasicInfo) {
                LCSaveMigrator save = new();
                save.MigrateOldSaveData();
            }
            // Check the Forest map to see if specific terrain features which
            // should be gone are still around. If they are, run the map
            // repair function to clean up.
            if (e.NewStage == LoadStage.Preloaded) {
                GameLocation forest = Game1.getLocationFromName("Forest");
                if (forest != null) {
                    bool doClean = false;
                    if (forest.terrainFeatures.ContainsKey(new Vector2(29f, 97f))) {
                        doClean = true;
                    }
                    if (!doClean) {
                        foreach (var feature in forest.largeTerrainFeatures) {
                            if (feature.Tile == new Vector2(29f, 96f)) {
                                doClean = true;
                                break;
                            }
                        }
                    }
                    if (doClean) {
                        LaceyMapRepair("", null);
                        // also rebuild Lacey's schedule, since the features
                        // have changed and will affect pathing.
                        NPC Lacey = Game1.getCharacterFromName(HML.LaceyInternalName);
                        if (Lacey != null) {
                            Lacey.TryLoadSchedule();
                        }
                    }
                }
            }
        }

    }
}
