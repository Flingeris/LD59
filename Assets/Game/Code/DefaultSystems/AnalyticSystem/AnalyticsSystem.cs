using GameAnalyticsSDK;
using UnityEngine;

public static class AnalyticsSystem
{
    private static bool _initialized;

    public static void Init()
    {
#if UNITY_EDITOR
#else
      if (_initialized)return;
        GameAnalytics.Initialize();
        _initialized = true;
            Debug.Log("[Analytics] GameAnalytics initialized");
#endif
    }

    public static void OnGameStarted()
    {
        if (!_initialized) return;

        GameAnalytics.NewDesignEvent("game:started");
    }

    public static void OnLevelChanged(int levelIndex, string levelName)
    {
        if (!_initialized) return;

        levelName = string.IsNullOrWhiteSpace(levelName) ? "unknown" : levelName;

        GameAnalytics.NewProgressionEvent(
            GAProgressionStatus.Start,
            "run",
            $"level_{levelIndex}",
            levelName
        );

        GameAnalytics.NewDesignEvent($"level:changed:{levelName}", levelIndex);
    }

    public static void OnLevelCompleted(int levelIndex, string levelName)
    {
        if (!_initialized) return;

        levelName = string.IsNullOrWhiteSpace(levelName) ? "unknown" : levelName;

        GameAnalytics.NewProgressionEvent(
            GAProgressionStatus.Complete,
            "run",
            $"level_{levelIndex}",
            levelName
        );
    }

    public static void OnLevelFailed(int levelIndex, string levelName)
    {
        if (!_initialized) return;

        levelName = string.IsNullOrWhiteSpace(levelName) ? "unknown" : levelName;

        GameAnalytics.NewProgressionEvent(
            GAProgressionStatus.Fail,
            "run",
            $"level_{levelIndex}",
            levelName
        );
    }

    public static void OnGameEnded(string result, int totalLevelsPassed)
    {
        if (!_initialized) return;

        result = string.IsNullOrWhiteSpace(result) ? "unknown" : result.ToLowerInvariant();

        GameAnalytics.NewDesignEvent($"run:ended:{result}", totalLevelsPassed);
    }

    public static void OnError(string message)
    {
        if (!_initialized) return;
        if (string.IsNullOrWhiteSpace(message)) return;

        GameAnalytics.NewErrorEvent(GAErrorSeverity.Error, message);
    }

    public static void OnRewardPicked(string rewardId)
    {
        if (!_initialized) return;
        if (string.IsNullOrWhiteSpace(rewardId)) rewardId = "unknown";

        GameAnalytics.NewDesignEvent($"reward:picked:{rewardId}");
    }

    public static void TestEvent(string eventId)
    {
        if (!_initialized) return;
        if (string.IsNullOrWhiteSpace(eventId)) eventId = "debug:unknown";
        GameAnalytics.NewDesignEvent(eventId);
    }
}
