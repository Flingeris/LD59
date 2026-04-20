using System.Collections.Generic;

public class Night1 : NightDefinition
{
    public override string Id => "night_1";
    public override float DurationSeconds => 30f;

    protected override IReadOnlyList<NightWaveEntry> BuildWaveEntries()
    {
        return new List<NightWaveEntry>
        {
            Wave(2f, Spawn("en1", 1)),
            Wave(30f, Spawn("en1", 2)),
            // Wave(38f, Spawn("en1", 2)),
            // Wave(50f, Spawn("en1", 2)),
        };
    }
}

public class Night2 : NightDefinition
{
    public override string Id => "night_2";
    public override float DurationSeconds => 60f;

    protected override IReadOnlyList<NightWaveEntry> BuildWaveEntries()
    {
        return new List<NightWaveEntry>
        {
            Wave(10f, Spawn("en1", 1)),
            Wave(20f, Spawn("en1", 2)),
            Wave(32f, Spawn("en1", 2)),
            Wave(44f, Spawn("en1", 2)),
            Wave(54f, Spawn("en1", 3)),
        };
    }
}

public class Night3 : NightDefinition
{
    public override string Id => "night_3";
    public override float DurationSeconds => 66f;

    protected override IReadOnlyList<NightWaveEntry> BuildWaveEntries()
    {
        return new List<NightWaveEntry>
        {
            Wave(8f, Spawn("en1", 1)),
            Wave(18f, Spawn("en1", 2)),
            Wave(30f, Spawn("en1", 3)),
            Wave(42f, Spawn("en1", 3)),
            Wave(56f, Spawn("en1", 3)),
        };
    }
}

public class Night4 : NightDefinition
{
    public override string Id => "night_4";
    public override float DurationSeconds => 72f;

    protected override IReadOnlyList<NightWaveEntry> BuildWaveEntries()
    {
        return new List<NightWaveEntry>
        {
            Wave(8f, Spawn("en1", 2)),
            Wave(20f, Spawn("en1", 2)),
            Wave(32f, Spawn("en1", 3)),
            Wave(44f, Spawn("en1", 3)),
            Wave(58f, Spawn("en1", 4)),
        };
    }
}

public class Night5 : NightDefinition
{
    public override string Id => "night_5";
    public override float DurationSeconds => 78f;

    protected override IReadOnlyList<NightWaveEntry> BuildWaveEntries()
    {
        return new List<NightWaveEntry>
        {
            Wave(8f, Spawn("en1", 2)),
            Wave(18f, Spawn("en1", 3)),
            Wave(30f, Spawn("en1", 3)),
            Wave(42f, Spawn("en1", 3)),
            Wave(54f, Spawn("en1", 3)),
            Wave(66f, Spawn("en1", 2)),
        };
    }
}