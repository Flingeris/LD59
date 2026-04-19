using System.Collections.Generic;
using UnityEngine;

public abstract class NightDefinition
{
    private IReadOnlyList<NightWaveEntry> cachedWaveEntries;

    public abstract string Id { get; }
    public abstract float DurationSeconds { get; }

    public IReadOnlyList<NightWaveEntry> WaveEntries
    {
        get
        {
            cachedWaveEntries ??= BuildWaveEntries();
            return cachedWaveEntries;
        }
    }

    protected abstract IReadOnlyList<NightWaveEntry> BuildWaveEntries();

    protected static NightWaveEntry Wave(float triggerTime, params NightWaveSpawnEntry[] spawns)
    {
        return new NightWaveEntry(triggerTime, spawns);
    }

    protected static NightWaveSpawnEntry Spawn(string enemyId, int count)
    {
        return new NightWaveSpawnEntry(enemyId, Mathf.Max(0, count));
    }
}
