using UnityEngine;

public class SingleLaneUnitSpawner
{
    public UnitSpawnResult TrySpawnPlayerUnit(UnitDef unitDef, SingleLaneHost laneHost)
    {
        if (laneHost == null)
        {
            return UnitSpawnResult.Failure(UnitSpawnFailureReason.MissingLaneHost);
        }

        if (laneHost.PlayerSpawnPoint == null)
        {
            return UnitSpawnResult.Failure(UnitSpawnFailureReason.MissingSpawnPoint);
        }

        if (unitDef == null || unitDef.ViewPrefab == null)
        {
            return UnitSpawnResult.Failure(UnitSpawnFailureReason.MissingPrefab);
        }

        var spawnedGameObject = Object.Instantiate(
            unitDef.ViewPrefab,
            laneHost.PlayerSpawnPoint.position,
            laneHost.PlayerSpawnPoint.rotation);

        var laneUnit = spawnedGameObject.GetComponent<LaneUnit>();
        if (laneUnit == null)
        {
            laneUnit = spawnedGameObject.AddComponent<LaneUnit>();
        }

        laneUnit.Initialize(unitDef);
        return UnitSpawnResult.Success(laneUnit);
    }
}
