using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class Main : MonoBehaviour
{
    private const int DebugFaithAmount = 10;
    private const int DebugGoldAmount = 10;
    private const int DebugCemeteryDamageAmount = 10;
    private const string DefaultDebugUpgradeId = "upgrade_morning_prayers";

    [SerializeField] private SingleLaneHost singleLaneHost;
    [SerializeField] private SingleLaneEncounterCoordinator laneEncounterCoordinator;
    [SerializeField] private KeeperActor keeperActor;
    [SerializeField] private string debugBellId;
    [SerializeField] private string debugUpgradeId;
    [SerializeField] private EnemyDef debugEnemyDef;
    [Min(0)] [SerializeField] private int initialCemeteryState = 100;
    [Min(0f)] [SerializeField] private float initialKeeperMoveSpeed = 3f;
    [Min(0f)] [SerializeField] private float keeperArrivalDistance = 0.05f;
    [FormerlySerializedAs("dayFaithReward")]
    [Min(0)] [SerializeField] private int startingNightFaith = 10;
    [FormerlySerializedAs("initialFaithCollectionPerSecond")]
    [Min(0f)] [SerializeField] private float initialFaithCollectionPayoutAmount = 3f;
    [Min(0.01f)] [SerializeField] private float initialFaithCollectionIntervalSeconds = 3f;
    [Min(0)] [SerializeField] private int initialNightCemeteryRepairAmount = 1;
    [Min(0.01f)] [SerializeField] private float initialNightCemeteryRepairIntervalSeconds = 1f;
    [Min(0)] [SerializeField] private int completedNightGoldReward = 10;

    [FormerlySerializedAs("targetSurvivedNights")]
    [Min(1)] [SerializeField] private int targetSurvivedDays = 5;

    private BellSystem bellSystem;
    private CemeteryStateSystem cemeteryStateSystem;
    private DayRewardSystem dayRewardSystem;
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
    private string pendingBellInteractionId = string.Empty;
    private readonly List<string> readyWaveSpawnEnemyIds = new();

    public RunState RunState { get; private set; }
    public bool IsNightWaveActive => RunState != null && RunState.CurrentPhase == GamePhase.Night && waveSystem != null && waveSystem.IsRunning;
    public float CurrentNightElapsedSeconds => waveSystem != null ? waveSystem.ElapsedTime : 0f;
    public float CurrentNightDurationSeconds => waveSystem != null ? waveSystem.DurationSeconds : 0f;
    public bool HasUpcomingNightWave => waveSystem != null && waveSystem.HasUpcomingWave;
    public float NextNightWaveTriggerTime => waveSystem != null ? waveSystem.NextWaveTriggerTime : 0f;
    public float PreviousNightWaveTriggerTime => waveSystem != null ? waveSystem.PreviousWaveTriggerTime : 0f;

    private void Awake()
    {
        Time.timeScale = 1f;
        G.main = this;
        RunState = RunState.CreateInitial(
            initialCemeteryState,
            initialKeeperMoveSpeed,
            startingNightFaith,
            Mathf.Max(0, Mathf.RoundToInt(initialFaithCollectionPayoutAmount)),
            initialFaithCollectionIntervalSeconds,
            initialNightCemeteryRepairAmount,
            initialNightCemeteryRepairIntervalSeconds);
        bellSystem = new BellSystem();
        cemeteryStateSystem = new CemeteryStateSystem();
        dayRewardSystem = new DayRewardSystem();
        faithCollectionSystem = new FaithCollectionSystem();
        upgradeSystem = new UpgradeSystem();
        unitSpawner = new SingleLaneUnitSpawner();
        enemySpawner = new SingleLaneEnemySpawner();
        waveSystem = new WaveSystem();
        keeperMovementSystem = new KeeperMovementSystem();
        nightPoiSystem = new NightPoiSystem();
        nightDefinitions = BuildNightDefinitions();

        if (laneEncounterCoordinator != null)
        {
            laneEncounterCoordinator.OnEnemyBreakthrough = HandleEnemyBreakthrough;
            laneEncounterCoordinator.OnEnemyCemeteryAttack = HandleEnemyCemeteryAttack;
        }
    }

    private void Start()
    {
        TryResolveKeeperActor();
        RebuildNightPoiRegistry();
        G.audioSystem.Play(SoundId.Ambient_Forest);
        G.audioSystem.Play(SoundId.Music_Main);
        BindHud();
        EnterDay();
        ValidateLanePrototypeSetup();
    }

    private void OnDestroy()
    {
        UnbindHud();
    }

    private void Update()
    {
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
        waveSystem.StopWave();
        StopKeeperMovement();
        RefreshPresentation();
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

        RunState.CurrentNight++;
        RunState.CurrentPhase = GamePhase.Night;
        ClearPendingBellInteraction();
        faithCollectionSystem.StartNight(RunState);
        cemeteryStateSystem.ResetNightRepairProgress(RunState);
        PrepareKeeperForNight();
        StartNightWave();
        RefreshPresentation();
        PlayPhaseTransitionCue(GamePhase.Night);
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

        var purchaseResult = upgradeSystem.TryPurchaseUpgrade(upgradeId, RunState);
        if (!purchaseResult.IsSuccess)
        {
            Debug.LogWarning($"Upgrade purchase failed for '{upgradeId}': {purchaseResult.FailureReason}");
            return purchaseResult;
        }

        Debug.Log($"Purchased upgrade '{purchaseResult.UpgradeDef.Id}'");
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

        var didChangeState = cemeteryStateSystem.ApplyBreakthroughDamage(RunState, amount);
        if (didChangeState)
        {
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

        return TryMoveKeeperTo(poi.GetWorldPosition(), poi.Id);
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
            PublishBellFeedback(missingBellResult);
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
            PublishBellFeedback(cooldownResult);
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
        }

        PublishBellFeedback(bellResult);
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

    public BellRingResult TryRingBell(string bellId)
    {
        ValidateLanePrototypeSetup();

        var bellResult = bellSystem.TryRingBell(bellId, RunState);
        if (!bellResult.IsSuccess)
        {
            Debug.LogWarning($"Bell ring failed for '{bellId}': {bellResult.FailureReason}");
            return bellResult;
        }

        bellResult.SpawnResult = unitSpawner.TrySpawnPlayerUnit(bellResult.UnitDef, singleLaneHost);
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

    private void PublishBellFeedback(BellRingResult bellResult)
    {
        if (G.HUD == null || bellResult == null)
        {
            return;
        }

        if (bellResult.IsSuccess && bellResult.SpawnResult != null && bellResult.SpawnResult.IsSuccess)
        {
            G.HUD.ShowBellFeedback($"Bell: {bellResult.BellDef.DisplayName}");
            return;
        }

        if (!bellResult.IsSuccess)
        {
            if (bellResult.FailureReason == BellRingFailureReason.OnCooldown)
            {
                G.HUD.ShowBellFeedback(
                    $"Bell cooldown: {bellResult.CooldownRemainingSeconds:0.0}s");
                return;
            }

            G.HUD.ShowBellFeedback($"Bell failed: {bellResult.FailureReason}");
            return;
        }

        if (bellResult.SpawnResult != null && !bellResult.SpawnResult.IsSuccess)
        {
            G.HUD.ShowBellFeedback($"Spawn failed: {bellResult.SpawnResult.FailureReason}");
        }
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

    public EnemySpawnResult TrySpawnDebugEnemy()
    {
        ValidateLanePrototypeSetup();

        var spawnResult = enemySpawner.TrySpawnEnemy(debugEnemyDef, singleLaneHost);
        if (!spawnResult.IsSuccess)
        {
            Debug.LogWarning($"Enemy spawn failed: {spawnResult.FailureReason}");
        }
        else
        {
            Debug.Log($"Enemy spawned '{debugEnemyDef.Id}'");
            RegisterEnemyOnLane(spawnResult.SpawnedEnemy);
        }

        return spawnResult;
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
        ApplyCompletedNightReward();
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
            keeperMovementSystem.UpdateMovement(RunState.Keeper, Time.deltaTime, keeperArrivalDistance);
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

    private void ApplyCompletedNightReward()
    {
        ApplyDayReward(
            dayRewardSystem.CreateCompletedNightReward(
                RunState.CurrentNight,
                completedNightGoldReward),
            $"Completed night {RunState.CurrentNight} reward");
    }

    private void ApplyDayReward(DayRewardData reward, string rewardSource)
    {
        if (reward == null)
        {
            return;
        }

        dayRewardSystem.ApplyReward(RunState, reward);

        if (!RunState.LastDayReward.HasAnyReward)
        {
            return;
        }

        Debug.Log($"{rewardSource}: {BuildRewardSummary(RunState.LastDayReward)}");

        RefreshPresentation();
    }

    private void UpdateNightFaithCollection()
    {
        if (RunState == null || RunState.CurrentPhase != GamePhase.Night)
        {
            return;
        }

        faithCollectionSystem.UpdateCollection(RunState, Time.deltaTime);
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

        var repairedAmount = cemeteryStateSystem.ApplyNightRepair(RunState, Time.deltaTime);
        if (repairedAmount <= 0)
        {
            return;
        }

        RefreshPresentation();
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
            return;
        }

        if (nightPoiSystem == null)
        {
            ClearKeeperInteractionAvailability();
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
            return;
        }

        RunState.Keeper.CurrentPoiId = poi.Id;
        RunState.Keeper.InteractionState = interactionState;
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
            new Night1()
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
        var upgradeDefs = new List<UpgradeDef>(CMS.GetAll<UpgradeDef>());
        upgradeDefs.Sort(CompareUpgradeDefsForDisplay);

        for (var i = 0; i < upgradeDefs.Count; i++)
        {
            var upgradeDef = upgradeDefs[i];
            if (upgradeDef == null)
            {
                continue;
            }

            if (RunState.PurchasedUpgradeIds != null && RunState.PurchasedUpgradeIds.Contains(upgradeDef.Id))
            {
                continue;
            }

            if (!UpgradeSystem.SupportsEffectType(upgradeDef.EffectType))
            {
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
        var effectValue = Mathf.Max(0, upgradeDef.EffectValue);

        return upgradeDef.EffectType switch
        {
            UpgradeEffectType.FaithIncomeBonus => $"+{effectValue} Faith per payout at Faith Point",
            UpgradeEffectType.CemeteryRepair => $"+{effectValue} cemetery repair",
            UpgradeEffectType.CemeteryMaxStateBonus => $"+{effectValue} cemetery max state",
            _ => string.Empty
        };
    }

    private static string BuildRewardSummary(DayRewardData reward)
    {
        if (reward == null || !reward.HasAnyReward)
        {
            return "no reward";
        }

        if (reward.FaithReward > 0 && reward.GoldReward > 0)
        {
            return $"+{reward.FaithReward} Faith, +{reward.GoldReward} Gold";
        }

        if (reward.FaithReward > 0)
        {
            return $"+{reward.FaithReward} Faith";
        }

        return $"+{reward.GoldReward} Gold";
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

        RunState.CurrentPhase = GamePhase.Defeat;
        waveSystem.StopWave();
        Time.timeScale = 0f;
        Debug.Log("Defeat: cemetery destroyed");
        RefreshPresentation();
        return true;
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

        var requiredDayCount = Mathf.Max(1, targetSurvivedDays);
        if (RunState.CurrentDay < requiredDayCount)
        {
            return false;
        }

        RunState.CurrentPhase = GamePhase.Win;
        waveSystem.StopWave();
        Time.timeScale = 0f;
        Debug.Log($"Victory: survived day {RunState.CurrentDay} of {requiredDayCount}");
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

    private void HandleDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            EnterNight();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            EnterDay();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            AddFaith(DebugFaithAmount);
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            AddGold(DebugGoldAmount);
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            DamageCemetery(DebugCemeteryDamageAmount);
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            TryRingBell(debugBellId);
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            TrySpawnDebugEnemy();
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            TryPurchaseUpgrade(GetDebugUpgradeId());
        }
    }

    private string GetDebugUpgradeId()
    {
        return string.IsNullOrWhiteSpace(debugUpgradeId) ? DefaultDebugUpgradeId : debugUpgradeId;
    }
}
