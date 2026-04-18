using UnityEngine;

public class SingleLaneEnemySpawner
{
    public EnemySpawnResult TrySpawnEnemy(EnemyDef enemyDef, SingleLaneHost laneHost)
    {
        if (laneHost == null)
        {
            return EnemySpawnResult.Failure(UnitSpawnFailureReason.MissingLaneHost);
        }

        if (laneHost.EnemySpawnPoint == null || laneHost.EnemyForwardTarget == null)
        {
            return EnemySpawnResult.Failure(UnitSpawnFailureReason.MissingSpawnPoint);
        }

        if (enemyDef == null || enemyDef.ViewPrefab == null)
        {
            return EnemySpawnResult.Failure(UnitSpawnFailureReason.MissingPrefab);
        }

        var spawnPosition = laneHost.EnemySpawnPoint.position;
        if (laneHost.EnemySpawnSpreadRadius > 0f)
        {
            var angle = Random.value * Mathf.PI * 2f;
            var radius = Mathf.Sqrt(Random.value) * laneHost.EnemySpawnSpreadRadius;
            var offset = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            spawnPosition += offset;
        }

        var spawnedGameObject = Object.Instantiate(
            enemyDef.ViewPrefab,
            spawnPosition,
            laneHost.EnemySpawnPoint.rotation);

        var laneEnemy = spawnedGameObject.GetComponent<LaneEnemy>();
        if (laneEnemy == null)
        {
            laneEnemy = spawnedGameObject.AddComponent<LaneEnemy>();
        }

        laneEnemy.Initialize(enemyDef, laneHost.EnemyForwardTarget.position);
        return EnemySpawnResult.Success(laneEnemy);
    }
}
