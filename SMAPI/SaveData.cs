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
}
