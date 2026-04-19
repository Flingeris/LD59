using System;
using UnityEngine;
using System.Collections.Generic;

public class Main : MonoBehaviour
{
    private const int DebugFaithAmount = 10;
    private const int DebugGoldAmount = 10;
    private const int DebugCemeteryDamageAmount = 10;
    private const string DefaultDebugUpgradeId = "upgrade_morning_prayers";

    [SerializeField] private SingleLaneHost singleLaneHost;
    [SerializeField] private SingleLaneEncounterCoordinator laneEncounterCoordinator;
    [SerializeField] private string debugBellId;
    [SerializeField] private string debugUpgradeId;
    [SerializeField] private EnemyDef debugEnemyDef;
    [SerializeField] private WaveSpawnEntry[] fixedNightWave;
    [Min(0)] [SerializeField] private int dayFaithReward = 20;
    [Min(0)] [SerializeField] private int completedNightGoldReward = 10;

    private BellSystem bellSystem;
    private DayRewardSystem dayRewardSystem;
    private UpgradeSystem upgradeSystem;
    private SingleLaneUnitSpawner unitSpawner;
    private SingleLaneEnemySpawner enemySpawner;
    private WaveSystem waveSystem;
    private bool laneSetupWarningShown;
    private readonly List<string> readyWaveSpawnEnemyIds = new();

    public RunState RunState { get; private set; }

    private void Awake()
    {
        G.main = this;
        RunState = RunState.CreateInitial();
        RunState.DayFaithIncome = Mathf.Max(0, dayFaithReward);
        bellSystem = new BellSystem();
        dayRewardSystem = new DayRewardSystem();
        upgradeSystem = new UpgradeSystem();
        unitSpawner = new SingleLaneUnitSpawner();
        enemySpawner = new SingleLaneEnemySpawner();
        waveSystem = new WaveSystem();

        if (laneEncounterCoordinator != null)
        {
            laneEncounterCoordinator.OnEnemyBreakthrough = HandleEnemyBreakthrough;
        }
    }

    private void Start()
    {
        BindHud();
        EnterDay();
        ApplyInitialDayReward();
        ValidateLanePrototypeSetup();
    }

    private void OnDestroy()
    {
        UnbindHud();
    }

    private void Update()
    {
        UpdateWaveSpawning();
        UpdateNightCompletion();
        HandleDebugInput();
    }

    public bool EnterDay()
    {
        if (RunState.CurrentPhase == GamePhase.Day)
        {
            return false;
        }

        if (RunState.CurrentNight > 0)
        {
            RunState.CurrentDay++;
        }

        RunState.CurrentPhase = GamePhase.Day;
        waveSystem.StopWave();
        RefreshPresentation();
        return true;
    }

    public bool EnterNight()
    {
        if (RunState.CurrentPhase == GamePhase.Night)
        {
            return false;
        }

        RunState.CurrentNight++;
        RunState.CurrentPhase = GamePhase.Night;
        StartNightWave();
        RefreshPresentation();
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
        RunState.CemeteryState -= amount;

        if (RunState.CemeteryState < 0)
        {
            RunState.CemeteryState = 0;
        }
    }

    private void HandleEnemyBreakthrough(LaneEnemy laneEnemy)
    {
        DamageCemetery(1);
        Debug.Log("Enemy breakthrough: cemetery damaged");
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
        waveSystem.StartWave(fixedNightWave);
        Debug.Log("Night wave started");
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
        EnterDay();
    }

    private void ApplyInitialDayReward()
    {
        ApplyDayReward(dayRewardSystem.CreateInitialDayReward(RunState.DayFaithIncome), "Initial day reward");
    }

    private void ApplyCompletedNightReward()
    {
        ApplyDayReward(
            dayRewardSystem.CreateCompletedNightReward(
                RunState.CurrentNight,
                RunState.DayFaithIncome,
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

        Debug.Log(
            $"{rewardSource}: +{RunState.LastDayReward.FaithReward} Faith, +{RunState.LastDayReward.GoldReward} Gold");

        RefreshPresentation();
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
    }

    private void UnbindHud()
    {
        if (G.HUD == null)
        {
            return;
        }

        G.HUD.DayScreenStartNightRequested -= HandleDayScreenStartNightRequested;
        G.HUD.DayScreenUpgradePurchaseRequested -= HandleDayScreenUpgradePurchaseRequested;
    }

    private void HandleDayScreenStartNightRequested()
    {
        TryStartNightFromDayScreen();
    }

    private void HandleDayScreenUpgradePurchaseRequested(string upgradeId)
    {
        TryPurchaseUpgrade(upgradeId);
    }

    private void RefreshPresentation()
    {
        if (G.HUD == null || RunState == null)
        {
            return;
        }

        if (RunState.CurrentPhase == GamePhase.Day)
        {
            G.HUD.ShowDayScreen(RunState, BuildDayUpgradeDisplayItems());
        }
        else
        {
            G.HUD.HideDayScreen();
        }
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
            UpgradeEffectType.FaithIncomeBonus => $"+{effectValue} day Faith income",
            UpgradeEffectType.CemeteryRepair => $"+{effectValue} cemetery repair",
            UpgradeEffectType.CemeteryMaxStateBonus => $"+{effectValue} cemetery max state",
            _ => string.Empty
        };
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
