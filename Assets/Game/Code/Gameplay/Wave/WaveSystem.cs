using System.Collections.Generic;

public class WaveSystem
{
    private readonly List<NightWaveEntry> activeEntries = new();
    private float elapsedTime;
    private float durationSeconds;
    private int nextWaveIndex;
    private bool isRunning;

    public bool IsRunning => isRunning;
    public bool AreAllSpawnsIssued => !isRunning || nextWaveIndex >= activeEntries.Count;
    public float ElapsedTime => elapsedTime;
    public float DurationSeconds => durationSeconds;
    public bool HasUpcomingWave => isRunning && nextWaveIndex < activeEntries.Count;
    public float NextWaveTriggerTime => HasUpcomingWave ? activeEntries[nextWaveIndex].TriggerTime : durationSeconds;
    public float PreviousWaveTriggerTime => nextWaveIndex <= 0 ? 0f : activeEntries[nextWaveIndex - 1].TriggerTime;

    public IReadOnlyList<NightWaveEntry> ActiveEntries => activeEntries;

    public void StartWave(NightDefinition nightDefinition)
    {
        activeEntries.Clear();
        elapsedTime = 0f;
        durationSeconds = 0f;
        nextWaveIndex = 0;
        isRunning = nightDefinition != null;

        if (nightDefinition == null)
        {
            return;
        }

        durationSeconds = nightDefinition.DurationSeconds;
        var entries = nightDefinition.WaveEntries;
        if (entries == null)
        {
            return;
        }

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (durationSeconds > 0f && entry.TriggerTime > durationSeconds)
            {
                continue;
            }

            activeEntries.Add(entry);
        }

        activeEntries.Sort((a, b) => a.TriggerTime.CompareTo(b.TriggerTime));
    }

    public void StopWave()
    {
        isRunning = false;
        elapsedTime = 0f;
        durationSeconds = 0f;
        nextWaveIndex = 0;
        activeEntries.Clear();
    }

    public void CollectReadySpawns(float deltaTime, List<string> readyEnemyIds)
    {
        if (!isRunning || readyEnemyIds == null)
        {
            return;
        }

        elapsedTime += deltaTime;

        while (nextWaveIndex < activeEntries.Count)
        {
            var nextEntry = activeEntries[nextWaveIndex];
            if (nextEntry.TriggerTime > elapsedTime)
            {
                break;
            }

            AppendReadySpawns(nextEntry, readyEnemyIds);
            nextWaveIndex++;
        }
    }

    private static void AppendReadySpawns(NightWaveEntry waveEntry, List<string> readyEnemyIds)
    {
        if (waveEntry?.Spawns == null || readyEnemyIds == null)
        {
            return;
        }

        for (var i = 0; i < waveEntry.Spawns.Count; i++)
        {
            var spawnEntry = waveEntry.Spawns[i];
            if (spawnEntry == null || string.IsNullOrWhiteSpace(spawnEntry.EnemyId) || spawnEntry.Count <= 0)
            {
                continue;
            }

            for (var spawnIndex = 0; spawnIndex < spawnEntry.Count; spawnIndex++)
            {
                readyEnemyIds.Add(spawnEntry.EnemyId);
            }
        }
    }
}