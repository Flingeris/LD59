using UnityEngine;
using System.Collections.Generic;

public class Main : MonoBehaviour
{
    private const int DebugFaithAmount = 10;
    private const int DebugGoldAmount = 10;
    private const int DebugCemeteryDamageAmount = 10;

    [SerializeField] private SingleLaneHost singleLaneHost;
    [SerializeField] private SingleLaneEncounterCoordinator laneEncounterCoordinator;
    [SerializeField] private string debugBellId;
    [SerializeField] private EnemyDef debugEnemyDef;
    [SerializeField] private WaveSpawnEntry[] fixedNightWave;

    private BellSystem bellSystem;
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
        bellSystem = new BellSystem();
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
        EnterDay();
        ValidateLanePrototypeSetup();
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
        return true;
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
            Debug.LogWarning("Lane prototype setup incomplete: assign SingleLaneHost and SingleLaneEncounterCoordinator on Main");
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
        Debug.Log("Night completed");
        EnterDay();
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
    }
}
