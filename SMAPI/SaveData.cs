using StardewModdingAPI;
using System.Collections.Generic;

/*
 * DEPRECATED since:
 *
 * Do not use the classes in this file. It was replaced with LCModData to fix
 * errors with multiplayer.
 *
 * It is kept around in order to have the class definitions to convert the
 * data from old saves. It will be removed in a future release, when that
 * conversion is no longer supported.
 */

namespace ichortower_HatMouseLacey
{
    internal sealed class LCHatsShown
    {
        public List<int> ids = new List<int>();
    }

    internal sealed class LCCrueltyScore
    {
        public int val = 0;
    }

    internal class LCSaveData
    {
        /* Tracks which hats you have shown to Lacey. */
        private static LCHatsShown _hatsShown = null!;
        /* How many mean points you have earned in heart events. */
        private static LCCrueltyScore _crueltyScore = null!;

        public static IModHelper HELPER = ModEntry.HELPER;

        public static void ClearCache()
        {
            _hatsShown = null;
            _crueltyScore = null;
        }

        public static void AddShownHat(int id)
        {
            if (_hatsShown is null) {
                _hatsShown = HELPER.Data.ReadSaveData<LCHatsShown>("HatsShown");
                if (_hatsShown is null) {
                    _hatsShown = new();
                }
            }
            _hatsShown.ids.Add(id);
            HELPER.Data.WriteSaveData<LCHatsShown>("HatsShown", _hatsShown);
        }

        public static bool HasShownHat(int id)
        {
            if (_hatsShown is null) {
                _hatsShown = HELPER.Data.ReadSaveData<LCHatsShown>("HatsShown");
                if (_hatsShown is null) {
                    _hatsShown = new();
                }
            }
            return _hatsShown.ids.Contains(id);
        }

        public static int CrueltyScore
        {
            get {
                if (_crueltyScore is null) {
                    _crueltyScore = HELPER.Data.ReadSaveData<LCCrueltyScore>("CrueltyScore");
                    if (_crueltyScore is null) {
                        _crueltyScore = new();
                    }
                }
                return _crueltyScore.val;
            }
            set {
                if (_crueltyScore is null) {
                    _crueltyScore = HELPER.Data.ReadSaveData<LCCrueltyScore>("CrueltyScore");
                    if (_crueltyScore is null) {
                        _crueltyScore = new();
                    }
                }
                _crueltyScore.val = value;
                HELPER.Data.WriteSaveData<LCCrueltyScore>("CrueltyScore", _crueltyScore);
            }
        }
    }

}
