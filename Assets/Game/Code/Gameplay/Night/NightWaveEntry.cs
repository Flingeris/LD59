using System.Collections.Generic;

public class NightWaveEntry
{
    public NightWaveEntry(float triggerTime, IReadOnlyList<NightWaveSpawnEntry> spawns)
    {
        TriggerTime = triggerTime < 0f ? 0f : triggerTime;
        Spawns = spawns ?? new List<NightWaveSpawnEntry>();
    }

    public float TriggerTime { get; }
    public IReadOnlyList<NightWaveSpawnEntry> Spawns { get; }
}
