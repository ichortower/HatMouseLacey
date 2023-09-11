using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using xTile;
using xTile.Layers;
using xTile.Tiles;

namespace ichortower_HatMouseLacey
{
    /*
     * This class holds functions that handle compatibility with other mods.
     * Put something here to handle it if it's impossible or slow via other
     * methods.
     */
    internal class LCCompat
    {
        /*
         * Clean up the map tiles near Lacey's cabin, if necessary.
         * The goal here is to support SVR3 properly: it edits the forest map,
         * but that can be disabled via config and we can't check that over in
         * Content Patcher land (we can check it here in C# with reflection,
         * but we're already here so let's try to be nicer).
         * TODO extend this for SVE support too if needed
         *
         * This checks one map layer (Back) at specific locations and maps
         * specific tile index values to new ones.
         * This approach should safely fix tiles that need fixing while
         * ignoring those that don't, and we don't have to check for the mod
         * or read its config.
         *
         * Back layer edits:
         *   256, 400, 401, 1964, 1965 -> 175
         *   329 -> 351
         *   405 -> 999
         * extremely lucky that i can omit 1966 to avoid special-casing the
         * new door area
         *
         * Buildings layer edits (not currently used):
         *   383, 384, 385 -> remove
         *
         * Front layer edits (not currently used):
         *   358, 359, 360 -> remove
         */
        public static void CleanMapTilesAroundHouse()
        {
            GameLocation forest = Game1.getLocationFromName("Forest");
            if (forest is null || forest.map is null) {
                return;
            }
            Layer back = forest.map.GetLayer("Back");
            //Layer buildings = forest.map.GetLayer("Buildings");
            //Layer front = forest.map.GetLayer("Front");
            if (back is null) { // || buildings is null || front is null) {
                return;
            }
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
            //var buildingsList = new List<Vector2>();
            //var frontList = new List<Vector2>();

            /* -1 in the value here means delete (null) */
            var convertDict = new Dictionary<int, int>() {
                {256, 175},
                {400, 175},
                {401, 175},
                {1964, 175},
                {1965, 175},
                {329, 351},
                {405, 999},
                {383, -1},
                {384, -1},
                {385, -1},
                {358, -1},
                {359, -1},
                {360, -1},
            };
            /* if you restore the other two layers, put the loop in a delegate
             * so you can reuse it in the other two loops
            Action<Vector2, Layer> mutate = delegate(Vector2 coords, Layer layer) { */
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
        }
    }
}
