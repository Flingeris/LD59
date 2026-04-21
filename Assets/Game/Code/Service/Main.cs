using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class Main : MonoBehaviour
{
    private enum DayUpgradeOfferCategory
    {
        Economy = 0,
        Combat = 1,
        Utility = 2
    }

    private const int DailyUpgradeOfferCount = 3;

    [SerializeField] private SingleLaneHost singleLaneHost;
    [SerializeField] private SingleLaneEncounterCoordinator laneEncounterCoordinator;
    [SerializeField] private KeeperActor keeperActor;
    [SerializeField] private bool showIntroScreenOnStartup = true;
    [SerializeField] private bool showFirstRunTutorial = true;
    [Min(0f)] [SerializeField] private float titleScreenFadeDuration = 1.25f;
    [SerializeField] private string firstRunTutorialBellId = "bell_small";
    [SerializeField] private string firstRunTutorialEnemyId = "en1";

    private BellSystem bellSystem;
    private CemeteryStateSystem cemeteryStateSystem;
    private FaithCollectionSystem faithCollectionSystem;
    private UpgradeSystem upgradeSystem;
    private SingleLaneUnitSpawner unitSpawner;
    private SingleLaneEnemySpawner enemySpawner;
    private WaveSystem waveSystem;
    private KeeperMovementSystem keeperMovementSystem;
    private NightPoiSystem nightPoiSystem;
    private List<NightDefinition> nightDefinitions;
    private NightDefinition activeNightDefinition;
    private readonly Dictionary<string, BellWorldObject> bellWorldObjectsById = new(StringComparer.Ordinal);
    private bool laneSetupWarningShown;
    private bool keeperBindingWarningShown;
    private bool keeperSceneBindingInitialized;
    private bool startupSequenceInProgress;
    private FirstRunTutorialController firstRunTutorialController;
    private BellUnlockAnnouncementController bellUnlockAnnouncementController;
    private string pendingBellInteractionId = string.Empty;
    private readonly List<string> readyWaveSpawnEnemyIds = new();

    public RunState RunState { get; private set; }
    public event Action<string, LaneUnit> BellSummoned;
    public event Action<int> FaithCollected;
    public event Action<LaneEnemy> EnemyKilled;
    public event Action<int> CemeteryDamaged;
    public event Action<int> CemeteryRepaired;

    public bool IsNightWaveActive => RunState != null && RunState.CurrentPhase == GamePhase.Night &&
                                     waveSystem != null && waveSystem.IsRunning;

    public float CurrentNightElapsedSeconds => waveSystem != null ? waveSystem.ElapsedTime : 0f;
    public float CurrentNightDurationSeconds => waveSystem != null ? waveSystem.DurationSeconds : 0f;
    public bool HasUpcomingNightWave => waveSystem != null && waveSystem.HasUpcomingWave;
    public float NextNightWaveTriggerTime => waveSystem != null ? waveSystem.NextWaveTriggerTime : 0f;
    public float PreviousNightWaveTriggerTime => waveSystem != null ? waveSystem.PreviousWaveTriggerTime : 0f;

    public IReadOnlyList<NightWaveEntry> CurrentNightWaveEntries => waveSystem != null
        ? waveSystem.ActiveEntries
        : Array.Empty<NightWaveEntry>();

    private void Awake()
    {
        Time.timeScale = 1f;
        G.main = this;
        RunState = RunState.CreateInitial(
            BellgraveBalance.Run.InitialCemeteryState,
            BellgraveBalance.Run.InitialKeeperMoveSpeed,
            BellgraveBalance.Run.StartingNightFaith,
            BellgraveBalance.Run.FaithCollectionPayoutAmount,
            BellgraveBalance.Run.FaithCollectionIntervalSeconds,
            BellgraveBalance.Run.NightCemeteryRepairAmount,
            BellgraveBalance.Run.NightCemeteryRepairIntervalSeconds);
        bellSystem = new BellSystem();
        cemeteryStateSystem = new CemeteryStateSystem();
        faithCollectionSystem = new FaithCollectionSystem();
        upgradeSystem = new UpgradeSystem();
        unitSpawner = new SingleLaneUnitSpawner();
        enemySpawner = new SingleLaneEnemySpawner();
        waveSystem = new WaveSystem();
        keeperMovementSystem = new KeeperMovementSystem();
        nightPoiSystem = new NightPoiSystem();
        nightDefinitions = BuildNightDefinitions();
        firstRunTutorialController =
            new FirstRunTutorialController(this, firstRunTutorialBellId, firstRunTutorialEnemyId);
        bellUnlockAnnouncementController = new BellUnlockAnnouncementController(this);

        if (laneEncounterCoordinator != null)
        {
            laneEncounterCoordinator.OnEnemyBreakthrough = HandleEnemyBreakthrough;
            laneEncounterCoordinator.OnEnemyCemeteryAttack = HandleEnemyCemeteryAttack;
            laneEncounterCoordinator.OnEnemyKilled = HandleEnemyKilled;
        }
    }

    private void Start()
    {
        TryResolveKeeperActor();
        RebuildNightPoiRegistry();
        G.audioSystem.Play(SoundId.Ambient_Forest);
        G.audioSystem.Play(SoundId.Music_Main);
        BindHud();
        BeginStartupSequence();
    }

    private void OnDestroy()
    {
        firstRunTutorialController?.Dispose();
        UnbindHud();
    }

    private void Update()
    {
        if (startupSequenceInProgress)
        {
            HandleDebugInput();
            return;
        }

        UpdateLoseCondition();
        if (RunState == null || RunState.CurrentPhase == GamePhase.Defeat || RunState.CurrentPhase == GamePhase.Win)
        {
            return;
        }

        UpdateKeeper();
        ProcessPendingBellInteraction();
        UpdateNightFaithCollection();
        UpdateNightCemeteryRepair();
        UpdateWaveSpawning();
        UpdateNightCompletion();
        HandleDebugInput();
    }

    public bool EnterDay()
    {
        if (RunState.CurrentPhase == GamePhase.Day ||
            RunState.CurrentPhase == GamePhase.Defeat ||
            RunState.CurrentPhase == GamePhase.Win)
        {
            return false;
        }

        if (RunState.CurrentNight > 0)
        {
            RunState.CurrentDay++;
        }

        RunState.CurrentPhase = GamePhase.Day;
        activeNightDefinition = null;
        ClearPendingBellInteraction();
        faithCollectionSystem.EndNight(RunState);
        cemeteryStateSystem.ResetNightRepairProgress(RunState);
        RunState.RemainingInstantNightRepairCharges = 0;
        waveSystem.StopWave();
        ClearLaneCombatants();
        StopKeeperMovement();
        GenerateDayUpgradeOffers();
        RefreshPresentation();
        G.audioSystem.Play(SoundId.SFX_LevelTransition);
        PlayPhaseTransitionCue(GamePhase.Day);
        return true;
    }

    public bool EnterNight()
    {
        if (RunState.CurrentPhase == GamePhase.Night ||
            RunState.CurrentPhase == GamePhase.Defeat ||
            RunState.CurrentPhase == GamePhase.Win)
        {
            return false;
        }

        var isFirstNightOfRun = RunState.CurrentNight <= 0;
        RunState.CurrentNight++;
        RunState.CurrentPhase = GamePhase.Night;
        ClearPendingBellInteraction();
        faithCollectionSystem.StartNight(RunState);
        cemeteryStateSystem.ResetNightRepairProgress(RunState);
        RunState.RemainingInstantNightRepairCharges = Mathf.Max(0, RunState.InstantNightRepairChargesPerNight);
        PrepareKeeperForNight();
        var spawnedNightUnlockBell = TrySpawnNightUnlockedBells();
        TryQueueBellUnlockAnnouncementForCurrentNight(spawnedNightUnlockBell);
        StartNightWave();
        RefreshPresentation();
        TryPlayPendingBellUnlockAnnouncement();
        PlayPhaseTransitionCue(GamePhase.Night);
        if (isFirstNightOfRun)
        {
            AnalyticsSystem.OnGameStarted();
        }

        TrackNightStarted();
        return true;
    }

    public bool TryStartNightFromDayScreen()
    {
        if (RunState == null || RunState.CurrentPhase != GamePhase.Day)
        {
            return false;
        }

        return EnterNight();
    }

    public UpgradePurchaseResult TryPurchaseUpgrade(string upgradeId)
    {
        if (RunState == null)
        {
            return UpgradePurchaseResult.Failure(UpgradePurchaseFailureReason.InvalidState);
        }

        if (RunState.CurrentPhase != GamePhase.Day)
        {
            var wrongPhaseResult = UpgradePurchaseResult.Failure(UpgradePurchaseFailureReason.WrongPhase);
            Debug.LogWarning($"Upgrade purchase failed for '{upgradeId}': {wrongPhaseResult.FailureReason}");
            return wrongPhaseResult;
        }

        if (!IsUpgradeOfferedToday(upgradeId))
        {
            var notOfferedResult = UpgradePurchaseResult.Failure(UpgradePurchaseFailureReason.UpgradeNotOfferedToday);
            Debug.LogWarning($"Upgrade purchase failed for '{upgradeId}': {notOfferedResult.FailureReason}");
            return notOfferedResult;
        }

        var purchaseResult = upgradeSystem.TryPurchaseUpgrade(upgradeId, RunState);
        if (!purchaseResult.IsSuccess)
        {
            Debug.LogWarning($"Upgrade purchase failed for '{upgradeId}': {purchaseResult.FailureReason}");
            return purchaseResult;
        }

        if (RunState.CurrentDayUpgradeOfferIds != null)
        {
            for (var i = 0; i < RunState.CurrentDayUpgradeOfferIds.Count; i++)
            {
                if (RunState.CurrentDayUpgradeOfferIds[i] == upgradeId)
                {
                    RunState.CurrentDayUpgradeOfferIds[i] = string.Empty;
                    break;
                }
            }
        }

        Debug.Log($"Purchased upgrade '{purchaseResult.UpgradeDef.Id}'");
        AnalyticsSystem.OnRewardPicked(purchaseResult.UpgradeDef.Id);
        RefreshPresentation();
        return purchaseResult;
    }

    public void AddFaith(int amount)
    {
        RunState.Faith += amount;
    }

    public void AddGold(int amount)
    {
        RunState.Gold += amount;
    }

    public void DamageCemetery(int amount)
    {
        if (RunState == null || cemeteryStateSystem == null)
        {
            return;
        }

        var previousCemeteryState = RunState.CemeteryState;
        var didChangeState = cemeteryStateSystem.ApplyBreakthroughDamage(RunState, amount);
        if (didChangeState)
        {
            var damageApplied = Mathf.Max(0, previousCemeteryState - RunState.CemeteryState);
            if (damageApplied > 0)
            {
                CemeteryDamaged?.Invoke(damageApplied);
            }

            RefreshPresentation();
        }

        TryEnterDefeat();
    }

    public bool TryGetNightPoiProgress(string poiId, NightPoiType poiType, out float normalizedProgress)
    {
        normalizedProgress = 0f;

        if (RunState == null || RunState.CurrentPhase != GamePhase.Night || string.IsNullOrWhiteSpace(poiId))
        {
            return false;
        }

        return poiType switch
        {
            NightPoiType.FaithPoint => TryGetFaithPoiProgress(poiId, out normalizedProgress),
            NightPoiType.RepairPoint => TryGetRepairPoiProgress(poiId, out normalizedProgress),
            _ => false
        };
    }

    public bool TryMoveKeeperTo(Vector2 targetPosition, string targetPointId = null)
    {
        if (RunState == null || RunState.Keeper == null || RunState.CurrentPhase != GamePhase.Night)
        {
            return false;
        }

        if (!TryResolveKeeperActor())
        {
            return false;
        }

        ClearPendingBellInteraction();
        keeperMovementSystem.SetMoveTarget(RunState.Keeper, targetPosition, targetPointId);
        return true;
    }

    public bool TryMoveKeeperToPoi(string poiId)
    {
        if (RunState == null || RunState.CurrentPhase != GamePhase.Night)
        {
            return false;
        }

        if (!TryResolveNightPoiById(poiId, out var poi))
        {
            Debug.LogWarning($"Keeper move failed: night poi '{poiId}' not found");
            return false;
        }

        return TryMoveKeeperTo(poi.GetKeeperTargetWorldPosition(), poi.Id);
    }

    public bool TryMoveKeeperToPoiType(NightPoiType poiType)
    {
        if (!TryResolveNightPoiByType(poiType, out var poi))
        {
            Debug.LogWarning($"Keeper move failed: night poi type '{poiType}' not found");
            return false;
        }

        return TryMoveKeeperToPoi(poi.Id);
    }

    public bool TryInteractWithBell(string bellId)
    {
        if (RunState == null || RunState.CurrentPhase != GamePhase.Night)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(bellId))
        {
            Debug.LogWarning("Bell interaction failed: empty bell id");
            return false;
        }

        var bellWorldObject = ResolveBellWorldObject(bellId);
        if (bellWorldObject == null)
        {
            var missingBellResult = BellRingResult.Failure(BellRingFailureReason.BellNotFound);
            PublishBellFeedback(null, missingBellResult);
            return false;
        }

        if (!IsKeeperReadyToUseBells())
        {
            var moveStarted = TryMoveKeeperToPoiType(NightPoiType.Bells);
            if (moveStarted)
            {
                pendingBellInteractionId = bellId;
            }

            return moveStarted;
        }

        if (bellWorldObject.IsOnCooldown)
        {
            var cooldownResult = BellRingResult.Failure(
                BellRingFailureReason.OnCooldown,
                bellWorldObject.CooldownRemainingSeconds);
            PublishBellFeedback(bellWorldObject, cooldownResult);
            return false;
        }

        ClearPendingBellInteraction();
        var bellResult = TryRingBell(bellId);
        if (bellResult.IsSuccess &&
            bellResult.SpawnResult != null &&
            bellResult.SpawnResult.IsSuccess &&
            bellResult.BellDef != null)
        {
            bellWorldObject.StartCooldown(bellResult.BellDef.CooldownSeconds);
            PublishBellFaithSpendEffect(bellWorldObject, bellResult);
            BellSummoned?.Invoke(bellId, bellResult.SpawnResult.SpawnedUnit);
        }

        PublishBellFeedback(bellWorldObject, bellResult);
        return bellResult.IsSuccess && bellResult.SpawnResult != null && bellResult.SpawnResult.IsSuccess;
    }

    private void HandleEnemyBreakthrough(LaneEnemy laneEnemy)
    {
        if (laneEnemy == null)
        {
            Debug.LogWarning("Enemy breakthrough failed: laneEnemy is missing");
            return;
        }

        if (laneEnemy.EnemyDef == null)
        {
            Debug.LogWarning("Enemy breakthrough failed: EnemyDef is missing");
            return;
        }

        Debug.Log($"Enemy breakthrough: '{laneEnemy.EnemyDef.Id}' started attacking cemetery");
    }

    private void HandleEnemyCemeteryAttack(LaneEnemy laneEnemy)
    {
        if (laneEnemy == null)
        {
            Debug.LogWarning("Enemy cemetery attack failed: laneEnemy is missing");
            return;
        }

        if (laneEnemy.EnemyDef == null)
        {
            Debug.LogWarning("Enemy cemetery attack failed: EnemyDef is missing");
            return;
        }

        var cemeteryDamage = laneEnemy.EnemyDef.Damage;
        DamageCemetery(cemeteryDamage);
        Debug.Log(
            $"Enemy cemetery attack: '{laneEnemy.EnemyDef.Id}' dealt {cemeteryDamage} cemetery damage");
    }

    private void HandleEnemyKilled(LaneEnemy laneEnemy)
    {
        if (RunState == null || RunState.CurrentPhase != GamePhase.Night)
        {
            return;
        }

        if (laneEnemy == null)
        {
            Debug.LogWarning("Enemy kill reward failed: laneEnemy is missing");
            return;
        }

        if (laneEnemy.EnemyDef == null)
        {
            Debug.LogWarning("Enemy kill reward failed: EnemyDef is missing");
            return;
        }

        EnemyKilled?.Invoke(laneEnemy);
        var goldReward = Mathf.Max(0, laneEnemy.EnemyDef.GoldReward);
        if (goldReward <= 0)
        {
            return;
        }

        AddGold(goldReward);
        G.HUD?.PlayGoldPickupEffect(laneEnemy.Position, goldReward);
        Debug.Log($"Enemy killed: '{laneEnemy.EnemyDef.Id}' awarded {goldReward} gold");
    }

    public BellRingResult TryRingBell(string bellId)
    {
        ValidateLanePrototypeSetup();

        var bellResult = bellSystem.TryRingBell(bellId, RunState);
        if (!bellResult.IsSuccess)
        {
            Debug.LogWarning($"Bell ring failed for '{bellId}': {bellResult.FailureReason}");
            return bellResult;
        }

        bellResult.SpawnResult = unitSpawner.TrySpawnPlayerUnit(bellResult.UnitDef, singleLaneHost, RunState);
        if (!bellResult.SpawnResult.IsSuccess)
        {
            RunState.Faith += bellResult.BellDef.FaithCost;
            Debug.LogWarning($"Failed to spawn unit for bell '{bellId}': {bellResult.SpawnResult.FailureReason}");
        }
        else
        {
            Debug.Log($"Bell '{bellId}' spawned unit '{bellResult.UnitDef.Id}'");
            RegisterPlayerUnitOnLane(bellResult.SpawnResult.SpawnedUnit);
        }

        return bellResult;
    }

    private bool IsKeeperReadyToUseBells()
    {
        return RunState?.Keeper != null &&
               RunState.Keeper.ActivityState != KeeperActivityState.Moving &&
               RunState.Keeper.InteractionState == KeeperInteractionState.Bells;
    }

    private void PublishBellFeedback(BellWorldObject bellWorldObject, BellRingResult bellResult)
    {
        if (bellWorldObject == null || bellResult == null)
        {
            return;
        }

        var popupText = BuildBellFeedbackText(bellResult);
        if (string.IsNullOrWhiteSpace(popupText))
        {
            return;
        }

        bellWorldObject.ShowFeedbackPopup(popupText, GetBellFeedbackColor(bellResult));
    }

    private void PublishBellFaithSpendEffect(BellWorldObject bellWorldObject, BellRingResult bellResult)
    {
        if (bellWorldObject == null || bellResult == null || bellResult.BellDef == null || G.HUD == null ||
            RunState == null)
        {
            return;
        }

        var faithSpent = Mathf.Max(0, bellResult.BellDef.FaithCost + RunState.BellFaithCostModifier);
        if (faithSpent <= 0)
        {
            return;
        }

        G.HUD.PlayFaithSpendEffect(bellWorldObject.transform.position, faithSpent);
    }

    private static string BuildBellFeedbackText(BellRingResult bellResult)
    {
        if (bellResult == null)
        {
            return string.Empty;
        }

        return bellResult.FailureReason switch
        {
            BellRingFailureReason.OnCooldown => $"Recharging {bellResult.CooldownRemainingSeconds:0.0}s",
            BellRingFailureReason.NotEnoughFaith => "Need more faith",
            _ => string.Empty
        };
    }

    private static Color GetBellFeedbackColor(BellRingResult bellResult)
    {
        if (bellResult != null && bellResult.FailureReason == BellRingFailureReason.OnCooldown)
        {
            return new Color(1f, 0.72f, 0.38f);
        }

        return new Color(1f, 0.45f, 0.45f);
    }

    private void ProcessPendingBellInteraction()
    {
        if (string.IsNullOrWhiteSpace(pendingBellInteractionId) || RunState?.CurrentPhase != GamePhase.Night)
        {
            return;
        }

        if (!IsKeeperReadyToUseBells())
        {
            return;
        }

        var bellId = pendingBellInteractionId;
        pendingBellInteractionId = string.Empty;

        TryInteractWithBell(bellId);
    }

    private void ClearPendingBellInteraction()
    {
        pendingBellInteractionId = string.Empty;
    }

    private EnemySpawnResult TrySpawnEnemyById(string enemyId)
    {
        if (string.IsNullOrWhiteSpace(enemyId))
        {
            Debug.LogWarning("Wave enemy spawn failed: empty enemy id");
            return EnemySpawnResult.Failure(UnitSpawnFailureReason.MissingPrefab);
        }

        var enemyDef = CMS.Get<EnemyDef>(enemyId);
        if (enemyDef == null)
        {
            Debug.LogWarning($"Wave enemy spawn failed: enemy def '{enemyId}' not found");
            return EnemySpawnResult.Failure(UnitSpawnFailureReason.MissingPrefab);
        }

        var spawnResult = enemySpawner.TrySpawnEnemy(enemyDef, singleLaneHost);
        if (!spawnResult.IsSuccess)
        {
            Debug.LogWarning($"Wave enemy spawn failed for '{enemyId}': {spawnResult.FailureReason}");
            return spawnResult;
        }

        Debug.Log($"Wave spawned enemy '{enemyId}'");
        RegisterEnemyOnLane(spawnResult.SpawnedEnemy);
        return spawnResult;
    }

    public EnemySpawnResult TrySpawnScriptedEnemy(string enemyId)
    {
        return TrySpawnEnemyById(enemyId);
    }

    private void RegisterPlayerUnitOnLane(LaneUnit laneUnit)
    {
        if (laneEncounterCoordinator == null)
        {
            Debug.LogWarning("Lane prototype setup incomplete: laneEncounterCoordinator is missing");
            return;
        }

        laneEncounterCoordinator.RegisterPlayerUnit(laneUnit);
    }

    private void RegisterEnemyOnLane(LaneEnemy laneEnemy)
    {
        if (laneEncounterCoordinator == null)
        {
            Debug.LogWarning("Lane prototype setup incomplete: laneEncounterCoordinator is missing");
            return;
        }

        laneEncounterCoordinator.RegisterEnemy(laneEnemy);
    }

    private bool ValidateLanePrototypeSetup()
    {
        if (singleLaneHost != null && laneEncounterCoordinator != null)
        {
            laneSetupWarningShown = false;
            return true;
        }

        if (!laneSetupWarningShown)
        {
            Debug.LogWarning(
                "Lane prototype setup incomplete: assign SingleLaneHost and SingleLaneEncounterCoordinator on Main");
            laneSetupWarningShown = true;
        }

        return false;
    }

    private void StartNightWave()
    {
        activeNightDefinition = ResolveNightDefinitionForCurrentNight();
        waveSystem.StartWave(activeNightDefinition);

        if (activeNightDefinition == null)
        {
            Debug.LogWarning($"Night setup failed: definition for night {RunState?.CurrentNight} not found");
            return;
        }

        Debug.Log(
            $"Night wave started: '{activeNightDefinition.Id}' duration={activeNightDefinition.DurationSeconds:0.0}s");
    }

    private void UpdateWaveSpawning()
    {
        if (RunState == null || RunState.CurrentPhase != GamePhase.Night || !waveSystem.IsRunning)
        {
            return;
        }

        if (firstRunTutorialController != null && firstRunTutorialController.BlocksWaveProgress)
        {
            return;
        }

        readyWaveSpawnEnemyIds.Clear();
        waveSystem.CollectReadySpawns(Time.deltaTime, readyWaveSpawnEnemyIds);

        for (var i = 0; i < readyWaveSpawnEnemyIds.Count; i++)
        {
            TrySpawnEnemyById(readyWaveSpawnEnemyIds[i]);
        }
    }

    private void UpdateNightCompletion()
    {
        if (RunState == null || RunState.CurrentPhase != GamePhase.Night)
        {
            return;
        }

        if (firstRunTutorialController != null && firstRunTutorialController.BlocksNightCompletion)
        {
            return;
        }

        if (!waveSystem.AreAllSpawnsIssued)
        {
            return;
        }

        if (laneEncounterCoordinator != null && laneEncounterCoordinator.HasActiveEnemyPressure)
        {
            return;
        }

        CompleteNight();
    }

    private void CompleteNight()
    {
        TrackNightCompleted();
        RecordCompletedNightSummary();
        G.audioSystem.Play(SoundId.SFX_Win);
        Debug.Log("Night completed");
        activeNightDefinition = null;
        if (TryEnterWin())
        {
            return;
        }

        EnterDay();
    }

    private void UpdateKeeper()
    {
        if (RunState == null || RunState.Keeper == null)
        {
            return;
        }

        if (!TryResolveKeeperActor())
        {
            return;
        }

        if (RunState.CurrentPhase == GamePhase.Night)
        {
            keeperMovementSystem.UpdateMovement(
                RunState.Keeper,
                Time.deltaTime,
                BellgraveBalance.Run.KeeperArrivalDistance);
        }
        else
        {
            ClearKeeperInteractionAvailability();
        }

        if (RunState.CurrentPhase == GamePhase.Night)
        {
            UpdateKeeperInteractionAvailability();
        }

        keeperActor.SetWorldPosition(RunState.Keeper.Position);
    }

    private void RecordCompletedNightSummary()
    {
        if (RunState == null)
        {
            return;
        }

        RunState.LastDayReward = new DayRewardData
        {
            SourceNightIndex = RunState.CurrentNight
        };
    }

    private void ClearLaneCombatants()
    {
        if (laneEncounterCoordinator == null)
        {
            return;
        }

        laneEncounterCoordinator.ClearCombatants();
    }

    private void UpdateNightFaithCollection()
    {
        if (RunState == null || RunState.CurrentPhase != GamePhase.Night)
        {
            return;
        }

        var collectedFaith = faithCollectionSystem.UpdateCollection(RunState, Time.deltaTime);
        if (collectedFaith <= 0)
        {
            return;
        }

        FaithCollected?.Invoke(collectedFaith);
        PublishFaithPickupFeedback(collectedFaith);
    }

    private void UpdateNightCemeteryRepair()
    {
        if (RunState == null ||
            cemeteryStateSystem == null ||
            RunState.CurrentPhase != GamePhase.Night ||
            RunState.Keeper == null ||
            RunState.Keeper.ActivityState == KeeperActivityState.Moving ||
            RunState.Keeper.InteractionState != KeeperInteractionState.RepairPoint)
        {
            return;
        }

        var instantRepairedAmount = TryApplyInstantNightRepair();
        if (instantRepairedAmount > 0)
        {
            CemeteryRepaired?.Invoke(instantRepairedAmount);
            RefreshPresentation();
            if (RunState.CemeteryState >= RunState.CemeteryMaxState)
            {
                return;
            }
        }

        var repairedAmount = cemeteryStateSystem.ApplyNightRepair(RunState, Time.deltaTime);
        if (repairedAmount <= 0)
        {
            return;
        }

        CemeteryRepaired?.Invoke(repairedAmount);
        RefreshPresentation();
    }

    private int TryApplyInstantNightRepair()
    {
        if (RunState == null ||
            cemeteryStateSystem == null ||
            RunState.RemainingInstantNightRepairCharges <= 0 ||
            RunState.InstantNightRepairAmount <= 0 ||
            RunState.CemeteryState >= RunState.CemeteryMaxState)
        {
            return 0;
        }

        var repairedAmount = cemeteryStateSystem.ApplyInstantRepair(RunState, RunState.InstantNightRepairAmount);
        if (repairedAmount <= 0)
        {
            return 0;
        }

        RunState.RemainingInstantNightRepairCharges--;
        RunState.NightCemeteryRepairTimerProgress = 0f;
        return repairedAmount;
    }

    private void PublishFaithPickupFeedback(int collectedFaith)
    {
        if (collectedFaith <= 0 || G.HUD == null || RunState?.Keeper == null)
        {
            return;
        }

        var worldPosition = new Vector3(RunState.Keeper.Position.x, RunState.Keeper.Position.y, 0f);
        if (!string.IsNullOrWhiteSpace(RunState.Keeper.CurrentPoiId) &&
            TryResolveNightPoiById(RunState.Keeper.CurrentPoiId, out var poi) &&
            poi != null &&
            poi.Type == NightPoiType.FaithPoint)
        {
            var poiPosition = poi.GetWorldPosition();
            worldPosition = new Vector3(poiPosition.x, poiPosition.y, 0f);
        }

        G.HUD.PlayFaithPickupEffect(worldPosition, collectedFaith);
    }

    private bool TryGetFaithPoiProgress(string poiId, out float normalizedProgress)
    {
        normalizedProgress = 0f;

        var collectionIntervalSeconds = Mathf.Max(0f, RunState.FaithCollectionIntervalSeconds);
        if (RunState.FaithCollectionPayoutAmount <= 0 || collectionIntervalSeconds <= 0f)
        {
            return false;
        }

        normalizedProgress = Mathf.Clamp01(RunState.FaithCollectionTimerProgress / collectionIntervalSeconds);
        return IsKeeperReadyAtPoi(poiId, NightPoiType.FaithPoint) || normalizedProgress > 0f;
    }

    private bool TryGetRepairPoiProgress(string poiId, out float normalizedProgress)
    {
        normalizedProgress = 0f;

        var repairIntervalSeconds = Mathf.Max(0f, RunState.NightCemeteryRepairIntervalSeconds);
        if (RunState.NightCemeteryRepairAmount <= 0 ||
            repairIntervalSeconds <= 0f ||
            RunState.CemeteryState >= RunState.CemeteryMaxState)
        {
            return false;
        }

        normalizedProgress = Mathf.Clamp01(RunState.NightCemeteryRepairTimerProgress / repairIntervalSeconds);
        return IsKeeperReadyAtPoi(poiId, NightPoiType.RepairPoint) || normalizedProgress > 0f;
    }

    private bool IsKeeperReadyAtPoi(string poiId, NightPoiType poiType)
    {
        if (RunState?.Keeper == null || RunState.Keeper.ActivityState == KeeperActivityState.Moving)
        {
            return false;
        }

        return RunState.Keeper.CurrentPoiId == poiId &&
               RunState.Keeper.InteractionState == NightPoiSystem.ToKeeperInteractionState(poiType);
    }

    private bool TryResolveKeeperActor()
    {
        if (keeperActor == null)
        {
            keeperActor = FindAnyObjectByType<KeeperActor>();
        }

        if (keeperActor == null)
        {
            if (!keeperBindingWarningShown)
            {
                Debug.LogWarning("Keeper setup incomplete: add a KeeperActor scene instance");
                keeperBindingWarningShown = true;
            }

            return false;
        }

        keeperBindingWarningShown = false;

        if (!keeperSceneBindingInitialized && RunState != null && RunState.Keeper != null)
        {
            RunState.Keeper.Position = keeperActor.GetWorldPosition();
            RunState.Keeper.TargetPosition = RunState.Keeper.Position;
            keeperActor.SetWorldPosition(RunState.Keeper.Position);
            keeperSceneBindingInitialized = true;
        }

        return true;
    }

    private void RebuildNightPoiRegistry()
    {
        var scenePois = FindObjectsByType<NightPointOfInterest>();
        nightPoiSystem.RebuildFromScene(scenePois);
        ValidateNightPoiClickSetup(scenePois);
        RebuildBellWorldObjectRegistry();
    }

    private bool TryResolveNightPoiById(string poiId, out NightPointOfInterest poi)
    {
        poi = null;

        if (nightPoiSystem == null)
        {
            return false;
        }

        if (nightPoiSystem.RegisteredPoiCount <= 0)
        {
            RebuildNightPoiRegistry();
        }

        return nightPoiSystem.TryGetPoiById(poiId, out poi);
    }

    private bool TryResolveNightPoiByType(NightPoiType poiType, out NightPointOfInterest poi)
    {
        poi = null;

        if (nightPoiSystem == null)
        {
            return false;
        }

        if (nightPoiSystem.RegisteredPoiCount <= 0)
        {
            RebuildNightPoiRegistry();
        }

        return nightPoiSystem.TryGetPoiByType(poiType, out poi);
    }

    public bool TryGetNightPoiByType(NightPoiType poiType, out NightPointOfInterest poi)
    {
        return TryResolveNightPoiByType(poiType, out poi);
    }

    private void ValidateNightPoiClickSetup(IReadOnlyList<NightPointOfInterest> scenePois)
    {
        if (scenePois == null || scenePois.Count == 0)
        {
            return;
        }

        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("Night POI click setup incomplete: main camera not found");
            return;
        }

        if (mainCamera.GetComponent<Physics2DRaycaster>() == null)
        {
            Debug.LogWarning("Night POI click setup incomplete: main camera is missing Physics2DRaycaster");
        }
    }

    private void RebuildBellWorldObjectRegistry()
    {
        var bellWorldObjects = FindObjectsByType<BellWorldObject>();
        bellWorldObjectsById.Clear();

        if (bellWorldObjects == null || bellWorldObjects.Length == 0)
        {
            Debug.LogWarning("Bell world setup incomplete: no BellWorldObject found in scene");
            return;
        }

        for (var i = 0; i < bellWorldObjects.Length; i++)
        {
            var bellWorldObject = bellWorldObjects[i];
            if (bellWorldObject == null)
            {
                continue;
            }

            if (bellWorldObject.TryGetWorldInteractionValidationError(out var validationError))
            {
                Debug.LogWarning(
                    $"Bell world setup incomplete for '{bellWorldObject.name}': {validationError}");
                continue;
            }

            if (bellWorldObjectsById.ContainsKey(bellWorldObject.BellId))
            {
                Debug.LogWarning($"Bell world setup incomplete: duplicate bell id '{bellWorldObject.BellId}'");
                continue;
            }

            bellWorldObjectsById.Add(bellWorldObject.BellId, bellWorldObject);
        }
    }

    private bool TrySpawnNightUnlockedBells()
    {
        var bellNightSpawnController = FindAnyObjectByType<BellNightSpawnController>();
        if (bellNightSpawnController == null)
        {
            return false;
        }

        if (!bellNightSpawnController.TrySpawnForNight(RunState.CurrentNight))
        {
            return false;
        }

        RebuildBellWorldObjectRegistry();
        return true;
    }

    private void TryQueueBellUnlockAnnouncementForCurrentNight(bool didSpawnUnlockedBell)
    {
        if (!didSpawnUnlockedBell || RunState == null)
        {
            return;
        }

        var currentNight = RunState.CurrentNight;
        if (currentNight != 2 && currentNight != 4)
        {
            return;
        }

        if (RunState.ShownBellUnlockTutorialNights != null &&
            RunState.ShownBellUnlockTutorialNights.Contains(currentNight))
        {
            return;
        }

        var unlockedBellId = ResolveNewlyUnlockedBellIdForNight(currentNight);
        if (string.IsNullOrWhiteSpace(unlockedBellId))
        {
            return;
        }

        RunState.PendingBellUnlockAnnouncement = BuildBellUnlockAnnouncement(unlockedBellId, currentNight);
        RunState.ShownBellUnlockTutorialNights?.Add(currentNight);

        if (firstRunTutorialController != null &&
            firstRunTutorialController.ShouldRunFirstRunTutorial &&
            currentNight == 1)
        {
            return;
        }

        TryPlayPendingBellUnlockAnnouncement();
    }

    private string ResolveNewlyUnlockedBellIdForNight(int nightIndex)
    {
        var bellNightSpawnController = FindAnyObjectByType<BellNightSpawnController>();
        if (bellNightSpawnController == null)
        {
            return null;
        }

        if (nightIndex == 2 && bellNightSpawnController.TryGetUnlockNightIndex("bell_zombie", out var zombieNight) &&
            zombieNight == nightIndex)
        {
            return "bell_zombie";
        }

        if (nightIndex == 4 && bellNightSpawnController.TryGetUnlockNightIndex("bell_vampire", out var vampireNight) &&
            vampireNight == nightIndex)
        {
            return "bell_vampire";
        }

        return null;
    }

    private BellUnlockAnnouncementData BuildBellUnlockAnnouncement(string bellId, int nightIndex)
    {
        if (nightIndex == 2)
        {
            return new BellUnlockAnnouncementData
            {
                BellId = bellId,
                Title = "NEW BELL",
                Message = "New bell has awakened. Ring it to summon zombie.",
                MarkerColor = new Color(1f, 0.86f, 0.36f, 1f)
            };
        }

        if (nightIndex == 4)
        {
            return new BellUnlockAnnouncementData
            {
                BellId = bellId,
                Title = "NEW BELL",
                Message = "Another bell has awakened. Vamp has joined you",
                MarkerColor = new Color(1f, 0.86f, 0.36f, 1f),
                NextAnnouncement = new BellUnlockAnnouncementData
                {
                    BellId = bellId,
                    Title = "NEW BELL",
                    Message = "His life is short but he is very strong",
                    MarkerColor = new Color(1f, 0.86f, 0.36f, 1f)
                }
            };
        }

        return new BellUnlockAnnouncementData
        {
            BellId = bellId,
            Title = "NEW BELL",
            Message = "A new bell is now available.",
            MarkerColor = new Color(1f, 0.86f, 0.36f, 1f)
        };
    }

    private void TryPlayPendingBellUnlockAnnouncement()
    {
        if (RunState == null || bellUnlockAnnouncementController == null)
        {
            return;
        }

        var pendingAnnouncement = RunState.PendingBellUnlockAnnouncement;
        if (pendingAnnouncement == null)
        {
            return;
        }

        bellUnlockAnnouncementController.TryPlay(pendingAnnouncement);
        RunState.PendingBellUnlockAnnouncement = null;
    }

    private BellWorldObject ResolveBellWorldObject(string bellId)
    {
        if (string.IsNullOrWhiteSpace(bellId))
        {
            return null;
        }

        if (bellWorldObjectsById.Count <= 0)
        {
            RebuildBellWorldObjectRegistry();
        }

        bellWorldObjectsById.TryGetValue(bellId, out var bellWorldObject);
        return bellWorldObject;
    }

    public bool TryGetBellWorldObject(string bellId, out BellWorldObject bellWorldObject)
    {
        bellWorldObject = ResolveBellWorldObject(bellId);
        return bellWorldObject != null;
    }

    private void BindHud()
    {
        if (G.HUD == null)
        {
            return;
        }

        G.HUD.DayScreenStartNightRequested -= HandleDayScreenStartNightRequested;
        G.HUD.DayScreenStartNightRequested += HandleDayScreenStartNightRequested;
        G.HUD.DayScreenUpgradePurchaseRequested -= HandleDayScreenUpgradePurchaseRequested;
        G.HUD.DayScreenUpgradePurchaseRequested += HandleDayScreenUpgradePurchaseRequested;
        G.HUD.DefeatScreenRestartRequested -= HandleDefeatScreenRestartRequested;
        G.HUD.DefeatScreenRestartRequested += HandleDefeatScreenRestartRequested;
        G.HUD.WinScreenRestartRequested -= HandleWinScreenRestartRequested;
        G.HUD.WinScreenRestartRequested += HandleWinScreenRestartRequested;
    }

    private void BeginStartupSequence()
    {
        startupSequenceInProgress = true;
        UI.EnsureInstance();
        var shouldRunFirstRunTutorial = showFirstRunTutorial &&
                                        firstRunTutorialController != null &&
                                        firstRunTutorialController.ShouldRunFirstRunTutorial;
        var canShowTitleScreen = showIntroScreenOnStartup && G.ui != null && G.ui.TitleScreenImage != null;

        if (!canShowTitleScreen && !shouldRunFirstRunTutorial)
        {
            G.ui?.ToggleTitle(false);
            CompleteStartupSequence();
            return;
        }

        StartCoroutine(PlayStartupSequence(canShowTitleScreen, shouldRunFirstRunTutorial));
        TryPlayPendingBellUnlockAnnouncement();
    }

    private IEnumerator PlayStartupSequence(bool useTitleScreen, bool runFirstRunTutorial)
    {
        if (useTitleScreen)
        {
            G.ui.ToggleTitle(true);

            while (!IsTitleScreenStartRequested())
            {
                yield return null;
            }

            G.ui.StopTitlePromptPulse();
            var titleScreenImage = G.ui.TitleScreenImage;
            if (titleScreenImage != null && G.ScreenFader != null)
            {
                var fadeCompleted = false;
                G.ScreenFader.FadeOutCustom(
                    titleScreenImage,
                    Mathf.Max(0f, titleScreenFadeDuration),
                    () => { fadeCompleted = true; });

                while (!fadeCompleted)
                {
                    yield return null;
                }
            }
            else
            {
                G.ui.ToggleTitle(false);
            }
        }
        else
        {
            G.ui?.ToggleTitle(false);
        }

        if (!runFirstRunTutorial || firstRunTutorialController == null)
        {
            CompleteStartupSequence();
            yield break;
        }

        yield return firstRunTutorialController.PlayStartupIntroSequence();
        G.ui?.ToggleTitle(false);
        EnterNight();
        ValidateLanePrototypeSetup();
        yield return firstRunTutorialController.FadeIntoGameplaySequence();
        startupSequenceInProgress = false;
        StartCoroutine(firstRunTutorialController.RunNightTutorialSequence());
    }

    private void CompleteStartupSequence()
    {
        G.ui?.ToggleTitle(false);
        EnterNight();
        ValidateLanePrototypeSetup();
        startupSequenceInProgress = false;
    }

    private static bool IsTitleScreenStartRequested()
    {
        return Input.GetMouseButtonDown(0) ||
               (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
    }

    private void UnbindHud()
    {
        if (G.HUD == null)
        {
            return;
        }

        G.HUD.DayScreenStartNightRequested -= HandleDayScreenStartNightRequested;
        G.HUD.DayScreenUpgradePurchaseRequested -= HandleDayScreenUpgradePurchaseRequested;
        G.HUD.DefeatScreenRestartRequested -= HandleDefeatScreenRestartRequested;
        G.HUD.WinScreenRestartRequested -= HandleWinScreenRestartRequested;
    }

    private void HandleDayScreenStartNightRequested()
    {
        TryStartNightFromDayScreen();
    }

    private void HandleDayScreenUpgradePurchaseRequested(string upgradeId)
    {
        TryPurchaseUpgrade(upgradeId);
    }

    private void HandleDefeatScreenRestartRequested()
    {
        TryRestartFromDefeatScreen();
    }

    private void HandleWinScreenRestartRequested()
    {
        TryRestartFromWinScreen();
    }

    private void RefreshPresentation()
    {
        if (G.HUD == null || RunState == null)
        {
            return;
        }

        if (RunState.CurrentPhase == GamePhase.Defeat)
        {
            G.HUD.HideDayScreen();
            G.HUD.HideWinScreen();
            G.HUD.HidePhaseTransition();
            G.HUD.ShowDefeatScreen(RunState);
            return;
        }

        G.HUD.HideDefeatScreen();

        if (RunState.CurrentPhase == GamePhase.Win)
        {
            G.HUD.HideDayScreen();
            G.HUD.HidePhaseTransition();
            G.HUD.ShowWinScreen(RunState);
            return;
        }

        G.HUD.HideWinScreen();

        if (RunState.CurrentPhase == GamePhase.Day)
        {
            G.HUD.ShowDayScreen(RunState, BuildDayUpgradeDisplayItems());
        }
        else
        {
            G.HUD.HideDayScreen();
        }
    }

    private void UpdateKeeperInteractionAvailability()
    {
        if (RunState?.Keeper == null)
        {
            return;
        }

        if (RunState.Keeper.ActivityState == KeeperActivityState.Moving)
        {
            ClearKeeperInteractionAvailability();
            UpdateFaithPoiAnimationState();
            return;
        }

        if (nightPoiSystem == null)
        {
            ClearKeeperInteractionAvailability();
            UpdateFaithPoiAnimationState();
            return;
        }

        if (nightPoiSystem.RegisteredPoiCount <= 0)
        {
            RebuildNightPoiRegistry();
        }

        if (!nightPoiSystem.TryResolveKeeperInteraction(
                RunState.Keeper.Position,
                out var poi,
                out var interactionState))
        {
            ClearKeeperInteractionAvailability();
            UpdateFaithPoiAnimationState();
            return;
        }

        RunState.Keeper.CurrentPoiId = poi.Id;
        RunState.Keeper.InteractionState = interactionState;
        keeperActor?.SetFacingDirection(poi.KeeperFacingDirection);
        UpdateFaithPoiAnimationState();
    }

    private void StopKeeperMovement()
    {
        if (RunState?.Keeper == null)
        {
            return;
        }

        ClearPendingBellInteraction();
        keeperMovementSystem.Stop(RunState.Keeper);
        ClearKeeperInteractionAvailability();
    }

    private List<NightDefinition> BuildNightDefinitions()
    {
        return new List<NightDefinition>
        {
            new Night1(),
            new Night2(),
            new Night3(),
            new Night4(),
            new Night5(),
            new Night6()
        };
    }

    private NightDefinition ResolveNightDefinitionForCurrentNight()
    {
        if (nightDefinitions == null || nightDefinitions.Count == 0)
        {
            return null;
        }

        var requestedNightIndex = Mathf.Max(0, RunState.CurrentNight - 1);
        var resolvedNightIndex = Mathf.Min(requestedNightIndex, nightDefinitions.Count - 1);
        return nightDefinitions[resolvedNightIndex];
    }

    private void PrepareKeeperForNight()
    {
        if (RunState?.Keeper == null)
        {
            return;
        }

        RunState.Keeper.InteractionState = KeeperInteractionState.None;
        if (RunState.Keeper.ActivityState != KeeperActivityState.Moving)
        {
            RunState.Keeper.TargetPosition = RunState.Keeper.Position;
            RunState.Keeper.CurrentPoiId = string.Empty;
            RunState.Keeper.CurrentTargetPointId = string.Empty;
            RunState.Keeper.ActivityState = KeeperActivityState.Idle;
        }
    }

    private void ClearKeeperInteractionAvailability()
    {
        if (RunState?.Keeper == null)
        {
            return;
        }

        RunState.Keeper.CurrentPoiId = string.Empty;
        RunState.Keeper.InteractionState = KeeperInteractionState.None;
    }

    private List<DayUpgradeItemData> BuildDayUpgradeDisplayItems()
    {
        var displayItems = new List<DayUpgradeItemData>();
        var offerIds = RunState.CurrentDayUpgradeOfferIds;

        if (offerIds == null || offerIds.Count == 0)
        {
            return displayItems;
        }

        for (var i = 0; i < offerIds.Count; i++)
        {
            var upgradeId = offerIds[i];

            if (string.IsNullOrWhiteSpace(upgradeId))
            {
                displayItems.Add(new DayUpgradeItemData());
                continue;
            }

            var upgradeDef = CMS.Get<UpgradeDef>(upgradeId);
            if (upgradeDef == null)
            {
                displayItems.Add(new DayUpgradeItemData());
                continue;
            }

            if (!upgradeDef.IsRepeatable &&
                RunState.PurchasedUpgradeIds != null &&
                RunState.PurchasedUpgradeIds.Contains(upgradeDef.Id))
            {
                displayItems.Add(new DayUpgradeItemData());
                continue;
            }

            if (!UpgradeSystem.SupportsEffectType(upgradeDef.EffectType))
            {
                displayItems.Add(new DayUpgradeItemData());
                continue;
            }

            var price = Mathf.Max(0, upgradeDef.Price);

            displayItems.Add(new DayUpgradeItemData
            {
                UpgradeId = upgradeDef.Id,
                NameText = string.IsNullOrWhiteSpace(upgradeDef.DisplayName) ? upgradeDef.Id : upgradeDef.DisplayName,
                PriceText = $"{price} Gold",
                EffectText = BuildUpgradeEffectText(upgradeDef),
                CanBuy = RunState.CurrentPhase == GamePhase.Day && RunState.Gold >= price
            });
        }

        return displayItems;
    }

    private void GenerateDayUpgradeOffers()
    {
        if (RunState == null)
        {
            return;
        }

        RunState.CurrentDayUpgradeOfferIds ??= new List<string>();
        RunState.CurrentDayUpgradeOfferIds.Clear();

        var eligibleUpgradeDefs = GetEligibleDayUpgradeDefs();
        if (eligibleUpgradeDefs.Count == 0)
        {
            return;
        }

        var affordableUpgradeDefs = GetAffordableEligibleDayUpgradeDefs(eligibleUpgradeDefs);

        TryAddDailyOffer(affordableUpgradeDefs, RunState.CurrentDayUpgradeOfferIds, DayUpgradeOfferCategory.Economy);
        TryAddDailyOffer(affordableUpgradeDefs, RunState.CurrentDayUpgradeOfferIds, DayUpgradeOfferCategory.Combat);
        TryAddDailyOffer(affordableUpgradeDefs, RunState.CurrentDayUpgradeOfferIds, DayUpgradeOfferCategory.Utility);

        TryAddDailyOffer(eligibleUpgradeDefs, RunState.CurrentDayUpgradeOfferIds, DayUpgradeOfferCategory.Economy);
        TryAddDailyOffer(eligibleUpgradeDefs, RunState.CurrentDayUpgradeOfferIds, DayUpgradeOfferCategory.Combat);
        TryAddDailyOffer(eligibleUpgradeDefs, RunState.CurrentDayUpgradeOfferIds, DayUpgradeOfferCategory.Utility);

        if (RunState.CurrentDayUpgradeOfferIds.Count < DailyUpgradeOfferCount)
        {
            FillDailyOffersFromPool(
                affordableUpgradeDefs,
                RunState.CurrentDayUpgradeOfferIds,
                (RunState.CurrentDay - 1) * 13 + 1);
        }

        if (RunState.CurrentDayUpgradeOfferIds.Count < DailyUpgradeOfferCount)
        {
            FillDailyOffersFromPool(
                eligibleUpgradeDefs,
                RunState.CurrentDayUpgradeOfferIds,
                (RunState.CurrentDay - 1) * 17 + 3);
        }
    }

    private void FillDailyOffersFromPool(
        IReadOnlyList<UpgradeDef> sourceUpgradeDefs,
        ICollection<string> selectedOfferIds,
        int salt)
    {
        if (sourceUpgradeDefs == null || sourceUpgradeDefs.Count == 0 || selectedOfferIds == null || RunState == null)
        {
            return;
        }

        var startIndex = GetDeterministicOfferIndex(
            sourceUpgradeDefs.Count,
            (RunState.CurrentDay - 1) * 11 + salt);

        for (var i = 0;
             i < sourceUpgradeDefs.Count && selectedOfferIds.Count < DailyUpgradeOfferCount;
             i++)
        {
            var candidateIndex = (startIndex + i) % sourceUpgradeDefs.Count;
            var candidate = sourceUpgradeDefs[candidateIndex];
            if (candidate == null || selectedOfferIds.Contains(candidate.Id))
            {
                continue;
            }

            selectedOfferIds.Add(candidate.Id);
        }
    }

    private List<UpgradeDef> GetAffordableEligibleDayUpgradeDefs(IReadOnlyList<UpgradeDef> eligibleUpgradeDefs)
    {
        var affordableUpgradeDefs = new List<UpgradeDef>();

        if (eligibleUpgradeDefs == null || RunState == null)
        {
            return affordableUpgradeDefs;
        }

        for (var i = 0; i < eligibleUpgradeDefs.Count; i++)
        {
            var upgradeDef = eligibleUpgradeDefs[i];
            if (upgradeDef == null)
            {
                continue;
            }

            var price = Mathf.Max(0, upgradeDef.Price);
            if (price <= RunState.Gold)
            {
                affordableUpgradeDefs.Add(upgradeDef);
            }
        }

        return affordableUpgradeDefs;
    }

    private void EnsureAtLeastOneAffordableOffer(
        IReadOnlyList<UpgradeDef> affordableUpgradeDefs,
        IList<string> selectedOfferIds)
    {
        if (RunState == null || selectedOfferIds == null || selectedOfferIds.Count == 0)
        {
            return;
        }

        var hasAffordableOffer = false;
        for (var i = 0; i < selectedOfferIds.Count; i++)
        {
            var offerId = selectedOfferIds[i];
            if (string.IsNullOrWhiteSpace(offerId))
            {
                continue;
            }

            var offerDef = CMS.Get<UpgradeDef>(offerId);
            if (offerDef == null)
            {
                continue;
            }

            if (Mathf.Max(0, offerDef.Price) <= RunState.Gold)
            {
                hasAffordableOffer = true;
                break;
            }
        }

        if (hasAffordableOffer || affordableUpgradeDefs == null || affordableUpgradeDefs.Count == 0)
        {
            return;
        }

        var replacementIndex = GetDeterministicOfferIndex(
            affordableUpgradeDefs.Count,
            (RunState.CurrentDay - 1) * 19 + (RunState.PurchasedUpgradeIds?.Count ?? 0));

        var replacement = affordableUpgradeDefs[replacementIndex];
        if (replacement == null)
        {
            return;
        }

        // Ставим доступный оффер в первый слот.
        if (selectedOfferIds.Count > 0)
        {
            selectedOfferIds[0] = replacement.Id;
        }
        else
        {
            selectedOfferIds.Add(replacement.Id);
        }
    }

    private List<UpgradeDef> GetEligibleDayUpgradeDefs()
    {
        var upgradeDefs = new List<UpgradeDef>(CMS.GetAll<UpgradeDef>());
        upgradeDefs.Sort(CompareUpgradeDefsForDisplay);

        var eligibleUpgradeDefs = new List<UpgradeDef>();
        for (var i = 0; i < upgradeDefs.Count; i++)
        {
            var upgradeDef = upgradeDefs[i];
            if (upgradeDef == null)
            {
                continue;
            }

            if (!upgradeDef.IsRepeatable &&
                RunState.PurchasedUpgradeIds != null &&
                RunState.PurchasedUpgradeIds.Contains(upgradeDef.Id))
            {
                continue;
            }

            if (!UpgradeSystem.SupportsEffectType(upgradeDef.EffectType))
            {
                continue;
            }

            if (!IsUpgradeUnlockedForCurrentRun(upgradeDef))
            {
                continue;
            }

            eligibleUpgradeDefs.Add(upgradeDef);
        }

        return eligibleUpgradeDefs;
    }

    private void TryAddDailyOffer(
        IReadOnlyList<UpgradeDef> eligibleUpgradeDefs,
        ICollection<string> selectedOfferIds,
        DayUpgradeOfferCategory category)
    {
        var categoryCandidates = new List<UpgradeDef>();
        for (var i = 0; i < eligibleUpgradeDefs.Count; i++)
        {
            var upgradeDef = eligibleUpgradeDefs[i];
            if (upgradeDef == null ||
                selectedOfferIds.Contains(upgradeDef.Id) ||
                ResolveDayUpgradeOfferCategory(upgradeDef) != category)
            {
                continue;
            }

            categoryCandidates.Add(upgradeDef);
        }

        if (categoryCandidates.Count == 0)
        {
            return;
        }

        var categorySalt = category switch
        {
            DayUpgradeOfferCategory.Economy => 3,
            DayUpgradeOfferCategory.Combat => 7,
            DayUpgradeOfferCategory.Utility => 11,
            _ => 0
        };
        var selectedIndex = GetDeterministicOfferIndex(
            categoryCandidates.Count,
            (RunState.CurrentDay - 1) * 5 + (RunState.PurchasedUpgradeIds?.Count ?? 0) + categorySalt);
        selectedOfferIds.Add(categoryCandidates[selectedIndex].Id);
    }

    private bool IsUpgradeOfferedToday(string upgradeId)
    {
        return RunState != null
               && !string.IsNullOrWhiteSpace(upgradeId)
               && RunState.CurrentDayUpgradeOfferIds != null
               && RunState.CurrentDayUpgradeOfferIds.Contains(upgradeId);
    }

    private bool IsUpgradeUnlockedForCurrentRun(UpgradeDef upgradeDef)
    {
        if (upgradeDef == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(upgradeDef.TargetUnitId))
        {
            return true;
        }

        return upgradeDef.TargetUnitId switch
        {
            "zombie" => IsBellUnlockedForCurrentRun("bell_zombie"),
            "vampire" => IsBellUnlockedForCurrentRun("bell_vampire"),
            _ => true
        };
    }

    private bool IsBellUnlockedForCurrentRun(string bellId)
    {
        if (string.IsNullOrWhiteSpace(bellId))
        {
            return false;
        }

        if (bellId == "bell_small")
        {
            return true;
        }

        var bellWorldObjects = FindObjectsByType<BellWorldObject>();
        for (var i = 0; i < bellWorldObjects.Length; i++)
        {
            var bellWorldObject = bellWorldObjects[i];
            if (bellWorldObject != null && bellWorldObject.BellId == bellId)
            {
                return true;
            }
        }

        var bellNightSpawnController = FindAnyObjectByType<BellNightSpawnController>();
        return bellNightSpawnController != null
               && bellNightSpawnController.TryGetUnlockNightIndex(bellId, out var unlockNightIndex)
               && RunState != null
               && RunState.CurrentNight >= unlockNightIndex;
    }

    private static DayUpgradeOfferCategory ResolveDayUpgradeOfferCategory(UpgradeDef upgradeDef)
    {
        return upgradeDef.EffectType switch
        {
            UpgradeEffectType.FaithIncomeBonus => DayUpgradeOfferCategory.Economy,
            UpgradeEffectType.BellFaithCostModifier => DayUpgradeOfferCategory.Economy,
            UpgradeEffectType.StartingNightFaithBonus => DayUpgradeOfferCategory.Economy,
            UpgradeEffectType.FaithCollectionIntervalModifier => DayUpgradeOfferCategory.Economy,

            UpgradeEffectType.UnitDamageModifier => DayUpgradeOfferCategory.Combat,
            UpgradeEffectType.UnitLifetimeModifier => DayUpgradeOfferCategory.Combat,
            UpgradeEffectType.UnitHpModifier => DayUpgradeOfferCategory.Combat,

            UpgradeEffectType.CemeteryRepair => DayUpgradeOfferCategory.Utility,
            UpgradeEffectType.CemeteryMaxStateBonus => DayUpgradeOfferCategory.Utility,
            UpgradeEffectType.KeeperMoveSpeedBonus => DayUpgradeOfferCategory.Utility,
            UpgradeEffectType.NightInstantRepairCharge => DayUpgradeOfferCategory.Utility,
            _ => DayUpgradeOfferCategory.Utility
        };
    }

    private static int GetDeterministicOfferIndex(int candidateCount, int seed)
    {
        if (candidateCount <= 0)
        {
            return 0;
        }

        return Mathf.Abs(seed) % candidateCount;
    }

    private static int CompareUpgradeDefsForDisplay(UpgradeDef left, UpgradeDef right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        var priceCompare = Mathf.Max(0, left.Price).CompareTo(Mathf.Max(0, right.Price));
        if (priceCompare != 0)
        {
            return priceCompare;
        }

        return string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal);
    }

    private static string BuildUpgradeEffectText(UpgradeDef upgradeDef)
    {
        var effectValue = upgradeDef.EffectValue;
        var effectValueText = FormatUpgradeValue(Mathf.Abs(effectValue));
        var targetUnitName = ResolveUpgradeUnitName(upgradeDef.TargetUnitId);

        return upgradeDef.EffectType switch
        {
            UpgradeEffectType.FaithIncomeBonus => $"+{effectValueText} Faith per payout",
            UpgradeEffectType.CemeteryRepair => $"+{effectValueText} cemetery repair",
            UpgradeEffectType.CemeteryMaxStateBonus => $"+{effectValueText} cemetery max HP",
            UpgradeEffectType.BellFaithCostModifier => $"-{effectValueText} bells Faith cost",
            UpgradeEffectType.StartingNightFaithBonus => $"+{effectValueText} Faith at night start",
            UpgradeEffectType.KeeperMoveSpeedBonus => $"+{effectValueText} keeper speed",
            UpgradeEffectType.UnitDamageModifier => $"+{effectValueText} {targetUnitName} damage",
            UpgradeEffectType.UnitLifetimeModifier => $"+{effectValueText}s {targetUnitName} lifetime",
            UpgradeEffectType.UnitHpModifier => $"+{effectValueText} {targetUnitName} HP",
            UpgradeEffectType.FaithCollectionIntervalModifier => $"-{effectValueText}s Faith payout interval",
            UpgradeEffectType.NightInstantRepairCharge =>
                $"Instant repair once per night (+{effectValueText} cemetery)",
            _ => string.Empty
        };
    }

    private static string FormatUpgradeValue(float value)
    {
        if (Mathf.Approximately(value, Mathf.Round(value)))
        {
            return Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture);
        }

        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string ResolveUpgradeUnitName(string unitId)
    {
        return unitId switch
        {
            "skel1" => "Skeleton",
            "vampire" => "Vampire",
            "zombie" => "Zombie",
            _ when string.IsNullOrWhiteSpace(unitId) => "Unit",
            _ => unitId
        };
    }

    private void UpdateLoseCondition()
    {
        if (RunState == null ||
            RunState.CurrentPhase == GamePhase.Defeat ||
            RunState.CurrentPhase == GamePhase.Win ||
            RunState.CemeteryState > 0)
        {
            return;
        }

        TryEnterDefeat();
    }

    private bool TryEnterDefeat()
    {
        if (RunState == null ||
            RunState.CurrentPhase == GamePhase.Defeat ||
            RunState.CurrentPhase == GamePhase.Win ||
            RunState.CemeteryState > 0)
        {
            return false;
        }

        TrackNightFailed();
        RunState.CurrentPhase = GamePhase.Defeat;
        G.audioSystem.Play(SoundId.SFX_Lose);
        waveSystem.StopWave();
        Time.timeScale = 0f;
        AnalyticsSystem.OnGameEnded("defeat", GetCompletedNightCount());
        Debug.Log("Defeat: cemetery destroyed");
        RefreshPresentation();
        return true;
    }

    private void UpdateFaithPoiAnimationState()
    {
        if (!TryResolveNightPoiByType(NightPoiType.FaithPoint, out var faithPoi) || faithPoi == null)
        {
            return;
        }

        var isKeeperNearFaithPoi =
            RunState?.Keeper != null &&
            RunState.Keeper.ActivityState != KeeperActivityState.Moving &&
            RunState.Keeper.CurrentPoiId == faithPoi.Id &&
            RunState.Keeper.InteractionState == KeeperInteractionState.FaithPoint;

        faithPoi.SetKeeperNearbyPresentation(isKeeperNearFaithPoi);
    }


    private void PlayPhaseTransitionCue(GamePhase phase)
    {
        if (G.HUD == null)
        {
            return;
        }

        G.HUD.ShowPhaseTransition(phase);
    }

    private bool TryEnterWin()
    {
        if (RunState == null ||
            RunState.CurrentPhase == GamePhase.Defeat ||
            RunState.CurrentPhase == GamePhase.Win)
        {
            return false;
        }

        var requiredDayCount = Mathf.Max(1, BellgraveBalance.Run.TargetSurvivedDays);
        var reachedDayCount = RunState.CurrentPhase == GamePhase.Night
            ? RunState.CurrentDay + 1
            : RunState.CurrentDay;
        if (reachedDayCount < requiredDayCount)
        {
            return false;
        }

        RunState.CurrentPhase = GamePhase.Win;
        waveSystem.StopWave();
        Time.timeScale = 0f;
        AnalyticsSystem.OnGameEnded("win", reachedDayCount);
        Debug.Log($"Victory: survived day {reachedDayCount} of {requiredDayCount}");
        RefreshPresentation();
        return true;
    }

    public bool TryRestartFromDefeatScreen()
    {
        if (RunState == null || RunState.CurrentPhase != GamePhase.Defeat)
        {
            return false;
        }

        RestartCurrentScene();
        return true;
    }

    public bool TryRestartFromWinScreen()
    {
        if (RunState == null || RunState.CurrentPhase != GamePhase.Win)
        {
            return false;
        }

        RestartCurrentScene();
        return true;
    }

    private void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        var activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    private void TrackNightStarted()
    {
        if (RunState == null || RunState.CurrentNight <= 0)
        {
            return;
        }

        AnalyticsSystem.OnLevelChanged(RunState.CurrentNight, GetCurrentNightAnalyticsName());
    }

    private void TrackNightCompleted()
    {
        if (RunState == null || RunState.CurrentNight <= 0)
        {
            return;
        }

        AnalyticsSystem.OnLevelCompleted(RunState.CurrentNight, GetCurrentNightAnalyticsName());
    }

    private void TrackNightFailed()
    {
        if (RunState == null || RunState.CurrentPhase != GamePhase.Night || RunState.CurrentNight <= 0)
        {
            return;
        }

        AnalyticsSystem.OnLevelFailed(RunState.CurrentNight, GetCurrentNightAnalyticsName());
    }

    private string GetCurrentNightAnalyticsName()
    {
        if (!string.IsNullOrWhiteSpace(activeNightDefinition?.Id))
        {
            return activeNightDefinition.Id;
        }

        return RunState != null && RunState.CurrentNight > 0
            ? $"night_{RunState.CurrentNight}"
            : "unknown";
    }

    private int GetCompletedNightCount()
    {
        return RunState != null ? Mathf.Max(0, RunState.CurrentDay) : 0;
    }

    private void HandleDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.N) &&
            RunState != null &&
            RunState.CurrentPhase == GamePhase.Night)
        {
            Debug.Log("Debug: skipped current night");
            CompleteNight();
            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene("Main");
        }
    }
}
