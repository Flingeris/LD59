using System;
using System.Collections;
using UnityEngine;

public sealed class FirstRunTutorialController : IDisposable
{
    private const string TutorialCompletedKey = "bellgrave_tutorial_v1_completed";
    private const float IntroFadeDuration = 0.45f;
    private const float TutorialEnemySpawnOffsetX = -1.75f;

    private readonly Main main;
    private readonly string tutorialBellId;
    private readonly string tutorialEnemyId;

    private TutorialOverlayView overlayView;
    private LaneEnemy tutorialEnemy;
    private bool isDisposed;
    private bool isRunning;
    private bool bellSummoned;
    private bool tutorialEnemyKilled;
    private bool faithCollected;

    public FirstRunTutorialController(Main main, string tutorialBellId, string tutorialEnemyId)
    {
        this.main = main;
        this.tutorialBellId = tutorialBellId;
        this.tutorialEnemyId = tutorialEnemyId;

        if (main != null)
        {
            main.BellSummoned += HandleBellSummoned;
            main.FaithCollected += HandleFaithCollected;
            main.EnemyKilled += HandleEnemyKilled;
        }
    }

    public bool ShouldRunFirstRunTutorial => PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 0;
    public bool BlocksWaveProgress { get; private set; }
    public bool BlocksNightCompletion { get; private set; }

    public IEnumerator PlayStartupIntroSequence()
    {
        var overlay = ResolveOverlay();
        if (overlay == null)
        {
            yield break;
        }

        overlay.ShowBlackBackdropImmediate();
        yield return overlay.PlayTypedMessage("The old keeper, Gravey Jones, is dead.", true);
        yield return overlay.PlayTypedMessage("Without him, the cemetery stands unguarded.", true);
    }

    public IEnumerator FadeIntoGameplaySequence()
    {
        var overlay = ResolveOverlay();
        if (overlay == null)
        {
            yield break;
        }

        overlay.HideMessage();
        yield return overlay.FadeBackdropTo(0f, IntroFadeDuration);
        overlay.HideImmediate();
    }

    public IEnumerator RunNightTutorialSequence()
    {
        if (!ShouldRunFirstRunTutorial)
        {
            yield break;
        }

        var overlay = ResolveOverlay();
        if (overlay == null)
        {
            yield break;
        }

        isRunning = true;
        BlocksWaveProgress = true;
        BlocksNightCompletion = true;
        bellSummoned = false;
        tutorialEnemyKilled = false;
        faithCollected = false;
        tutorialEnemy = null;

        yield return overlay.PlayTypedMessage("Each night, the living come to rob the dead.", false);

        var enemySpawnResult = main.TrySpawnScriptedEnemy(tutorialEnemyId);
        if (!enemySpawnResult.IsSuccess || enemySpawnResult.SpawnedEnemy == null)
        {
            ReleaseTutorialLocks();
            yield break;
        }

        tutorialEnemy = enemySpawnResult.SpawnedEnemy;
        tutorialEnemy.transform.position += new Vector3(TutorialEnemySpawnOffsetX, 0f, 0f);
        yield return new WaitForSeconds(1f);
        SetTutorialEnemyPaused(true);

        yield return overlay.PlayTypedMessage("Now you are the new keeper.", false);

        if (!main.TryGetBellWorldObject(tutorialBellId, out var bellWorldObject))
        {
            ReleaseTutorialLocks();
            yield break;
        }

        overlay.ShowWorldMarker(bellWorldObject.transform, "RING BELL", new Color(1f, 0.86f, 0.36f, 1f));
        yield return overlay.PlayTypedMessage("Ring the bells to signal the graveyard's defenders.", false);
        while (!bellSummoned)
        {
            yield return null;
        }

        overlay.HideWorldMarker();
        SetTutorialEnemyPaused(false);

        yield return overlay.PlayTypedMessage("The dead cannot linger here for long.", false);
        yield return overlay.PlayTypedMessage(
            "Each defender has limited <color=#F2D35E>time</color> to serve this holy ground", false);

        while (!tutorialEnemyKilled)
        {
            yield return null;
        }

        yield return overlay.PlayTypedMessage(
            "Raising undead defenders costs faith.",
            false);
        yield return overlay.PlayTypedMessage(
            "To get it you must pray at sacred place, but it can take time.",
            false);

        if (!main.TryGetNightPoiByType(NightPoiType.FaithPoint, out var faithPointPoi))
        {
            ReleaseTutorialLocks();
            yield break;
        }

        overlay.ShowWorldMarker(faithPointPoi.transform, "COLLECT FAITH", new Color(0.56f, 0.9f, 1f, 1f));
        while (!faithCollected)
        {
            yield return null;
        }

        overlay.HideWorldMarker();
        yield return overlay.PlayTypedMessage(
            "Survive to the final night, and take Gravey Jones's place.",
            false);

        PlayerPrefs.SetInt(TutorialCompletedKey, 1);
        PlayerPrefs.Save();

        ReleaseTutorialLocks();
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        if (main != null)
        {
            main.BellSummoned -= HandleBellSummoned;
            main.FaithCollected -= HandleFaithCollected;
            main.EnemyKilled -= HandleEnemyKilled;
        }

        overlayView?.HideImmediate();
    }

    private TutorialOverlayView ResolveOverlay()
    {
        if (overlayView != null)
        {
            return overlayView;
        }

        overlayView = G.ui != null ? G.ui.EnsureTutorialOverlay() : null;
        return overlayView;
    }

    private void ReleaseTutorialLocks()
    {
        SetTutorialEnemyPaused(false);
        overlayView?.HideWorldMarker();
        overlayView?.HideImmediate();
        BlocksWaveProgress = false;
        BlocksNightCompletion = false;
        isRunning = false;
    }

    private void HandleBellSummoned(string bellId, LaneUnit spawnedUnit)
    {
        if (!isRunning || string.IsNullOrWhiteSpace(bellId) || spawnedUnit == null)
        {
            return;
        }

        if (!string.Equals(bellId, tutorialBellId, StringComparison.Ordinal))
        {
            return;
        }

        bellSummoned = true;
    }

    private void HandleFaithCollected(int collectedFaith)
    {
        if (!isRunning || collectedFaith <= 0)
        {
            return;
        }

        faithCollected = true;
    }

    private void HandleEnemyKilled(LaneEnemy laneEnemy)
    {
        if (!isRunning || laneEnemy == null || tutorialEnemy == null)
        {
            return;
        }

        if (!ReferenceEquals(laneEnemy, tutorialEnemy))
        {
            return;
        }

        tutorialEnemyKilled = true;
    }

    private void SetTutorialEnemyPaused(bool paused)
    {
        if (tutorialEnemy == null)
        {
            return;
        }

        tutorialEnemy.enabled = !paused;
    }
}