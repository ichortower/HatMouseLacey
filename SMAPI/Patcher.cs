using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using xTile.Dimensions;

namespace ichortower_HatMouseLacey
{
    /*
     * Each function in this class is a Harmony patch. It should be public and
     * static, and its name determines how it is applied by a loop in ModEntry:
     *
     *   ClassName_Within_StardewValley__MethodName__Type
     *
     * Two underscores separate the class name, method name, and type
     * (Prefix or Postfix; Transpiler in the future if needed).
     * In the class name, single underscores are converted to dots to resolve
     * the targeted class.
     *
     * Why do this instead of using annotations?
     * I already had this class structure set up, and I prefer it to making
     * a new class for every patch.
     */
    internal class Patcher
    {
        /*
         * Load nice song names for the jukebox.
         */
        public static void Utility__getSongTitleFromCueName__Postfix(
                string cueName,
                ref string __result)
        {
            if (cueName.StartsWith(HML.CPId)) {
                __result = Game1.content.LoadString($"Strings\\StringsFromCSFiles:{cueName}");
            }
        }


        /*
         * Add an extra check for the "can interact/which cursor" NPC code, to
         * display the dialogue cursor when you are pointing to Lacey and
         * wearing an unseen hat.
         */
        public static void Utility__checkForCharacterInteractionAtTile__Postfix(
                StardewValley.Utility __instance,
                Vector2 tileLocation,
                Farmer who,
                ref bool __result)
        {
            if (Game1.mouseCursor > Game1.cursor_default) {
                return;
            }
            NPC Lacey = Game1.currentLocation.isCharacterAtTile(tileLocation);
            if (Lacey != null && Lacey.Name.Equals(HML.LaceyInternalName)) {
                string hatstr = LCHatString.GetCurrentHatString(who);
                if (hatstr != null && !LCModData.HasShownHat(hatstr)) {
                    Game1.mouseCursor = Game1.cursor_talk;
                    __result = true;
                    if (Utility.tileWithinRadiusOfPlayer(
                            (int)tileLocation.X, (int)tileLocation.Y, 1, who)) {
                        Game1.mouseCursorTransparency = 1f;
                    }
                    else {
                        Game1.mouseCursorTransparency = 0.5f;
                    }
                }
            }
        }

        /*
         * NPC.sayHiTo() generates the "Hi, <NPC>!" etc. speech bubbles when
         * NPCs are walking near each other. This patch makes Lacey and Andy
         * say "..." to each other instead, since they don't get along.
         */
        public static bool NPC__sayHiTo__Prefix(
                StardewValley.NPC __instance,
                StardewValley.Character c)
        {
            if ((__instance.Name.Equals(HML.LaceyInternalName) &&
                    c.Name.Equals("Andy")) ||
                    (__instance.Name.Equals("Andy") &&
                    c.Name.Equals(HML.LaceyInternalName))) {
                __instance.showTextAboveHead("...");
                if (c is NPC && Game1.random.NextDouble() < 0.66) {
                    (c as NPC).showTextAboveHead("...", preTimer:
                            1000 + Game1.random.Next(500));
                }
                return false;
            }
            return true;
        }

        /*
         * NPC.isGaySpouse is only used to decide between pregnancy and
         * adoption for your children (including by the mouse children patch,
         * for the same purpose).
         * If the AlwaysAdopt config setting is set to true, this patch will
         * cause Lacey to return true (gay) every time, forcing adoption.
         */
        public static void NPC__isGaySpouse__Postfix(
                StardewValley.NPC __instance,
                ref bool __result)
        {
            if (__instance.Name.Equals(HML.LaceyInternalName) &&
                    ModEntry.Config.AlwaysAdopt) {
                __result = true;
            }
        }


        /*
         * Prefix NPC.checkAction to load Lacey's reactions when you are
         * wearing a hat she hasn't seen you in.
         * Requires a mail id which is set by watching the 2-heart event.
         */
        /*
        public static bool NPC__checkAction__Prefix(
                StardewValley.NPC __instance,
                StardewValley.Farmer who,
                StardewValley.GameLocation l,
                ref bool __result)
        {
            if (!__instance.Name.Equals(ModEntry.LCInternalName)) {
                return true;
            }
            if (!who.hasOrWillReceiveMail($"{MailPrefix}HatReactions")) {
                return true;
            }
            if (who.ActiveObject != null && who.ActiveObject.canBeGivenAsGift()) {
                return true;
            }
            if (who.isRidingHorse()) {
                return true;
            }
            string hatstr = LCHatString.GetCurrentHatString(who);
            if (hatstr is null || LCModData.HasShownHat(hatstr)) {
                return true;
            }
            string hatkey = hatstr.Replace(" ", "").Replace("'", "").Replace("|", ".");

            string newHatText = Game1.content.LoadString(
                    $"Strings\\{__instance.Name}HatReactions:newHat");
            __instance.faceTowardFarmerForPeriod(4000, 4, faceAway: false, who);
            __instance.doEmote(32);
            who.currentLocation.localSound("give_gift");
            __instance.CurrentDialogue.Push(new Dialogue(newHatText, __instance));
            Game1.drawDialogue(__instance);
            Game1.afterDialogues = delegate {
                int nowFacing = who.FacingDirection;
                DelayedAction.delayedBehavior turn = delegate {
                    who.faceDirection(++nowFacing % 4);
                };
                int turntime = 500;
                who.freezePause = 4*turntime+800;
                DelayedAction[] anims = new DelayedAction[5] {
                        new DelayedAction(turntime, turn),
                        new DelayedAction(2*turntime, turn),
                        new DelayedAction(3*turntime, turn),
                        new DelayedAction(4*turntime, turn),
                        new DelayedAction(4*turntime+600, delegate {
                            string reactionText = Game1.content.LoadStringReturnNullIfNotFound(
                                    $"Strings\\{__instance.Name}HatReactions:{hatkey}");
                            if (reactionText is null) {
                                reactionText = Game1.content.LoadString(
                                    $"Strings\\{__instance.Name}HatReactions:404");
                            }
                            __instance.CurrentDialogue.Push(
                                    new Dialogue(reactionText, __instance));
                            Game1.drawDialogue(__instance);
                            Game1.player.changeFriendship(10, __instance);
                            who.completeQuest(236750210);
                            LCModData.AddShownHat(hatstr);
                        })
                };
                foreach (var a in anims) {
                    Game1.delayedActions.Add(a);
                }
            };
            __result = true;
            return false;
        }
        */


        /*
         * Prefix NPC.tryToReceiveActiveObject to implement bouquet reaction
         * dialogue.
         * This is to give character-specific reactions for <4 hearts
         * ("bouquetLow"), <8 hearts ("bouquetMid"), or 8+ acceptance
         * ("bouquetAccept").
         *
         * But there's also "bouquetRejectCruelty" and
         * "bouquetRejectCrueltyRepeat", which apply if you have been mean
         * enough to her in her heart events. The former queues a letter which
         * adds a quest enabling an extra event where you can apologize.
         * "bouquetAcceptApologized" is used if you did the apology event and
         * are giving the bouquet afterward (could be cold feet in the event
         * or after a breakup).
         */
        /*
        public static bool NPC__tryToReceiveActiveObject__Prefix(
                StardewValley.NPC __instance,
                StardewValley.Farmer who)
        {
            var obj = who.ActiveObject;
            if (!__instance.Name.Equals(ModEntry.LCInternalName) || obj.ParentSheetIndex != 458) {
                return true;
            }
            if (__instance.isMarriedOrEngaged()) {
                return true;
            }
            if (!who.friendshipData.ContainsKey(__instance.Name)) {
                return true;
            }
            Friendship friendship = who.friendshipData[__instance.Name];
            if (friendship.IsDating() || friendship.IsDivorced()) {
                return true;
            }
            who.Halt();
            who.faceGeneralDirection(__instance.getStandingPosition(),
                    0, opposite: false, useTileCalculations: false);
            string toLoad = $"Characters\\Dialogue\\{__instance.Name}:";
            bool accepted = false;
            bool addApologyQuest = false;
            if (friendship.Points < 1000) {
                toLoad += "bouquetLow";
            }
            else if (friendship.Points < 2000) {
                toLoad += "bouquetMid";
            }
            else {
                if (Game1.player.hasOrWillReceiveMail($"{MailPrefix}ApologyAccepted")) {
                    toLoad += "bouquetAcceptApologized";
                    accepted = true;
                }
                else if (Game1.player.hasOrWillReceiveMail($"{MailPrefix}ApologySummons")) {
                    toLoad += "bouquetRejectCrueltyRepeat";
                }
                else if (LCModData.CrueltyScore >= 4) {
                    toLoad += "bouquetRejectCruelty";
                    addApologyQuest = true;
                }
                else {
                    toLoad += "bouquetAccept";
                    accepted = true;
                }
            }
            if (accepted) {
                if (!friendship.IsDating()) {
                    friendship.Status = FriendshipStatus.Dating;
                    // more reflection abuse
                    Multiplayer mp = (Multiplayer)typeof(Game1)
                            .GetField("multiplayer", BindingFlags.Static | BindingFlags.NonPublic)
                            .GetValue(null);
                    mp.globalChatInfoMessage("Dating",
                            Game1.player.Name, __instance.displayName);
                }
                who.changeFriendship(25, __instance);
                who.reduceActiveItemByOne();
                who.completelyStopAnimatingOrDoingAction();
                __instance.doEmote(20);
            }
            string response = Game1.content.LoadString(toLoad);
            __instance.CurrentDialogue.Push(new Dialogue(response, __instance));
            Game1.drawDialogue(__instance);
            if (addApologyQuest) {
                Game1.afterDialogues = delegate {
                    Game1.addMailForTomorrow($"{MailPrefix}ApologySummons");
                };
            }
            return false;
        }
        */

        /*
         * Prefix for StardewValley/Event.checkAction, used to implement the
         * hat shop at the Stardew Valley Fair.
         * Checks for a tile property "Action": "HatShop" on the buildings
         * layer, then generates the hat shop menu just like the forest shop.
         */
            /*
        public static bool Event__checkAction__Prefix(
                StardewValley.Event __instance,
                Location tileLocation,
                xTile.Dimensions.Rectangle viewport,
                Farmer who,
                ref bool __result)
        {
            try {
                if (!__instance.isFestival) {
                    return true;
                }
                string tileAction = Game1.currentLocation.doesTileHaveProperty(
                        tileLocation.X, tileLocation.Y, "Action", "Buildings");
                if (tileAction is null) {
                    return true;
                }
                string word = tileAction.Split(' ')[0];
                if (word.Equals("HatShop")) {
                    var stock = Utility.getHatStock();
                    if (stock.Count == 0) {
                        return true;
                    }
                    string dialogue = Game1.content.LoadString(
                        $"Characters\\Dialogue\\{ModEntry.LCInternalName}:" +
                        "fall_16.fair.shopdialogue");
                    var menu = new ShopMenu(stock, 0, "default");
                    menu.portraitPerson = Game1.getCharacterFromName(ModEntry.LCInternalName);
                    menu.potraitPersonDialogue = Game1.parseText(
                            dialogue, Game1.dialogueFont, 304);
                    Game1.activeClickableMenu = menu;
                    __result = true;
                    return false;
                }
                return true;
            }
            catch (Exception e) {
                Monitor.Log($"Harmony patch failed in {nameof(Event__checkAction__Prefix)}:\n{e}",
                        LogLevel.Error);
                return true;
            }
        }
        */


        /*
         * Postfix patch for Event->DefaultCommands.Viewport.
         * If the current map is Lacey's house interior, and the command was
         * of the form "viewport x y" or "viewport x y true", and the viewport
         * is large enough to fit the entire map, honor the command coordinates
         * instead of forcing the viewport to the center of the map.
         * (I submit this is the correct behavior on all maps, but I'm trying
         * not to break anything)
         */
        public static void Event_nest_DefaultCommands__Viewport__Postfix(
                StardewValley.Event @event,
                string[] args,
                EventContext context)
        {
            if (!Game1.currentLocation.Name.Equals($"{HML.CPId}_MouseHouse")) {
                return;
            }
            // just redoing the normal calculation and not doing the map size part
            if (args.Length == 3 || (args.Length == 4 && args[3].Equals("true"))) {
                int tx = @event.OffsetTileX(Convert.ToInt32(args[1]));
                int ty = @event.OffsetTileX(Convert.ToInt32(args[2]));
                Game1.viewport.X = tx * 64 + 32 - Game1.viewport.Width / 2;
                Game1.viewport.Y = ty * 64 + 32 - Game1.viewport.Height / 2;
            }
        }

        /*
         * Prefix patch for Event.skipEvent.
         * In vanilla, any events with end behavior other than "end", or with
         * important things to do which rely on certain event commands to
         * execute (giving items or crafting recipes, setting flags, etc.),
         * have their ids hardcoded into a switch statement so that those tasks
         * can be done when skipped.
         *
         * This adds checks for Lacey's events. It is a prefix in order to run
         * the desired endBehaviors before the vanilla function, which defaults
         * to running simply "end".
         *
         * Except in one case (14 hearts), this does not avoid the default
         * function. This means exitEvent is called twice, and the rest of
         * event cleanup happens after endBehaviors instead of before.
         * This does not seem to cause any problems.
         */
        /*
        public static bool Event__skipEvent__Prefix(
                StardewValley.Event __instance)
        {
            if (__instance.id == 236750200) {
                if (!Game1.player.mailReceived.Contains($"{MailPrefix}HatReactions")) {
                    Game1.player.mailReceived.Add($"{MailPrefix}HatReactions");
                }
                Game1.player.addQuest(236750210);
                __instance.endBehaviors(new string[1]{"end"},
                        Game1.currentLocation);
            }
            else if (__instance.id == 236751001) {
                LCEventCommands.command_timeAfterFade(Game1.currentLocation,
                        Game1.currentGameTime,
                        new string[2]{"timeAfterFade", "2200"});
                __instance.endBehaviors(new string[2]{"end", "warpOut"},
                        Game1.currentLocation);
            }
            // for this one, we have to skip the default function, since we
            // need to warp to the farmhouse and *then* use "end warpOut".
            // that takes time, so we can't let the base game run "end".
            else if (__instance.id == 236751400 || __instance.id == 236751401) {
                LCEventCommands.command_timeAfterFade(Game1.currentLocation,
                        Game1.currentGameTime,
                        new string[2]{"timeAfterFade", "2100"});
                LocationRequest req = Game1.getLocationRequest("FarmHouse");
                // save our current location. null out its event reference
                // when the warp finishes
                GameLocation skipLocation = Game1.currentLocation;
                req.OnLoad += delegate {
                    skipLocation.currentEvent = null;
                    Game1.currentLocation.currentEvent = __instance;
                    __instance.endBehaviors(new string[2]{"end", "warpOut"},
                            Game1.currentLocation);
                };
                Game1.warpFarmer(req, Game1.player.getTileX(),
                        Game1.player.getTileY(), Game1.player.FacingDirection);

                if (__instance.playerControlSequence) {
                    __instance.EndPlayerControlSequence();
                }
                Game1.playSound("drumkit6");
                // reflection abuse
                var apam = (Dictionary<string, Vector3>)(typeof(StardewValley.Event))
                        .GetField("actorPositionsAfterMove", BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(__instance);
                apam.Clear();
                foreach (NPC i in __instance.actors) {
                    bool ignore_stop_animation = i.Sprite.ignoreStopAnimation;
                    i.Sprite.ignoreStopAnimation = true;
                    i.Halt();
                    i.Sprite.ignoreStopAnimation = ignore_stop_animation;
                    __instance.resetDialogueIfNecessary(i);
                }
                Game1.player.Halt();
                Game1.player.ignoreCollisions = false;
                Game1.exitActiveMenu();
                Game1.dialogueUp = false;
                Game1.dialogueTyping = false;
                Game1.pauseTime = 0f;
                return false;
            }
            return true;
        }
        */


        /*
         * Postfix patch Dialogue.parseDialogueString to add a new dialogue
         * command: $m.
         *      $m <mail id>#<text1>|<text2>
         * Display text1 if the given mail has been sent, or text2 otherwise.
         *
         * Works sort of like $d (world state), but checks a mail id. Only
         * switches the next command (# to #), instead of the whole dialogue
         * string like $d.
         */
        public static void Dialogue__parseDialogueString__Postfix(
                StardewValley.Dialogue __instance,
                string masterString)
        {
            /* the way the parsing works, if we find a dialogue that says
             * "$m <mail id>", the next one will be the two options separated
             * by a "|" character.
             * we'll check the mail id, choose the correct half, then remove
             * the $m message. */
            for (int i = 0; i < __instance.dialogues.Count - 1; ++i) {
                string command = __instance.dialogues[i];
                if (!command.StartsWith("$m ") || command.Length <= 3) {
                    continue;
                }
                string mailId = command.Substring(3);
                string[] options = __instance.dialogues[i+1].Split('|');
                /* put the '{' at the end of text1 if text2 has one. this lets
                 * us continue with #$b# */
                if (options.Length >= 2 && options[1].EndsWith("{")) {
                    options[0] += "{";
                }
                if (options.Length == 1) {
                    HML.Monitor.Log($"WARNING: couldn't find '|' separator in $m dialogue command",
                            LogLevel.Warn);
                }
                else if (Game1.player.hasOrWillReceiveMail(mailId)) {
                    __instance.dialogues[i+1] = options[0];
                }
                else {
                    __instance.dialogues[i+1] = options[1];
                }
                __instance.dialogues.Remove(command);
            }
        }


        internal static Farmer getFarmerFromUniqueMultiplayerID(long id)
        {
            if (!Game1.IsMultiplayer) {
                return Game1.player;
            }
            if (Game1.serverHost.Value != null) {
                return Game1.serverHost.Value;
            }
            return (from f in Game1.otherFarmers.Values
                    where f.UniqueMultiplayerID == id
                    select f).ElementAt(0);
        }

        /*
         * Loads mouse-child graphics for babies and toddlers if the conditions
         * are right (biological children with Lacey).
         * This is done by checking a custom field on the Child's modData, and
         * writing that field if not already set.
         */
        public static void Characters_Child__reloadSprite__Postfix(
                Child __instance)
        {
            string lc = HML.LaceyInternalName;
            string variant;
            if (!__instance.modData.TryGetValue($"{lc}/ChildVariant", out variant) ||
                    variant is null) {
                /*
                 * the Child already has idOfParent set to the parent farmer's
                 * uniqueMultiplayerID. find that farmer and check the spouse.
                 */
                Farmer parent = getFarmerFromUniqueMultiplayerID(__instance.idOfParent.Value);
                if (parent is null) {
                    HML.Monitor.Log($"Found child {__instance.Name} with missing parent.",
                            LogLevel.Warn);
                    return;
                }
                NPC l = parent.getSpouse();
                /* I don't like dumping out here, but on a normal load this
                 * postfix runs three times, and the first time spouse is null.
                 * so I can't save the -1 value yet */
                if (l is null) {
                    HML.Monitor.Log($"Spouse missing for unsaved child {__instance.Name}",
                            LogLevel.Warn);
                    return;
                }
                variant = "-1";
                if (l.Name.Equals(lc) && !l.isGaySpouse()) {
                    variant = "0";
                    /* if darkSkinned is set (50% for dark farmers), use brown
                     * mouse child. otherwise, pick one randomly. */
                    if (__instance.darkSkinned.Value) {
                        variant = "1";
                    }
                    else if (Game1.random.NextDouble() < 0.33) {
                        variant = "1";
                    }
                }
                __instance.modData[$"{lc}/ChildVariant"] = variant;
            }
            if (variant == "-1") {
                return;
            }
            /* only need to set the name. the other fields are already handled */
            if (__instance.Age >= 3) {
                __instance.Sprite.textureName.Value = $"Characters\\{lc}\\Toddler_" +
                        $"{(__instance.Gender == 0 ? "boy" : "girl")}_" +
                        $"{variant}";
            }
            else {
                __instance.Sprite.textureName.Value = $"Characters\\{lc}\\Baby_{variant}";
            }
        }


        /*
         * Make TerrainFeatures.Grass honor the "isTemporarilyInvisible" flag.
         * This is set by the "makeInvisible" event command, which I use only
         * for SVE compatibility in the picnic event (as a stopgap until I
         * implement the temporary location version).
         */
        public static bool TerrainFeatures_Grass__draw__Prefix(
                StardewValley.TerrainFeatures.Grass __instance,
                SpriteBatch spriteBatch,
                Vector2 tileLocation)
        {
            if (__instance.isTemporarilyInvisible) {
                return false;
            }
            return true;
        }

    }
}
