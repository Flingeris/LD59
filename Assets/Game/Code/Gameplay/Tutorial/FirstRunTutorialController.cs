using System;
using System.Collections;
using UnityEngine;

public sealed class FirstRunTutorialController : IDisposable
{
    private const string TutorialCompletedKey = "bellgrave_tutorial_v1_completed";
    private const float IntroFadeDuration = 0.45f;
    private const float TutorialEnemySpawnOffsetX = -1.75f;
    private const string GraveyJonesMarkup = "<link=\"wave\"><color=#D95C5C>Gravey Jones</color></link>";
    private const string GraveyJonesPossessiveMarkup = "<link=\"wave\"><color=#D95C5C>Gravey Jones's</color></link>";
    private const string BellsMarkup = "<color=#F2D35E>bells</color>";
    private const string FaithMarkup = "<color=#7EDBFF>faith</color>";
    private const string LimitedTimeMarkup = "<color=#F2D35E>limited time</color>";
    private const string SacredPlacesMarkup = "<color=#7EDBFF>sacred place</color>";
    private const string RepairCemeteryMarkup = "<color=#C8E1BA>repair cemetery</color>";

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
    private bool cemeteryDamaged;
    private bool cemeteryRepaired;

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
            main.CemeteryDamaged += HandleCemeteryDamaged;
            main.CemeteryRepaired += HandleCemeteryRepaired;
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
        yield return overlay.PlayTypedMessage($"The old keeper, {GraveyJonesMarkup}, is dead.", true);
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
        cemeteryDamaged = false;
        cemeteryRepaired = false;
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

        overlay.ShowWorldMarker(
            bellWorldObject.transform,
            "RING BELL",
            new Color(1f, 0.86f, 0.36f, 1f),
            TutorialWorldMarkerAnchor.BellPosition);
        yield return overlay.PlayTypedMessage(
            $"Ring the {BellsMarkup} to signal the graveyard's defenders.",
            false);
        while (!bellSummoned)
        {
            yield return null;
        }

        overlay.HideWorldMarker();
        SetTutorialEnemyPaused(false);

        yield return overlay.PlayTypedMessage("The dead cannot linger here for long.", false);
        yield return overlay.PlayTypedMessage(
            $"Each defender has {LimitedTimeMarkup} to serve this holy ground.",
            false);

        while (!tutorialEnemyKilled)
        {
            yield return null;
        }

        yield return overlay.PlayTypedMessage(
            $"Raising undead defenders costs {FaithMarkup}.",
            false);
        yield return overlay.PlayTypedMessage(
            $"To get it you must pray at {SacredPlacesMarkup}, but it can take time.",
            false);

        if (!main.TryGetNightPoiByType(NightPoiType.FaithPoint, out var faithPointPoi))
        {
            ReleaseTutorialLocks();
            yield break;
        }

        overlay.ShowWorldMarker(
            faithPointPoi.transform,
            "COLLECT FAITH",
            new Color(0.56f, 0.9f, 1f, 1f),
            TutorialWorldMarkerAnchor.FaithPosition);
        while (!faithCollected)
        {
            yield return null;
        }

        overlay.HideWorldMarker();
        yield return overlay.PlayTypedMessage(
            $"Survive until the final night, and take {GraveyJonesPossessiveMarkup} place.",
            false);

        BlocksWaveProgress = false;
        BlocksNightCompletion = false;

        while (!cemeteryDamaged)
        {
            yield return null;
        }

        if (!main.TryGetNightPoiByType(NightPoiType.RepairPoint, out var repairPointPoi))
        {
            CompleteTutorial();
            yield break;
        }

        overlay.ShowWorldMarker(
            repairPointPoi.transform,
            "REPAIR",
            new Color(0.78f, 0.88f, 0.72f, 1f),
            TutorialWorldMarkerAnchor.RepairPosition);
        yield return overlay.PlayTypedMessage(
            $"You can {RepairCemeteryMarkup}, but it can take a while.",
            false);

        while (!cemeteryRepaired)
        {
            yield return null;
        }

        CompleteTutorial();
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
            main.CemeteryDamaged -= HandleCemeteryDamaged;
            main.CemeteryRepaired -= HandleCemeteryRepaired;
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

    private void CompleteTutorial()
    {
        PlayerPrefs.SetInt(TutorialCompletedKey, 1);
        PlayerPrefs.Save();
        ReleaseTutorialLocks();
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

    private void HandleCemeteryDamaged(int damageAmount)
    {
        if (!isRunning || damageAmount <= 0)
        {
            return;
        }

        cemeteryDamaged = true;
    }

    private void HandleCemeteryRepaired(int repairedAmount)
    {
        if (!isRunning || repairedAmount <= 0)
        {
            return;
        }

        cemeteryRepaired = true;
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