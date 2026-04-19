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
        };
    }
}