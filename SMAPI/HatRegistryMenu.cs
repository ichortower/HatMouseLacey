using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using FSApi = FashionSense.Framework.Interfaces.API.IApi;

namespace ichortower_HatMouseLacey
{
    internal class HatRegistryMenu : IClickableMenu
    {
        private static string _MenuOpenSound = "shwip";
        private static string _PageTurnSound = "shwip";
        private static int _InnerPadding = 4;
        private static int _OuterPadding = 8;
        private static int _BorderWidth = 16;
        private static int _RoomForTitle = 72;

        private static int _Columns = 8;
        private static int _Rows = 6;

        private static int _DefaultWidth = (20*4)*_Columns + _InnerPadding*7 +
                _OuterPadding*2 + _BorderWidth*2;
        private static int _DefaultHeight = _RoomForTitle + (20*4)*_Rows +
                _InnerPadding*5 + _OuterPadding*2 + _BorderWidth*2;

        private Rectangle[] _FrameNineslice = new Rectangle[9] {
            new(16, 16, 28, 28),
            new(44, 16, 28, 28),
            new(212, 16, 28, 28),
            new(16, 44, 28, 28),
            Rectangle.Empty,
            new(212, 44, 28, 28),
            new(16, 212, 28, 28),
            new(44, 212, 28, 28),
            new(212, 212, 28, 28),
        };
        private Rectangle _FrameBg = new(64, 128, 64, 64);

        private List<List<ClickableTextureComponent>> _Pages = new();
        private int _CurrentPage = 0;
        public int CurrentPage {
            get {
                return _CurrentPage;
            }
            set {
                _CurrentPage = Math.Max(0, Math.Min(value, _Pages.Count-1));
            }
        }
        public ClickableTextureComponent BackButton;
        public ClickableTextureComponent ForwardButton;
        private string _HoverText;

        private static List<HatProxy> _FashionSenseHats = new();
        private static FSApi _fsapi = null;
        private static bool FSBroken = false;

        public HatRegistryMenu(bool playSound = true)
            : base(Game1.uiViewport.Width/2 - _DefaultWidth/2,
                Game1.uiViewport.Height/2 - _DefaultHeight/2,
                _DefaultWidth, _DefaultHeight, true)
        {
            Dictionary<string, string> hatData = DataLoader.Hats(Game1.content);
            // fill the collapse map in advance. it would be filled
            // automatically during the iteration later, but not until after
            // the point at which we want to dump out to skip hats that should
            // be omitted due to collapse.
            LCHatString.FillCollapseMap();
            // FillCollapseMap calls this, but it's possible we still need to
            LookForFashionSenseHats();

            BackButton = new ClickableTextureComponent(new Rectangle(
                        xPositionOnScreen + _BorderWidth + _OuterPadding*2,
                        yPositionOnScreen + _BorderWidth + _OuterPadding*2, 48, 44),
                    texture: Game1.mouseCursors,
                    sourceRect: new Rectangle(352, 495, 12, 11),
                    scale: 4f) {
                        myID = -20,
                        rightNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                        leftNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                        upNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                        downNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                    };
            ForwardButton = new ClickableTextureComponent(new Rectangle(
                        xPositionOnScreen + width - _BorderWidth - _OuterPadding*2 - 48,
                        yPositionOnScreen + _BorderWidth + _OuterPadding*2, 48, 44),
                    texture: Game1.mouseCursors,
                    sourceRect: new Rectangle(365, 495, 12, 11),
                    scale: 4f) {
                        myID = -21,
                        rightNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                        leftNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                        upNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                        downNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                    };
            int count = 0;
            int baseX = xPositionOnScreen + _BorderWidth + _OuterPadding;
            int baseY = yPositionOnScreen + _BorderWidth + _OuterPadding + _RoomForTitle;
            int pagesize = _Columns * _Rows;
            foreach (var kvp in hatData) {
                // skip hats considered duplicates of others, except vanilla
                // (vanilla is just the pan hats. the party hats collapse by
                // having identical names)
                if (ModEntry.Config.CollapseHatRegistry &&
                        !kvp.Key.StartsWith("SV") &&
                        LCHatString.HatCollapseMap.ContainsKey(kvp.Key)) {
                    continue;
                }
                int subcount = count % 48;
                if (subcount == 0) {
                    _Pages.Add(new List<ClickableTextureComponent>());
                }
                int xpos = baseX + (subcount%_Columns)*(20*4 + _InnerPadding);
                int ypos = baseY + (subcount/_Columns)*(20*4 + _InnerPadding);
                ParsedItemData pid = ItemRegistry.GetDataOrErrorItem($"(H){kvp.Key}");
                int spriteIndex = pid.SpriteIndex;
                Texture2D texture = pid.GetTexture();
                Rectangle sourceRect = new(spriteIndex * 20 % texture.Width,
                        spriteIndex * 20 / texture.Width * 20 * 4, 20, 20);
                bool hatWasShown = MakeHoverTextObj(pid, out string hoverText);
                ClickableTextureComponent obj = new(
                        $"obj \"{pid.QualifiedItemId}\" {hatWasShown}",
                        bounds: new Rectangle(xpos, ypos, 80, 80),
                        label: null,
                        hoverText: hoverText,
                        texture: texture,
                        sourceRect: sourceRect,
                        scale: 4f,
                        drawShadow: false) {
                            myID = count,
                            rightNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                            leftNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                            upNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                            downNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                        };
                _Pages[_Pages.Count-1].Add(obj);
                ++count;
            }
            foreach (HatProxy hat in _FashionSenseHats) {
                if (ModEntry.Config.CollapseHatRegistry &&
                        LCHatString.HatCollapseMap.ContainsKey(
                            LCHatString.GetFSHatString(hat.Id))) {
                    continue;
                }
                if (hat.IsLocked) {
                    continue;
                }
                int subcount = count % 48;
                if (subcount == 0) {
                    _Pages.Add(new List<ClickableTextureComponent>());
                }
                int xpos = baseX + (subcount%_Columns)*(20*4 + _InnerPadding);
                int ypos = baseY + (subcount/_Columns)*(20*4 + _InnerPadding);
                bool hatWasShown = MakeHoverTextFS(hat, out string hoverText);
                int useScale = 4;
                Rectangle useRect = new(hat.SourceRect.X, hat.SourceRect.Y,
                        hat.SourceRect.Width, hat.SourceRect.Height);
                while (useScale > 1 &&
                        (useRect.Width * useScale > 80 ||
                        useRect.Height * useScale > 80)) {
                    --useScale;
                }
                if (useScale == 1) {
                    if (useRect.Width > 80) {
                        useRect.X += (useRect.Width - 80) / 2;
                        useRect.Width = 80;
                    }
                    if (useRect.Height > 80) {
                        useRect.Y += useRect.Height - 80;
                        useRect.Height = 80;
                    }
                }
                ClickableTextureComponent obj = new(
                        $"fs \"{hat.Id}\" {hatWasShown}",
                        bounds: new Rectangle(xpos, ypos, 80, 80),
                        label: null,
                        hoverText: hoverText,
                        texture: hat.Texture,
                        sourceRect: useRect,
                        scale: (float)useScale,
                        drawShadow: false) {
                            myID = count,
                            rightNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                            leftNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                            upNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                            downNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                        };
                _Pages[_Pages.Count-1].Add(obj);
                ++count;
            }
            if (playSound) {
                Game1.playSound(_MenuOpenSound);
            }
            if (Game1.options.SnappyMenus) {
                populateClickableComponentList();
                snapToDefaultClickableComponent();
            }
        }

        /*
         * This extremely gnarly method does some reflection crimes in order to
         * access internal stuff within Fashion Sense that isn't exposed via
         * the API. This would be less criminal with an assembly reference, but
         * using one of those is off the table because it would make FS a hard
         * dependency.
         *
         * Please contact me if you know a less heinous way to accomplish this.
         */
        internal static void LookForFashionSenseHats()
        {
            if (FSBroken) {
                return;
            }
            if (_FashionSenseHats.Count > 0) {
                return;
            }
            if (_fsapi is null) {
                var api = HML.ModHelper.ModRegistry.GetApi<FSApi>(
                        "PeacefulEnd.FashionSense");
                if (api is null) {
                    return;
                }
                _fsapi = api;
            }
            FieldInfo target = _fsapi.GetType().GetField("__Target",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            if (target is null) {
                Log.Warn("Tried to find __Target on FS API object but got nothing. " +
                        "Something critical is broken. Please report this.");
                FSBroken = true;
                return;
            }
            //
            // the goal here is to call
            // FashionSense.textureManager.GetAllAppearanceModels<HatContentPack>()
            // to get a list of all installed hats. from there, we save proxy
            // objects for them that store the stuff we need for the menu.
            //
            var asm = target.GetValue(_fsapi).GetType().Assembly;
            var texman = Type.GetType("FashionSense.FashionSense, " + asm.FullName)?
                    .GetField("textureManager", BindingFlags.NonPublic | BindingFlags.Static)?
                    .GetValue(null);
            if (texman is null) {
                Log.Warn("Tried to access Fashion Sense's texture manager but " +
                        "got nothing. FS has likely changed its internals. " +
                        "Please report this to Hat Mouse Lacey.");
                FSBroken = true;
                return;
            }

            Type HCP = Type.GetType("FashionSense.Framework.Models.Appearances" +
                    ".Hat.HatContentPack, " + asm.FullName);
            Type HModel = Type.GetType("FashionSense.Framework.Models.Appearances" +
                    ".Hat.HatModel, " + asm.FullName);
            Type FSize = Type.GetType("FashionSense.Framework.Models.Appearances" +
                    ".Generic.Size, " + asm.FullName);
            Type FPos = Type.GetType("FashionSense.Framework.Models.Appearances" +
                    ".Generic.Position, " + asm.FullName);
            MethodInfo gaam = texman.GetType().GetMethods()
                    .Where(m => m.Name == "GetAllAppearanceModels" && m.IsGenericMethod)
                    .First();
            MethodInfo gaamgen = gaam.MakeGenericMethod(HCP);
            // blech
            PropertyInfo packId = HCP.GetProperty("Id",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo packName = HCP.GetProperty("Name",
                    BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo packIsLocked = HCP.GetProperty("IsLocked",
                    BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo packTexture = HCP.GetProperty("Texture",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo packFrontHat = HCP.GetProperty("FrontHat",
                    BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo packFromItemId = HCP.GetProperty("FromItemId",
                    BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo modelSize = HModel.GetProperty("HatSize",
                    BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo modelPosition = HModel.GetProperty("StartingPosition",
                    BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo modelWidth = FSize.GetProperty("Width",
                    BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo modelLength = FSize.GetProperty("Length",
                    BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo modelX = FPos.GetProperty("X",
                    BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo modelY = FPos.GetProperty("Y",
                    BindingFlags.Public | BindingFlags.Instance);

            var allhats = (IEnumerable<object>)gaamgen.Invoke(texman, null);
            try {
                foreach (var hat in allhats) {
                    HatProxy stored = new();
                    stored.Name = (string)packName.GetValue(hat);
                    stored.Id = (string)packId.GetValue(hat);
                    stored.IsLocked = (bool)packIsLocked.GetValue(hat);
                    stored.Texture = (Texture2D)packTexture.GetValue(hat);
                    object model = packFrontHat.GetValue(hat);
                    object size = modelSize.GetValue(model);
                    object position = modelPosition.GetValue(model);
                    int x = (int)modelX.GetValue(position);
                    int y = (int)modelY.GetValue(position);
                    int w = (int)modelWidth.GetValue(size);
                    int h = (int)modelLength.GetValue(size);
                    stored.SourceRect = new(x, y, w, h);
                    _FashionSenseHats.Add(stored);

                    string fromItem = (string)packFromItemId.GetValue(hat);
                    if (string.IsNullOrEmpty(fromItem)) {
                        continue;
                    }
                    if (fromItem.StartsWith("(H)")) {
                        fromItem = fromItem.Substring("(H)".Length);
                    }
                    LCHatString.AddCollapseEntry(
                            LCHatString.GetFSHatString(stored.Id), fromItem);
                }
                _FashionSenseHats.Sort((a, b) => a.Id.CompareTo(b.Id));
            }
            catch (Exception e) {
                Log.Warn($"{e}");
                Log.Warn("Failed to access properties on Fashion Sense's hat " +
                        "models. FS has likely changed its internals. " +
                        "Please report this to Hat Mouse Lacey.");
                FSBroken = true;
                return;
            }
        }

        private bool MakeHoverTextObj(ParsedItemData pid, out string hoverText)
        {
            Hat h = (Hat)ItemRegistry.Create(pid.QualifiedItemId);
            string hatString = LCHatString.HatIdCollapse(
                    LCHatString.GetItemHatString(h));
            if (!LCModData.HasShownHat(hatString)) {
                hoverText = "???^" + HML.ModHelper.Translation.Get(
                        "hatreactions.menu.NotYetShown");
                return false;
            }
            hoverText = $"{pid.DisplayName}^{GetReaction(hatString)}";
            return true;
        }

        private bool MakeHoverTextFS(HatProxy hat, out string hoverText)
        {
            string hatString = LCHatString.HatIdCollapse(
                    LCHatString.GetFSHatString(hat.Id));
            if (!LCModData.HasShownHat(hatString)) {
                hoverText = "???^" + HML.ModHelper.Translation.Get(
                        "hatreactions.menu.NotYetShown");
                return false;
            }
            hoverText = $"{hat.Name}^{GetReaction(hatString)}";
            return true;
        }

        private string GetReaction(string hatString)
        {
            string reactionKey = LCHatString.KeyFromHatString(hatString);
            NPC Lacey = Game1.getCharacterFromName(HML.LaceyInternalName);
            Dialogue d = Dialogue.TryGetDialogue(Lacey,
                    $"{LCHatString.ReactionsAsset}:{reactionKey}");
            if (d is null) {
                d = Dialogue.FromTranslation(Lacey, $"{LCHatString.ReactionsAsset}:404");
            }
            string continued = d.dialogues.Count > 1 ? " (...)" : "";
            string text = d.ReplacePlayerEnteredStrings(d.dialogues[0].Text);
            return $"{text}{continued}";
        }

        public override void draw(SpriteBatch b)
        {
            this.drawFrame(b);
            base.draw(b);
            string title = HML.ModHelper.Translation.Get("hatreactions.menu.HatRegistry")
                    .ToString().Replace("@", Game1.player.Name);
            Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
            Vector2 titlePos = new(xPositionOnScreen + width/2 - titleSize.X/2,
                    yPositionOnScreen + _BorderWidth + _OuterPadding);
            Utility.drawTextWithShadow(b, title, Game1.dialogueFont,
                    titlePos, Game1.textColor);
            foreach (var obj in _Pages[CurrentPage]) {
                string[] split = ArgUtility.SplitBySpace(obj.name);
                ArgUtility.TryGetOptionalBool(split, split.Length-1, out bool seen, out string err);
                Color drawColor = seen ? Color.White : Color.Black * 0.15f;
                int x = 80 - (int)(obj.sourceRect.Width * obj.baseScale);
                int y = 80 - (int)(obj.sourceRect.Height * obj.baseScale);
                obj.draw(b, drawColor, 0.86f, xOffset: x/2, yOffset: y/2);
            }
            if (CurrentPage > 0) {
                BackButton.draw(b);
            }
            if (CurrentPage < _Pages.Count - 1) {
                ForwardButton.draw(b);
            }
            if (!String.IsNullOrEmpty(_HoverText)) {
                string[] split = _HoverText.Split("^");
                string lines = Game1.parseText((split.Length > 1 ? split[1] : split[0]),
                        Game1.smallFont, 380);
                string hoverTitle = (split.Length > 1 ? split[0] : null);
                IClickableMenu.drawHoverText(b, font: Game1.smallFont,
                        text: lines, boldTitleText: hoverTitle);
            }
            base.drawMouse(b);
        }

        private void drawFrame(SpriteBatch b)
        {
            Rectangle bounds = new(xPositionOnScreen, yPositionOnScreen, width, height);
            Rectangle[] dests = nineslice(bounds, 28, 28);
            Texture2D tex = Game1.menuTexture;
            b.Draw(tex, color: Color.White,
                    sourceRectangle: _FrameBg,
                    destinationRectangle: new Rectangle(bounds.X+_BorderWidth, bounds.Y+_BorderWidth, bounds.Width-_BorderWidth*2, bounds.Height-_BorderWidth*2));
            for (int i = 0; i < _FrameNineslice.Length; ++i) {
                Rectangle r = _FrameNineslice[i];
                if (!r.Equals(Rectangle.Empty)) {
                    b.Draw(tex, color: Color.White,
                            sourceRectangle: r,
                            destinationRectangle: dests[i]);
                }
            }
        }

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);
            _HoverText = "";
            foreach (var obj in _Pages[CurrentPage]) {
                if (obj.containsPoint(x, y)) {
                    obj.scale = Math.Min(obj.scale * 1.02f, obj.baseScale * 1.1f);
                    _HoverText = obj.hoverText;
                }
                else {
                    obj.scale = Math.Max(obj.scale * 0.98f, obj.baseScale);
                }
            }
            ForwardButton.tryHover(x, y, 0.5f);
            BackButton.tryHover(x, y, 0.5f);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (CurrentPage > 0 && BackButton.containsPoint(x, y)) {
                --CurrentPage;
                if (playSound) {
                    Game1.playSound(_PageTurnSound);
                }
            }
            if (CurrentPage < _Pages.Count - 1 && ForwardButton.containsPoint(x, y)) {
                ++CurrentPage;
                if (playSound) {
                    Game1.playSound(_PageTurnSound);
                }
            }
            foreach (var obj in _Pages[CurrentPage]) {
                if (obj.containsPoint(x, y)) {
                    ReplayHatDialogue(obj, playSound);
                    return;
                }
            }
            base.receiveLeftClick(x, y, playSound);
        }

        public override void receiveScrollWheelAction(int direction)
        {
            if (CurrentPage > 0 && direction >= 10) {
                --CurrentPage;
                Game1.playSound(_PageTurnSound);
            }
            if (CurrentPage < _Pages.Count - 1 && direction <= -10) {
                ++CurrentPage;
                Game1.playSound(_PageTurnSound);
            }
        }

        public override void snapToDefaultClickableComponent()
        {
            int id = 0 + 48 * CurrentPage;
            currentlySnappedComponent = getComponentWithID(id);
            snapCursorToCurrentSnappedComponent();
        }

        public override void populateClickableComponentList()
        {
            List <ClickableComponent> l = new();
            l.Add(BackButton);
            l.Add(ForwardButton);
            for (int i = 0; i < _Pages.Count; ++i) {
                l.AddRange(_Pages[i]);
            }
            if (upperRightCloseButton != null) {
                l.Add(upperRightCloseButton);
            }
            allClickableComponents = l;
        }

        private void ReplayHatDialogue(ClickableTextureComponent obj, bool playSound = true)
        {
            string[] split = ArgUtility.SplitBySpaceQuoteAware(obj.name);
            string hatString = "BARF";
            if (split[0].Equals("obj")) {
                Hat h = (Hat)ItemRegistry.Create(split[1]);
                hatString = LCHatString.HatIdCollapse(
                        LCHatString.GetItemHatString(h));
            }
            else if (split[0].Equals("fs")) {
                hatString = LCHatString.HatIdCollapse(
                        LCHatString.GetFSHatString(split[1]));
            }
            if (!LCModData.HasShownHat(hatString)) {
                return;
            }
            _HoverText = "";
            string reactionKey = LCHatString.KeyFromHatString(hatString);
            NPC Lacey = Game1.getCharacterFromName(HML.LaceyInternalName);
            Dialogue d = Dialogue.TryGetDialogue(Lacey,
                    $"{LCHatString.ReactionsAsset}:{reactionKey}");
            if (d is null) {
                d = Dialogue.FromTranslation(Lacey, $"{LCHatString.ReactionsAsset}:404");
            }
            DialogueBox child = new(d);
            var parent = this;
            SetChildMenu(child);
            Game1.afterDialogues = delegate {
                parent.SetChildMenu(null);
            };
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            xPositionOnScreen = newBounds.Width/2 - _DefaultWidth/2;
            yPositionOnScreen = newBounds.Height/2 - _DefaultHeight/2;
        }

        public static Rectangle[] nineslice(Rectangle source, int cornerX, int cornerY)
        {
            int[] xval = {
                source.X,
                source.X + cornerX,
                source.X + source.Width - cornerX
            };
            int[] yval = {
                source.Y,
                source.Y + cornerY,
                source.Y + source.Height - cornerY
            };
            int[] wval = {
                cornerX,
                source.Width - 2 * cornerX,
                cornerX
            };
            int[] hval = {
                cornerY,
                source.Height - 2 * cornerY,
                cornerY
            };
            var ret = new List<Rectangle>();
            for (int c = 0; c < 3; ++c) {
                for (int r = 0; r < 3; ++r) {
                    ret.Add(new Rectangle(xval[r], yval[c], wval[r], hval[c]));
                }
            }
            return ret.ToArray();
        }
    }

    internal class HatProxy
    {
        public string Name;
        public string Id;
        public bool IsLocked;
        public Texture2D Texture;
        public Rectangle SourceRect;
    }
}
