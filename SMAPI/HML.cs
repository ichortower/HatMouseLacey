using StardewModdingAPI;

namespace ichortower_HatMouseLacey
{
    /*
     * Holds constants and static references that I need to use in multiple
     * source files around the namespace.
     *
     * The Monitor, ModHelper, and Manifest aren't available until mod entry,
     * so it's ModEntry's job to set them.
     */
    internal sealed class HML
    {
        public static string CPId = "ichortower.HatMouseLacey";
        public static string CoreId = "ichortower.HatMouseLacey.Core";
        public static string LaceyInternalName = $"{CPId}_Lacey";
        public static string MailPrefix = $"{CPId}_Mail_";
        public static string EventPrefix = $"{CPId}_Event_";
        public static string QuestPrefix = $"{CPId}_Quest_";
        public static string CTPrefix = $"{CPId}_CT_";

        public static IMonitor Monitor = null!;
        public static IModHelper ModHelper = null!;
        public static IManifest Manifest = null!;
    }
}
