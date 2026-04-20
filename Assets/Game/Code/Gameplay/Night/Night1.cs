using System.Collections.Generic;

public class Night1 : NightDefinition
{
    public override string Id => "night_1";
    public override float DurationSeconds => BellgraveBalance.Nights.Night1.DurationSeconds;

    protected override IReadOnlyList<NightWaveEntry> BuildWaveEntries()
    {
        return NightBalanceBuilder.BuildEntries(BellgraveBalance.Nights.Night1);
    }
}

public class Night2 : NightDefinition
{
    public override string Id => "night_2";
    public override float DurationSeconds => BellgraveBalance.Nights.Night2.DurationSeconds;

    protected override IReadOnlyList<NightWaveEntry> BuildWaveEntries()
    {
        return NightBalanceBuilder.BuildEntries(BellgraveBalance.Nights.Night2);
    }
}

public class Night3 : NightDefinition
{
    public override string Id => "night_3";
    public override float DurationSeconds => BellgraveBalance.Nights.Night3.DurationSeconds;

    protected override IReadOnlyList<NightWaveEntry> BuildWaveEntries()
    {
        return NightBalanceBuilder.BuildEntries(BellgraveBalance.Nights.Night3);
    }
}

public class Night4 : NightDefinition
{
    public override string Id => "night_4";
    public override float DurationSeconds => BellgraveBalance.Nights.Night4.DurationSeconds;

    protected override IReadOnlyList<NightWaveEntry> BuildWaveEntries()
    {
        return NightBalanceBuilder.BuildEntries(BellgraveBalance.Nights.Night4);
    }
}

public class Night5 : NightDefinition
{
    public override string Id => "night_5";
    public override float DurationSeconds => BellgraveBalance.Nights.Night5.DurationSeconds;

    protected override IReadOnlyList<NightWaveEntry> BuildWaveEntries()
    {
        return NightBalanceBuilder.BuildEntries(BellgraveBalance.Nights.Night5);
    }
}

public class Night6 : NightDefinition
{
    public override string Id => "night_6";
    public override float DurationSeconds => BellgraveBalance.Nights.Night6.DurationSeconds;

    protected override IReadOnlyList<NightWaveEntry> BuildWaveEntries()
    {
        return NightBalanceBuilder.BuildEntries(BellgraveBalance.Nights.Night6);
    }
}

internal static class NightBalanceBuilder
{
    public static IReadOnlyList<NightWaveEntry> BuildEntries(NightWaveBalance balance)
    {
        var result = new List<NightWaveEntry>(balance.Entries.Count);
        for (var entryIndex = 0; entryIndex < balance.Entries.Count; entryIndex++)
        {
            var timedEntry = balance.Entries[entryIndex];
            var spawnEntries = new List<NightWaveSpawnEntry>(timedEntry.Spawns.Count);
            for (var spawnIndex = 0; spawnIndex < timedEntry.Spawns.Count; spawnIndex++)
            {
                var spawn = timedEntry.Spawns[spawnIndex];
                spawnEntries.Add(new NightWaveSpawnEntry(spawn.EnemyId, spawn.Count));
            }

            result.Add(new NightWaveEntry(timedEntry.TriggerTime, spawnEntries));
        }

        return result;
    }
}
