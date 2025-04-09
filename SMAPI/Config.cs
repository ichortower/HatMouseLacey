using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.IO;
using System.Collections.Generic;

namespace ichortower_HatMouseLacey;

internal sealed class ModConfig
{
    /*
     * ChildStrategy decides how to handle any children the farmer has with
     * Lacey. The default value is "ByGender", which means Lacey can get
     * pregnant with a male farmer. If set to "AlwaysAdopt", Lacey will always
     * ask to adopt children, and if set to "AlwaysPregnant", Lacey will always
     * get pregnant, no matter what the farmer's gender is.
     *
     * If Lacey gets pregnant and gives birth to children, they will be mice.
     * Adopted children will be adopted humans. This has no effect on any other
     * player children.
     */
    public ChildStrategy ChildStrategy { get; set; } = ChildStrategy.ByGender;

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

     /*
      * CollapseHatRegistry controls whether the hat registry menu will
      * eliminate "copies" of modded hats by not displaying any hats which
      * give the same reaction as another hat.
      *
      * Many hat mods add lots of variations on the same hat, and Lacey gives
      * the same reaction to all of them, so this aims to reduce clutter.
      */
     public bool CollapseHatRegistry = true;
}

internal enum ChildStrategy {
    ByGender,
    AlwaysAdopt,
    AlwaysPregnant,
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

        cmapi.AddTextOption(
            mod: HML.Manifest,
            name: () => TR.Get("gmcm.childstrategy.name"),
            fieldId: "ChildStrategy",
            tooltip: () => TR.Get("gmcm.childstrategy.tooltip"),
            allowedValues: Enum.GetNames<ChildStrategy>(),
            getValue: () => ModEntry.Config.ChildStrategy.ToString(),
            setValue: value => {
                var v = (ChildStrategy)Enum.Parse(typeof(ChildStrategy), value);
                if (ModEntry.Config.ChildStrategy != v) {
                    ConfigForcePatchUpdate = true;
                }
                ModEntry.Config.ChildStrategy = v;
            }
        );
        cmapi.AddBoolOption(
            mod: HML.Manifest,
            name: () => TR.Get("gmcm.dtf.name"),
            tooltip: () => TR.Get("gmcm.dtf.tooltip"),
            getValue: () => ModEntry.Config.DTF,
            setValue: value => ModEntry.Config.DTF = value
        );

        // There is an unavoidable 16px padding on GMCM's table rows, so putting these in
        // creates a gap in the row spacing even though they render out of flow
        cmapi.AddComplexOption(
            mod: HML.Manifest,
            name: () => "",
            draw: PortraitPreviewer.Draw,
            beforeMenuClosed: PortraitPreviewer.Unload
        );
        cmapi.AddComplexOption(
            mod: HML.Manifest,
            name: () => "",
            draw: OutfitPreviewer.Draw,
            beforeMenuClosed: OutfitPreviewer.Unload
        );
        cmapi.AddComplexOption(
            mod: HML.Manifest,
            name: () => "",
            draw: ChildPreviewer.Draw,
            beforeMenuClosed: ChildPreviewer.Unload
        );

        cmapi.AddSectionTitle(
            mod: HML.Manifest,
            text: () => TR.Get("gmcm.appearancesection.text"),
            tooltip: null
        );

        cmapi.AddBoolOption(
            mod: HML.Manifest,
            name: () => TR.Get("gmcm.seasonaloutfits.name"),
            fieldId: "SeasonalOutfits",
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
            name: () => TR.Get("gmcm.portraitstyle.name"),
            fieldId: "PortraitStyle",
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
        cmapi.AddTextOption(
            mod: HML.Manifest,
            name: () => TR.Get("gmcm.weddingattire.name"),
            fieldId: "WeddingAttire",
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
        string[] colorNames = Enum.GetNames<Palette>();
        cmapi.AddTextOption(
            mod: HML.Manifest,
            name: () => TR.Get("gmcm.recolorpalette.name"),
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
            name: () => TR.Get("gmcm.interiorpalette.name"),
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
            name: () => TR.Get("gmcm.matchretexture.name"),
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
            text: () => TR.Get("gmcm.othersection.text"),
            tooltip: null
        );
        cmapi.AddBoolOption(
            mod: HML.Manifest,
            name: () => TR.Get("gmcm.markunseenhats.name"),
            tooltip: () => TR.Get("gmcm.markunseenhats.tooltip"),
            getValue: () => ModEntry.Config.MarkUnseenHats,
            setValue: value => {
                ModEntry.Config.MarkUnseenHats = value;
            }
        );
        cmapi.AddBoolOption(
            mod: HML.Manifest,
            name: () => TR.Get("gmcm.collapsehatregistry.name"),
            tooltip: () => TR.Get("gmcm.collapsehatregistry.tooltip"),
            getValue: () => ModEntry.Config.CollapseHatRegistry,
            setValue: value => {
                ModEntry.Config.CollapseHatRegistry = value;
            }
        );
        cmapi.OnFieldChanged(
            mod: HML.Manifest,
            onChange: UpdatePreviews
        );

        PortraitPreviewer.Unload();
        // this could go in unload, but it can't change without restarting the
        // game so checking just once, here, is fine
        PortraitPreviewer.HasNyapu = (HML.ModHelper.ModRegistry
                .Get("Nyapu.Portraits") != null);
        OutfitPreviewer.Unload();
        ChildPreviewer.Unload();

        Log.Trace($"Registered Generic Mod Config Menu entries");
    }

    public static void UpdatePreviews(string fieldId, object newValue)
    {
        if (fieldId == "PortraitStyle") {
            PortraitPreviewer.Type = (Portraits)Enum.Parse(typeof(Portraits),
                    (string)newValue);
        }
        else if (fieldId == "SeasonalOutfits") {
            OutfitPreviewer.SeasonalsOn = (bool)newValue;
        }
        else if (fieldId == "WeddingAttire") {
            OutfitPreviewer.WeddingAttire = (Outfit)Enum.Parse(typeof(Outfit),
                    (string)newValue);
        }
        else if (fieldId == "ChildStrategy") {
            ChildPreviewer.Type = (ChildStrategy)Enum.Parse(typeof(ChildStrategy),
                    (string)newValue);
        }
    }
}

internal sealed class PortraitPreviewer
{
    private static Texture2D _Nouveau;
    private static Texture2D _Nyapu;
    private static Texture2D _Classic;

    private static void LoadPortraits()
    {
        if (_Nouveau != null && _Nyapu != null && _Classic != null) {
            return;
        }
        var modInfo = HML.ModHelper.ModRegistry.Get(HML.CPId);
        if (modInfo is null) {
            Log.Error("Fatal: Hat Mouse Lacey is not installed correctly.");
            _Nouveau = Game1.mouseCursors;
            _Nyapu = Game1.mouseCursors;
            _Classic = Game1.mouseCursors;
            return;
        }
        string modPath = (string)modInfo.GetType().GetProperty("DirectoryPath")
                .GetValue(modInfo);
        _Nouveau = Texture2D.FromFile(Game1.graphics.GraphicsDevice,
                Path.Combine(modPath, "assets/character/portraits_nouveau_default.png"));
        _Nyapu = Texture2D.FromFile(Game1.graphics.GraphicsDevice,
                Path.Combine(modPath, "assets/character/portraits_nyapu_default.png"));
        _Classic = Texture2D.FromFile(Game1.graphics.GraphicsDevice,
                Path.Combine(modPath, "assets/character/portraits_classic_default.png"));
    }

    public static bool HasNyapu = false;
    public static Portraits Type = Portraits.Auto;

    public static void Unload()
    {
        Type = ModEntry.Config.PortraitStyle;
        _Nouveau?.Dispose();
        _Nouveau = null;
        _Nyapu?.Dispose();
        _Nyapu = null;
        _Classic?.Dispose();
        _Classic = null;
    }

    public static void Draw(SpriteBatch sb, Vector2 coords)
    {
        float semi = 0.4f;
        LoadPortraits();
        float nouveauT = (Type == Portraits.Nouveau ? 1f : semi);
        float nyapuT = (Type == Portraits.Nyapu ? 1f : semi);
        float classicT = (Type == Portraits.Classic ? 1f : semi);
        if (Type == Portraits.Auto) {
            nouveauT = (HasNyapu ? semi : 1f);
            nyapuT = (HasNyapu ? 1f : semi);
        }
        Rectangle dest = new((int)coords.X + 96, (int)coords.Y - 64, 128, 128);
        sb.Draw(_Nouveau,
                color: Color.White * nouveauT,
                sourceRectangle: new(0, 0, 64, 64),
                destinationRectangle: dest);
        dest.X += dest.Width;
        sb.Draw(_Nyapu,
                color: Color.White * nyapuT,
                sourceRectangle: new(0, 0, 64, 64),
                destinationRectangle: dest);
        dest.X += dest.Width;
        sb.Draw(_Classic,
                color: Color.White * classicT,
                sourceRectangle: new(0, 0, 64, 64),
                destinationRectangle: dest);
    }
}

internal sealed class OutfitPreviewer
{
    private static Texture2D _Spring;
    private static Texture2D _Summer;
    private static Texture2D _Fall;
    private static Texture2D _Winter;
    private static Texture2D _Tuxedo;

    private static void LoadSprites()
    {
        if (_Spring != null && _Summer != null && _Fall != null &&
                _Winter != null && _Tuxedo != null) {
            return;
        }
        var modInfo = HML.ModHelper.ModRegistry.Get(HML.CPId);
        if (modInfo is null) {
            Log.Error("Fatal: Hat Mouse Lacey is not installed correctly.");
            _Spring = Game1.mouseCursors;
            _Summer = Game1.mouseCursors;
            _Fall = Game1.mouseCursors;
            _Winter = Game1.mouseCursors;
            _Tuxedo = Game1.mouseCursors;
            return;
        }
        string modPath = (string)modInfo.GetType().GetProperty("DirectoryPath")
                .GetValue(modInfo);
        _Spring = Texture2D.FromFile(Game1.graphics.GraphicsDevice,
                Path.Combine(modPath, "assets/character/sprites_default.png"));
        _Summer = Texture2D.FromFile(Game1.graphics.GraphicsDevice,
                Path.Combine(modPath, "assets/character/sprites_summer.png"));
        _Fall = Texture2D.FromFile(Game1.graphics.GraphicsDevice,
                Path.Combine(modPath, "assets/character/sprites_fall.png"));
        _Winter = Texture2D.FromFile(Game1.graphics.GraphicsDevice,
                Path.Combine(modPath, "assets/character/sprites_winter_outdoors.png"));
        _Tuxedo = Texture2D.FromFile(Game1.graphics.GraphicsDevice,
                Path.Combine(modPath, "assets/character/wedding_tuxedo.png"));
    }

    public static bool SeasonalsOn = false;
    public static Outfit WeddingAttire = Outfit.Dress;

    public static void Unload()
    {
        OutfitPreviewer.SeasonalsOn = ModEntry.Config.SeasonalOutfits;
        OutfitPreviewer.WeddingAttire = ModEntry.Config.WeddingAttire;
        _Spring?.Dispose();
        _Spring = null;
        _Summer?.Dispose();
        _Summer = null;
        _Fall?.Dispose();
        _Fall = null;
        _Winter?.Dispose();
        _Winter = null;
        _Tuxedo?.Dispose();
        _Tuxedo = null;
    }

    public static void Draw(SpriteBatch sb, Vector2 coords)
    {
        float semi = 0.4f;
        LoadSprites();
        Rectangle dest = new((int)coords.X + 96, (int)coords.Y + 48, 32, 64);
        sb.Draw(_Spring,
                color: Color.White * 1f,
                sourceRectangle: new(0, 0, 16, 32),
                destinationRectangle: dest);
        dest.X += dest.Width;
        sb.Draw(_Summer,
                color: Color.White * (SeasonalsOn ? 1f : semi),
                sourceRectangle: new(0, 0, 16, 32),
                destinationRectangle: dest);
        dest.X += dest.Width;
        sb.Draw(_Fall,
                color: Color.White * (SeasonalsOn ? 1f : semi),
                sourceRectangle: new(0, 0, 16, 32),
                destinationRectangle: dest);
        dest.X += dest.Width;
        sb.Draw(_Winter,
                color: Color.White * 1f,
                sourceRectangle: new(0, 0, 16, 32),
                destinationRectangle: dest);
        dest.X += dest.Width * 2;
        dest.Width *= 2;
        sb.Draw((WeddingAttire == Outfit.Tuxedo ? _Tuxedo : _Spring),
                color: Color.White * 1f,
                sourceRectangle: new(0, (WeddingAttire == Outfit.Tuxedo ? 0 : 288), 32, 32),
                destinationRectangle: dest);
    }
}

internal sealed class ChildPreviewer
{
    private static Texture2D _HumanBoy;
    private static Texture2D _HumanGirl;
    private static Texture2D _MouseBoy;
    private static Texture2D _MouseGirl;

    private static void LoadSprites()
    {
        if (_HumanBoy != null && _HumanGirl != null && _MouseBoy != null && _MouseGirl != null) {
            return;
        }
        var modInfo = HML.ModHelper.ModRegistry.Get(HML.CPId);
        if (modInfo is null) {
            Log.Error("Fatal: Hat Mouse Lacey is not installed correctly.");
            _HumanBoy = Game1.mouseCursors;
            _HumanGirl = Game1.mouseCursors;
            _MouseBoy = Game1.mouseCursors;
            _MouseGirl = Game1.mouseCursors;
            return;
        }
        _HumanBoy = Game1.content.Load<Texture2D>("Characters/Toddler");
        _HumanGirl = Game1.content.Load<Texture2D>("Characters/Toddler_girl_dark");
        string modPath = (string)modInfo.GetType().GetProperty("DirectoryPath")
                .GetValue(modInfo);
        _MouseBoy = Texture2D.FromFile(Game1.graphics.GraphicsDevice,
                Path.Combine(modPath, "assets/character/toddler_boy_0.png"));
        _MouseGirl = Texture2D.FromFile(Game1.graphics.GraphicsDevice,
                Path.Combine(modPath, "assets/character/toddler_girl_1.png"));
    }

    public static ChildStrategy Type = ChildStrategy.ByGender;

    public static void Unload()
    {
        Type = ModEntry.Config.ChildStrategy;
        // don't Dispose the vanilla textures. bad news
        _HumanBoy = null;
        _HumanGirl = null;
        _MouseBoy?.Dispose();
        _MouseBoy = null;
        _MouseGirl?.Dispose();
        _MouseGirl = null;
    }

    public static void Draw(SpriteBatch sb, Vector2 coords)
    {
        float semi = 0.4f;
        bool human = (Type == ChildStrategy.ByGender || Type == ChildStrategy.AlwaysAdopt);
        bool mouse = (Type == ChildStrategy.ByGender || Type == ChildStrategy.AlwaysPregnant);
        LoadSprites();
        Rectangle dest = new((int)coords.X + 96 + 256, (int)coords.Y + 32, 32, 64);
        sb.Draw(_HumanBoy,
                color: Color.White * (human ? 1f : semi),
                sourceRectangle: new(0, 0, 16, 32),
                destinationRectangle: dest);
        dest.X += dest.Width;
        sb.Draw(_HumanGirl,
                color: Color.White * (human ? 1f : semi),
                sourceRectangle: new(0, 0, 16, 32),
                destinationRectangle: dest);
        dest.X += dest.Width;
        sb.Draw(_MouseBoy,
                color: Color.White * (mouse ? 1f : semi),
                sourceRectangle: new(0, 0, 16, 32),
                destinationRectangle: dest);
        dest.X += dest.Width;
        sb.Draw(_MouseGirl,
                color: Color.White * (mouse ? 1f : semi),
                sourceRectangle: new(0, 0, 16, 32),
                destinationRectangle: dest);
    }
}

