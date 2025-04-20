using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace ichortower_HatMouseLacey;

internal sealed class LCModData
{
    private static string LC = HML.CPId;

    private static Dictionary<Farmer, HashSet<string>> HatsCache = new();
    private static Dictionary<Farmer, Timer> HatTimers = new();

    //
    // Read (deserialize) the set of shown hats from this farmer's modData.
    //
    private static HashSet<string> ReadHats(Farmer who)
    {
        if (who is null || !who.modData.TryGetValue($"{LC}/HatsShown", out string serial)) {
            return new HashSet<string>();
        }
        string[] items = serial.Trim('[',']').Split(",")
                .Select(s => s.Trim('"')).ToArray();
        HashSet<string> ret = new();
        foreach (string s in items) {
            ret.Add(s);
        }
        return ret;
    }

    //
    // Write the set of shown hats out to modData.
    // This waits for a short time before writing, to allow batching and avoid thrashing
    // during save migration and hat jubilees. In normal gameplay, this won't matter.
    //
    private static void WriteHats(Farmer who)
    {
        if (HatTimers.TryGetValue(who, out Timer t)) {
            t.Stop();
            t.Dispose();
            HatTimers.Remove(who);
        }
        // 100 ms is very generous
        Timer delay = new Timer(100);
        delay.Elapsed += delegate(object sender, ElapsedEventArgs e) {
            string serial = "[" + String.Join(",", HatsShown(who).ToArray()
                    .Select(s => $"\"{s}\"")) + "]";
            who.modData[$"{LC}/HatsShown"] = serial;
            delay.Stop();
            delay.Dispose();
            HatTimers.Remove(who);
        };
        delay.AutoReset = false;
        delay.Enabled = true;
        HatTimers[who] = delay;
    }

    public static void ClearCache()
    {
        HatsCache.Clear();
        // this should never matter but let's handle it anyway
        foreach (var t in HatTimers.Values) {
            t.Stop();
            t.Dispose();
        }
        HatTimers.Clear();
    }

    //
    // Return the set of hats this player has shown to Lacey.
    // The set is cached after the first read so we don't have to keep deserializing the list.
    //
    public static HashSet<string> HatsShown(Farmer who)
    {
        if (who is null) {
            return new HashSet<string>();
        }
        if (!HatsCache.ContainsKey(who)) {
            HatsCache.Add(who, ReadHats(who));
        }
        return HatsCache[who];
    }

    public static bool HasShownHat(Farmer who, string id)
    {
        return HatsShown(who).Contains(id);
    }

    public static bool AddShownHat(Farmer who, string id)
    {
        bool ret = HatsShown(who).Add(id);
        if (ret) {
            WriteHats(who);
        }
        return ret;
    }

    public static bool RemoveShownHat(Farmer who, string id)
    {
        bool ret = HatsShown(who).Remove(id);
        if (ret) {
            WriteHats(who);
        }
        return ret;
    }

    public static bool ClearShownHats(Farmer who)
    {
        var list = HatsShown(who);
        bool ret = list.Count > 0;
        list.Clear();
        if (ret) {
            WriteHats(who);
        }
        return ret;
    }

    //
    // Get the player's current cruelty score.
    // Unlike hats, this isn't cached, since it's much less work to read.
    //
    public static int CrueltyScore(Farmer who)
    {
        if (!who.modData.TryGetValue($"{LC}/CrueltyScore", out string sScore)) {
            sScore = "0";
        }
        if (int.TryParse(sScore, out int ret)) {
            return ret;
        }
        return 0;
    }

    //
    // Write out the player's cruelty score.
    // Unlike hats, no timer/batching support, since I don't need it.
    //
    public static void SetCrueltyScore(Farmer who, int score)
    {
        who.modData[$"{LC}/CrueltyScore"] = Convert.ToString(score);
    }

}
