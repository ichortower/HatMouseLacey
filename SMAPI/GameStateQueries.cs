using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;

namespace ichortower_HatMouseLacey;

internal sealed class LCGameStateQueries
{
    public static void Register()
    {
        GameStateQuery.Register($"{HML.CPId}_PLAYER_HAS_SHOWN_HAT",
                PLAYER_HAS_SHOWN_HAT);
        GameStateQuery.Register($"{HML.CPId}_PLAYER_SHOWN_HAT_COUNT",
                PLAYER_SHOWN_HAT_COUNT);
    }

    public static bool PLAYER_HAS_SHOWN_HAT(string[] query, GameStateQueryContext context)
    {
        if (!ArgUtility.TryGet(query, 1, out string playerKey, out string error) ||
                !ArgUtility.TryGet(query, 2, out string firstId, out error)) {
            return GameStateQuery.Helpers.ErrorResult(query, error);
        }
        return GameStateQuery.Helpers.WithPlayer(context.Player, playerKey,
            (Farmer target) => {
                if (LCModData.HatsShown(target).Contains(firstId)) {
                    return true;
                }
                return GameStateQuery.Helpers.AnyArgMatches(query, 3,
                    (string id) => LCModData.HatsShown(target).Contains(id));
            });
    }

    public static bool PLAYER_SHOWN_HAT_COUNT(string[] query, GameStateQueryContext context)
    {
        if (!ArgUtility.TryGet(query, 1, out string playerKey, out string error) ||
                !ArgUtility.TryGetInt(query, 2, out int minCount, out error) ||
                !ArgUtility.TryGetOptionalInt(query, 3, out int maxCount, out error,
                    defaultValue: -1)) {
            return GameStateQuery.Helpers.ErrorResult(query, error);
        }
        return GameStateQuery.Helpers.WithPlayer(context.Player, playerKey,
            (Farmer target) => {
                if (!HML.HatReactionsAvailable(target)) {
                    return false;
                }
                int haveCount = LCModData.HatsShown(target).Count;
                if (haveCount < minCount) {
                    return false;
                }
                if (maxCount > 0 && haveCount > maxCount) {
                    return false;
                }
                return true;
            });
    }
}
