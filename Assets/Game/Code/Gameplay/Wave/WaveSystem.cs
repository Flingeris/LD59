using System.Collections.Generic;

public class WaveSystem
{
    private readonly List<WaveSpawnEntry> activeEntries = new();

    private float elapsedTime;
    private int nextSpawnIndex;
    private bool isRunning;

    public bool IsRunning => isRunning;
    public bool AreAllSpawnsIssued => !isRunning || nextSpawnIndex >= activeEntries.Count;

    public void StartWave(IReadOnlyList<WaveSpawnEntry> entries)
    {
        activeEntries.Clear();
        elapsedTime = 0f;
        nextSpawnIndex = 0;
        isRunning = true;

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

            activeEntries.Add(new WaveSpawnEntry
            {
                enemyId = entry.enemyId,
                spawnTime = entry.spawnTime
            });
        }

        activeEntries.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));
    }

    public void StopWave()
    {
        isRunning = false;
        elapsedTime = 0f;
        nextSpawnIndex = 0;
        activeEntries.Clear();
    }

    public void CollectReadySpawns(float deltaTime, List<string> readyEnemyIds)
    {
        if (!isRunning || readyEnemyIds == null)
        {
            return;
        }

        elapsedTime += deltaTime;

        while (nextSpawnIndex < activeEntries.Count)
        {
            var nextEntry = activeEntries[nextSpawnIndex];
            if (nextEntry.spawnTime > elapsedTime)
            {
                break;
            }

            readyEnemyIds.Add(nextEntry.enemyId);
            nextSpawnIndex++;
        }
    }
}
