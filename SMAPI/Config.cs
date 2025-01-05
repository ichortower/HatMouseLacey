using GenericModConfigMenu;
using StardewValley;
using System;
using System.Collections.Generic;

namespace ichortower_HatMouseLacey;

internal sealed class ModConfig
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

    /*
     * PortraitStyle controls which set of portrait images to use. Auto
     * will choose Nouveau or Nyapu, depending on whether Nyapu's portraits
     * are installed.
     */
    public Portraits PortraitStyle = Portraits.Auto;

    /*
     * SeasonalOutfits controls whether Lacey's summer and fall outfits are
     * enabled. Spring is the default outfit, and winter isn't available
     * to control since it's vanilla behavior.
     */
    public bool SeasonalOutfits { get; set; } = false;

     /*
      * WeddingAttire lets you choose what Lacey will wear when you marry
      * her: Dress or Tuxedo.
      */
     public Outfit WeddingAttire = Outfit.Dress;

     /*
      * MarkUnseenHats controls whether to draw a little icon of Lacey's
      * face on hats she has not commented on.
      */
     public bool MarkUnseenHats = true;
}

internal enum Palette {
    Auto,
    Vanilla,
    Earthy,
    VPR,
    Starblue,
    Wittily,
}

internal enum Retexture {
    Auto,
    Vanilla,
    WaybackPT,
    ElleTown,
    YriYellog,
    FlowerValley,
}

internal enum Portraits {
    Auto,
    Nouveau,
    Nyapu,
    Classic,
}

internal enum Outfit {
    Dress,
    Tuxedo,
}


internal sealed class LCConfig
{
    /*
     * Set to true when GMCM saves our config during gameplay and any of
     * the appearance settings have been changed.
     * When this happens, we run the console command `patch update` to
     * force a context update.
     */
    public static bool ConfigForcePatchUpdate = false;

    /*
     * Set to true when GMCM saves our config during gameplay and the
     * SeasonalOutfits option, specifically, has been changed.
     * When this happens, we tell Lacey to choose a new outfit right away.
     */
    public static bool ConfigForceClothesChange = false;

    public static void Register()
    {
        var cmapi = HML.ModHelper.ModRegistry.GetApi<IGenericModConfigMenuApi>(
                "spacechase0.GenericModConfigMenu");
        if (cmapi is null) {
            return;
        }

        cmapi.Register(
            mod: HML.Manifest,
            reset: () => ModEntry.Config = new ModConfig(),
            save: () => {
                HML.ModHelper.WriteConfig(ModEntry.Config);
                if (Game1.gameMode != Game1.titleScreenGameMode) {
                    LCCompat.DetectModMatching();
                    if (ConfigForcePatchUpdate) {
                        LCCompat.QueueConsoleCommand.Value("patch update");
                    }
                    // queue this command instead of calling it directly;
                    // it needs to wait until the patch update is done
                    if (ConfigForceClothesChange) {
                        LCCompat.QueueConsoleCommand.Value("hatmouselacey change_clothes");
                    }
                }
                ConfigForcePatchUpdate = false;
                ConfigForceClothesChange = false;
            }
        );
        cmapi.AddSectionTitle(
            mod: HML.Manifest,
            text: () => TR.Get("gmcm.contentsection.text"),
            tooltip: null
        );
        cmapi.AddBoolOption(
            mod: HML.Manifest,
            name: () => "AlwaysAdopt",
            tooltip: () => TR.Get("gmcm.alwaysadopt.tooltip"),
            getValue: () => ModEntry.Config.AlwaysAdopt,
            setValue: value => ModEntry.Config.AlwaysAdopt = value
        );
        cmapi.AddBoolOption(
            mod: HML.Manifest,
            name: () => "DTF",
            tooltip: () => TR.Get("gmcm.dtf.tooltip"),
            getValue: () => ModEntry.Config.DTF,
            setValue: value => ModEntry.Config.DTF = value
        );
        cmapi.AddSectionTitle(
            mod: HML.Manifest,
            text: () => TR.Get("gmcm.appearancesection.text"),
            tooltip: null
        );
        cmapi.AddTextOption(
            mod: HML.Manifest,
            name: () => "PortraitStyle",
            tooltip: () => TR.Get("gmcm.portraitstyle.tooltip"),
            allowedValues: Enum.GetNames<Portraits>(),
            getValue: () => ModEntry.Config.PortraitStyle.ToString(),
            setValue: value => {
                var v = (Portraits)Enum.Parse(typeof(Portraits), value);
                if (v == Portraits.Auto || ModEntry.Config.PortraitStyle != v) {
                    ConfigForcePatchUpdate = true;
                }
                ModEntry.Config.PortraitStyle = v;
            }
        );
        string[] colorNames = Enum.GetNames<Palette>();
        cmapi.AddTextOption(
            mod: HML.Manifest,
            name: () => "RecolorPalette",
            tooltip: () => TR.Get("gmcm.recolorpalette.tooltip"),
            allowedValues: colorNames,
            getValue: () => ModEntry.Config.RecolorPalette.ToString(),
            setValue: value => {
                var v = (Palette)Enum.Parse(typeof(Palette), value);
                if (v == Palette.Auto || ModEntry.Config.RecolorPalette != v) {
                    ConfigForcePatchUpdate = true;
                }
                ModEntry.Config.RecolorPalette = v;
            }
        );
        List<string> trimmed = new List<string>(colorNames);
        trimmed.Remove("Wittily");
        cmapi.AddTextOption(
            mod: HML.Manifest,
            name: () => "InteriorPalette",
            tooltip: () => TR.Get("gmcm.interiorpalette.tooltip"),
            allowedValues: trimmed.ToArray(),
            getValue: () => ModEntry.Config.InteriorPalette.ToString(),
            setValue: value => {
                var v = (Palette)Enum.Parse(typeof(Palette), value);
                if (v == Palette.Auto || ModEntry.Config.InteriorPalette != v) {
                    ConfigForcePatchUpdate = true;
                }
                ModEntry.Config.InteriorPalette = v;
            }
        );
        cmapi.AddTextOption(
            mod: HML.Manifest,
            name: () => "MatchRetexture",
            tooltip: () => TR.Get("gmcm.matchretexture.tooltip"),
            allowedValues: Enum.GetNames<Retexture>(),
            getValue: () => ModEntry.Config.MatchRetexture.ToString(),
            setValue: value => {
                var v = (Retexture)Enum.Parse(typeof(Retexture), value);
                if (v == Retexture.Auto || ModEntry.Config.MatchRetexture != v) {
                    ConfigForcePatchUpdate = true;
                }
                ModEntry.Config.MatchRetexture = v;
            }
        );

        cmapi.AddSectionTitle(
            mod: HML.Manifest,
            text: () => TR.Get("gmcm.outfitssection.text"),
            tooltip: null
        );
        cmapi.AddBoolOption(
            mod: HML.Manifest,
            name: () => "SeasonalOutfits",
            tooltip: () => TR.Get("gmcm.seasonaloutfits.tooltip"),
            getValue: () => ModEntry.Config.SeasonalOutfits,
            setValue: value => {
                if (ModEntry.Config.SeasonalOutfits != value) {
                    ConfigForcePatchUpdate = true;
                    ConfigForceClothesChange = true;
                }
                ModEntry.Config.SeasonalOutfits = value;
            }
        );
        cmapi.AddTextOption(
            mod: HML.Manifest,
            name: () => "WeddingAttire",
            tooltip: () => TR.Get("gmcm.weddingattire.tooltip"),
            allowedValues: Enum.GetNames<Outfit>(),
            getValue: () => ModEntry.Config.WeddingAttire.ToString(),
            setValue: value => {
                var v = (Outfit)Enum.Parse(typeof(Outfit), value);
                if (ModEntry.Config.WeddingAttire != v) {
                    ConfigForcePatchUpdate = true;
                }
                ModEntry.Config.WeddingAttire = v;
            }
        );

        cmapi.AddSectionTitle(
            mod: HML.Manifest,
            text: () => TR.Get("gmcm.othersection.text"),
            tooltip: null
        );
        cmapi.AddBoolOption(
            mod: HML.Manifest,
            name: () => "MarkUnseenHats",
            tooltip: () => TR.Get("gmcm.markunseenhats.tooltip"),
            getValue: () => ModEntry.Config.MarkUnseenHats,
            setValue: value => {
                ModEntry.Config.MarkUnseenHats = value;
            }
        );
        Log.Trace($"Registered Generic Mod Config Menu entries");
    }
}

