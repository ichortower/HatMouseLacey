using ContentPatcher;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Enums;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace ichortower_HatMouseLacey
{
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
         * Suggests which portrait style to load. Automatically set at
         * save load time.
         */
        public static string PortraitStyleDetected = "Nouveau";



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
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
            helper.Events.Specialized.LoadStageChanged += this.OnLoadStageChanged;
            helper.Events.Content.AssetRequested += LCCompat.OnAssetRequested;

            // see ConsoleCommands.cs
            helper.ConsoleCommands.Add(HML.CommandWord,
                    $"Run a Hat Mouse Lacey command. '{HML.CommandWord} help' for details.",
                    ConsoleCommands.Main);

            GameLocation.RegisterTileAction($"{HML.CPId}_PhotoMessage",
                    this.PhotoMessage);
            GameLocation.RegisterTileAction($"{HML.CPId}_HatRegistry",
                    this.HatRegistry);

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


        private bool PhotoMessage(GameLocation location, string[] args,
                Farmer player, Point tile)
        {
            if (args.Length != 3 && args.Length != 5) {
                Log.Error($"'{args[0]}': incorrect argument count" +
                        $" (expected 3 or 5, got {args.Length})");
                return false;
            }
            int x = tile.X;
            int y = tile.Y;
            int dx = 0;
            int dy = 0;
            string err;
            if (args.Length == 5 &&
                    (!ArgUtility.TryGetInt(args, 3, out dx, out err) ||
                     !ArgUtility.TryGetInt(args, 4, out dy, out err))) {
                Log.Error($"{args[0]}': parse failure: {err}");
                return false;
            }
            x += dx;
            y += dy;
            // 0: action type
            // 1: image asset (game asset path)
            // 2: text key (from StringsFromMaps)
            string key = args[2].Replace("\"", "");
            if (key != "") {
                key = "Strings\\StringsFromMaps:" + key;
            }
            ImageDialog img = new(x, y, args[1].Replace("\"", ""), key);
            Game1.activeClickableMenu = img;
            return true;
        }

        private bool HatRegistry(GameLocation location, string[] args,
                Farmer player, Point tile)
        {
            string asset = "Strings\\StringsFromMaps";
            bool enabled = player.hasOrWillReceiveMail($"{HML.MailPrefix}HatReactions");
            bool isSpouse = (player.getSpouse()?.Name.Equals(HML.LaceyInternalName) == true);
            string key = $"{HML.CPId}.HatRegistry.Inspect";
            if (isSpouse) {
                key += "Spouse";
            }
            if (!enabled) {
                key += "Disabled";
            }

            Action proceedToRegistry = delegate {
                Game1.afterFadeFunction openMenu = delegate {
                    if (enabled) {
                        Game1.activeClickableMenu = new HatRegistryMenu();
                    }
                };

                string messageText = Game1.content.LoadStringReturnNullIfNotFound($"{asset}:{key}");
                if (messageText is null) {
                    openMenu();
                }
                else {
                    Game1.drawDialogueNoTyping(messageText);
                    Game1.afterDialogues = openMenu;
                }
            };
            if (!isSpouse) {
                proceedToRegistry();
                return true;
            }

            var choices = new Response[] {
                new("shop", Helper.Translation.Get("hatreactions.menu.ShopChoice")),
                new("registry", Helper.Translation.Get("hatreactions.menu.RegistryChoice")),
                new("cancel", Helper.Translation.Get("hatreactions.menu.CancelChoice")),
            };
            var actions = new Action[] {
                () => {
                    Utility.TryOpenShopMenu("HatMouse",
                            Game1.currentLocation, playOpenSound: true);
                },
                proceedToRegistry,
                () => {}
            };
            int width = 600;
            for (int i = 0; i < choices.Length; ++i) {
                width = Math.Max(width, SpriteText.getWidthOfString(choices[i].responseText)+128);
            }
            Game1.drawObjectQuestionDialogue("", choices, width+64);
            Game1.currentLocation.afterQuestion = delegate (Farmer who, string whichAnswer) {
                for (int i = 0; i < choices.Length; ++i) {
                    if (choices[i].responseKey.Equals(whichAnswer)) {
                        actions[i]();
                        return;
                    }
                }
            };
            return true;
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

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            Patcher.showedAHatToday = false;
            string registryMail = $"{HML.MailPrefix}HatRegistryNotice";
            string registryDialogueKey = "Characters\\Dialogue\\MarriageDialogue" +
                    $"{HML.LaceyInternalName}:HatRegistryNew";
            if (LCModData.HatsShown(Game1.player).Count == 0) {
                return;
            }
            if (Game1.player.hasOrWillReceiveMail(registryMail)) {
                return;
            }
            NPC spouse = Game1.player.getSpouse();
            if (spouse?.Name.Equals(HML.LaceyInternalName) == true) {
                Game1.player.mailReceived.Add(registryMail);
                Dialogue say = Dialogue.FromTranslation(spouse, registryDialogueKey);
                spouse.CurrentDialogue.Push(say);
            }
            else {
                Game1.player.mailbox.Add(registryMail);
            }
        }

        /*
         * Register Content Patcher tokens (for config mirroring).
         * Register GMCM entries.
         */
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            LCEventCommands.Register();
            LCEventCommands.RegisterPreconditions();
            LCActions.Register();
            LCConfig.Register();
            LCGameStateQueries.Register();
            var cpapi = this.Helper.ModRegistry.GetApi<IContentPatcherAPI>(
                    "Pathoschild.ContentPatcher");
            cpapi.RegisterToken(this.ModManifest, "ChildPolicy", () => {
                return new[] {$"{Config.ChildPolicy.ToString()}"};
            });
            cpapi.RegisterToken(this.ModManifest, "DTF", () => {
                return new[] {$"{Config.DTF}"};
            });
            cpapi.RegisterToken(this.ModManifest, "PortraitStyleConfig", () => {
                return new[] {$"{Config.PortraitStyle.ToString()}"};
            });
            cpapi.RegisterToken(this.ModManifest, "WeddingAttire", () => {
                return new[] {$"{Config.WeddingAttire.ToString()}"};
            });
            cpapi.RegisterToken(this.ModManifest, "SeasonalOutfits", () => {
                return new[] {$"{Config.SeasonalOutfits}"};
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
            cpapi.RegisterToken(this.ModManifest, "PortraitStyleDetected", () => {
                return new[] {ModEntry.PortraitStyleDetected};
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
            cpapi.RegisterToken(this.ModManifest, "EvenFestivalWorkaround", () => {
                return new[] {$"{Utility.CompareGameVersions(Game1.version, "1.6.4") < 0}"};
            });
            Log.Trace($"Registered Content Patcher tokens for config options");
            cpapi.RegisterToken(this.ModManifest, "FatherName", () => new[]{"Fletcher"});
            cpapi.RegisterToken(this.ModManifest, "MotherName", () => new[]{"Diana"});
            cpapi.RegisterToken(this.ModManifest, "SisterName", () => new[]{"Melody"});
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
                    if (modInfo is null) ModEntry.CompatSVR3Forest = false;
                    else
                    {
                        var modPath = (string)modInfo.GetType().GetProperty("DirectoryPath")
                          .GetValue(modInfo);
                        var jConfig = JObject.Parse(File.ReadAllText(Path.Combine(modPath, "config.json")));
                        var forest = jConfig.GetValue("Forest").Value<string>();
                        ModEntry.CompatSVR3Forest = (forest == "on");
                    }
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
                        ConsoleCommands.MapRepair("", null);
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
