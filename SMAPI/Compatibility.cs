using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using xTile;
using xTile.Layers;
using xTile.Tiles;

namespace ichortower_HatMouseLacey;

internal class LCCompat
{
    //
    // All of these fields and properties are lazy loaders for GetToken
    //
    private static Mod _cpModRef = null;
    private static Type _tokenStringType = null;
    private static Type _logPathBuilderType = null;
    private static MethodInfo _getContextFor = null;

    internal static object TokenManagerRef = null;

    internal static Mod CpModRef {
        get {
            if (_cpModRef is null) {
                var mi = HML.ModHelper.ModRegistry.Get("Pathoschild.ContentPatcher");
                _cpModRef = mi.GetType().GetProperty("Mod",
                        BindingFlags.Public | BindingFlags.Instance)
                        .GetValue(mi) as Mod;
            }
            return _cpModRef;
        }
    }

    internal static Type TokenStringType {
        get {
            _tokenStringType ??= CpModRef.GetType().Assembly.GetType(
                    "ContentPatcher.Framework.Conditions.TokenString");
            return _tokenStringType;
        }
    }

    internal static Type LogPathBuilderType {
        get {
            _logPathBuilderType ??= CpModRef.GetType().Assembly.GetType(
                    "ContentPatcher.Framework.LogPathBuilder");
            return _logPathBuilderType;
        }
    }

    internal static MethodInfo GetContextFor {
        get {
            if (_getContextFor is null) {
                object screenManField = CpModRef.GetType().GetField("ScreenManager",
                        BindingFlags.NonPublic | BindingFlags.Instance).GetValue(CpModRef);
                object screenManValue = screenManField.GetType().GetProperty("Value",
                        BindingFlags.Public | BindingFlags.Instance).GetValue(screenManField);
                TokenManagerRef = screenManValue.GetType().GetProperty("TokenManager",
                        BindingFlags.Public | BindingFlags.Instance).GetValue(screenManValue);
                _getContextFor = TokenManagerRef.GetType().GetMethod("GetContextFor",
                        BindingFlags.Public | BindingFlags.Instance);
            }
            return _getContextFor;
        }
    }

    /*
     * Parse a token from a Content Patcher pack and return it as a string.
     * See above for most of the reflection crimes involved in doing this.
     */
    public static string GetToken(string modId, string key)
    {
        try {
            object lpb = Activator.CreateInstance(LogPathBuilderType, new object[] {"none"});
            // DoNotWrapExceptions here lets us catch different types
            object context = GetContextFor.Invoke(TokenManagerRef,
                    BindingFlags.DoNotWrapExceptions, null, new object[] {modId}, null);
            object tokstr = Activator.CreateInstance(TokenStringType, new object[] {"{{" + key + "}}", context, lpb});
            return tokstr.ToString();
        }
        // this can be thrown by GetContextFor when CP hasn't finished initializing
        // (e.g. in GameLaunched). ignore and return null; in our use case it will
        // be refreshed later, so it's harmless
        catch (KeyNotFoundException) {
        }
        catch (Exception e) {
            Log.Warn(e.ToString());
        }
        return null;
    }

    /*
     * Parse a config.json from another mod.
     * Returns true if the mod is installed, and false otherwise.
     * `config` will be null if the mod isn't installed, and an empty object
     * if the requested mod has no config.json.
     *
     * NOTE this relies on knowing the standard location for the file and uses
     * System APIs to read it, which may become disallowed in the future.
     * But there's no official channel for this, and SMAPI mods and content
     * packs have different stuff to reflect into, and it's gnarly, so this way
     * remains for now.
     */
    internal static bool TryGetConfig(string modId, out JObject config)
    {
        var modInfo = HML.ModHelper.ModRegistry.Get(modId);
        if (modInfo is null) {
            config = null;
            return false;
        }
        try {
            string baseDir = modInfo.GetType().GetProperty("DirectoryPath")
                    .GetValue(modInfo) as string;
            config = JObject.Parse(File.ReadAllText(Path.Combine(baseDir, "config.json")));
        }
        catch {
            config = new();
        }
        return true;
    }

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
    public static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
    {
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
                // -1 in the value here means delete (null)
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
                // saved delegate here for if the other two layers are needed
                // Action<Vector2, Layer> mutate = delegate(Vector2 coords, Layer layer)
                foreach (var coords in backList) {
                    Tile t = back.Tiles[(int)coords.X, (int)coords.Y];
                    if (t is null) {
                        continue;
                    }
                    int target;
                    if (!convertDict.TryGetValue(t.TileIndex, out target)) {
                        continue;
                    }
                    // delete if the value is -1, as above
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
        ModEntry.RecolorDetected = "Vanilla";
        ModEntry.InteriorDetected = "Vanilla";
        ModEntry.RetextureDetected = "Vanilla";
        ModEntry.PortraitStyleDetected = "Nouveau";

        string nyapu = "Nyapu.Portraits";
        if (HML.ModHelper.ModRegistry.Get(nyapu) != null) {
            Log.Trace($"Found mod '{nyapu}'. Setting detected " +
                    "portrait style to 'Nyapu'.");
            ModEntry.PortraitStyleDetected = "Nyapu";
        }

        Dictionary<string, string> recolorMods = new() {
            {"DaisyNiko.EarthyRecolour", "Earthy"},
            {"grapeponta.VibrantPastoralRecolor", "VPR"},
            {"Lita.StarblueValley", "Starblue"},
            {"Lita.StarblueValleyUnofficial", "Starblue"},
            {"Acerbicon.Recolor", "Wittily"},
        };
        foreach (var pair in recolorMods) {
            var modInfo = HML.ModHelper.ModRegistry.Get(pair.Key);
            if (modInfo != null) {
                Log.Trace($"Found mod '{pair.Key}'. Setting detected " +
                        $"palette to '{pair.Value}'.");
                ModEntry.RecolorDetected = pair.Value;
                break;
            }
        }

        // interior recoloring is more complicated. each mod does it
        // differently, and wittily doesn't do it at all
        Dictionary<string, string> interiorMods = new() {
            {"DaisyNiko.EarthyInteriors", "Earthy"},
            {"VibrantPastoral.C", "Interiors:true:VPR"},
            {"Lita.StarblueValley", "Interiors:true:Starblue"},
            {"Lita.StarblueValleyUnofficial", "Interiors:true:Starblue"},
        };
        foreach (var pair in interiorMods) {
            var split = pair.Value.Split(":");
            var modInfo = HML.ModHelper.ModRegistry.Get(pair.Key);
            if (modInfo != null) {
                if (split.Length == 1) {
                    Log.Trace($"Found mod '{pair.Key}'. Setting detected " +
                            $"interior palette to '{split[0]}'.");
                    ModEntry.InteriorDetected = split[0];
                    break;
                }
                if (split.Length != 3) {
                    Log.Warn("Found bad interior detection format: " +
                            $"'{pair.Key}' -> '{pair.Value}'. " +
                            "Expected 1 or 3 fields in value. Skipping.");
                    continue;
                }
                try {
                    var modPath = (string)modInfo.GetType()
                            .GetProperty("DirectoryPath").GetValue(modInfo);
                    var jConfig = JObject.Parse(File.ReadAllText(
                            Path.Combine(modPath, "config.json")));
                    var cvalue = jConfig.GetValue(split[0])
                            .Value<string>();
                    if (cvalue.Equals(split[1], StringComparison.OrdinalIgnoreCase)) {
                        Log.Trace($"Found active mod '{pair.Key}'. Setting " +
                                $"detected interior palette to '{split[2]}'.");
                        ModEntry.InteriorDetected = split[2];
                        break;
                    }
                }
                catch (Exception e) {
                    Log.Warn("Caught exception trying to read config for " +
                            $"'{pair.Key}': {e}");
                }
            }
        }

        // retextures work like interior recolors: only some use config
        // values.
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
            var modInfo = HML.ModHelper.ModRegistry.Get(pair.Key);
            if (modInfo != null) {
                if (split.Length == 1) {
                    Log.Trace($"Found mod '{pair.Key}'. Setting detected" +
                            $" retexture to '{split[0]}'.");
                    ModEntry.RetextureDetected = split[0];
                    break;
                }
                if (split.Length != 3) {
                    Log.Warn("Found bad retexture detection format: " +
                            $"'{pair.Key}' -> '{pair.Value}'. " +
                            "Expected 1 or 3 fields in value. Skipping.");
                    continue;
                }
                try {
                    var modPath = (string)modInfo.GetType()
                            .GetProperty("DirectoryPath").GetValue(modInfo);
                    var jConfig = JObject.Parse(File.ReadAllText(
                            Path.Combine(modPath, "config.json")));
                    var cvalue = jConfig.GetValue(split[0])
                            .Value<string>();
                    if (cvalue.Equals(split[1], StringComparison.OrdinalIgnoreCase)) {
                        Log.Trace($"Found active mod '{pair.Key}'. Setting" +
                                $" detected retexture to '{split[2]}'.");
                        ModEntry.RetextureDetected = split[2];
                        break;
                    }
                }
                catch (Exception e) {
                    Log.Warn("Caught exception trying to read config for " +
                            $"'{pair.Key}': {e}");
                }
            }
        }
    }

    /*
     * Big thanks to Shockah for this one. Wizardry
     */
    public static Lazy<Action<string>> QueueConsoleCommand = new(() => {
        var sCoreType = Type.GetType(
                "StardewModdingAPI.Framework.SCore,StardewModdingAPI")!;
        var commandQueueType = Type.GetType(
                "StardewModdingAPI.Framework.CommandQueue,StardewModdingAPI")!;
        var sCoreGetter = sCoreType.GetProperty("Instance",
                BindingFlags.NonPublic | BindingFlags.Static).GetGetMethod(true);
        var rawCommandQueueField = sCoreType.GetField("RawCommandQueue",
                BindingFlags.NonPublic | BindingFlags.Instance);
        var queueAddMethod = commandQueueType.GetMethod("Add",
                BindingFlags.Public | BindingFlags.Instance);

        var method = new DynamicMethod("QueueConsoleCommand",
                null, new Type[] {typeof(string)});
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Call, sCoreGetter);
        il.Emit(OpCodes.Ldfld, rawCommandQueueField);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, queueAddMethod);
        il.Emit(OpCodes.Ret);
        return method.CreateDelegate<Action<string>>();
    });

} // LCCompat
