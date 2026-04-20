using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CloudLoopController : MonoBehaviour
{
    private static readonly Color ActiveBoundsFillColor = new(0.35f, 0.75f, 1f, 0.08f);
    private static readonly Color ActiveBoundsLineColor = new(0.35f, 0.75f, 1f, 0.95f);
    private static readonly Color RecycleBoundsFillColor = new(1f, 0.75f, 0.2f, 0.05f);
    private static readonly Color RecycleBoundsLineColor = new(1f, 0.75f, 0.2f, 0.9f);
    private static readonly Color DirectionLineColor = new(0.65f, 1f, 0.55f, 0.95f);

    [SerializeField] private LoopingCloud cloudPrefab;
    [SerializeField] private Sprite[] cloudSprites;
    [Min(1)] [SerializeField] private int cloudCount = 4;
    [SerializeField] private Vector2 horizontalBounds = new(-12f, 12f);
    [SerializeField] private Vector2 verticalBounds = new(3f, 6f);
    [SerializeField] private Vector2 speedRange = new(0.2f, 0.45f);
    [Min(0f)] [SerializeField] private float recyclePadding = 2f;
    [SerializeField] private bool moveLeftToRight = true;
    [SerializeField] private bool randomizeSpriteOnRecycle = true;
    [SerializeField] private bool randomizeSpeedOnRecycle = true;

    private readonly List<LoopingCloud> spawnedClouds = new();
    private bool missingSetupWarningShown;

    private void Start()
    {
        SpawnInitialClouds();
    }

    private void OnDrawGizmosSelected()
    {
        var minX = GetMinX();
        var maxX = GetMaxX();
        var minY = GetMinY();
        var maxY = GetMaxY();
        var activeWidth = Mathf.Max(0.01f, maxX - minX);
        var activeHeight = Mathf.Max(0.01f, maxY - minY);
        var recycleWidth = activeWidth + Mathf.Max(0f, recyclePadding) * 2f;
        var boundsCenter = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);

        var previousMatrix = Gizmos.matrix;
        var previousColor = Gizmos.color;
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = ActiveBoundsFillColor;
        Gizmos.DrawCube(boundsCenter, new Vector3(activeWidth, activeHeight, 0.01f));
        Gizmos.color = ActiveBoundsLineColor;
        Gizmos.DrawWireCube(boundsCenter, new Vector3(activeWidth, activeHeight, 0.01f));

        Gizmos.color = RecycleBoundsFillColor;
        Gizmos.DrawCube(boundsCenter, new Vector3(recycleWidth, activeHeight, 0.01f));
        Gizmos.color = RecycleBoundsLineColor;
        Gizmos.DrawWireCube(boundsCenter, new Vector3(recycleWidth, activeHeight, 0.01f));

        DrawDirectionGizmo(minX, maxX, maxY);

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }

    public void RecycleCloud(LoopingCloud cloud)
    {
        if (cloud == null)
        {
            return;
        }

        ConfigureCloud(cloud, false);
    }

    private void SpawnInitialClouds()
    {
        if (spawnedClouds.Count > 0)
        {
            return;
        }

        if (!HasValidSetup())
        {
            return;
        }

        for (var i = 0; i < cloudCount; i++)
        {
            var cloud = Instantiate(cloudPrefab, transform);
            cloud.name = $"{cloudPrefab.name}_{i + 1}";
            spawnedClouds.Add(cloud);
            ConfigureCloud(cloud, true);
        }
    }

    private void ConfigureCloud(LoopingCloud cloud, bool isInitialSpawn)
    {
        if (cloud == null)
        {
            return;
        }

        var sprite = isInitialSpawn || randomizeSpriteOnRecycle
            ? GetRandomSprite()
            : cloud.CurrentSprite;
        var speed = isInitialSpawn || randomizeSpeedOnRecycle
            ? GetRandomSpeed()
            : cloud.CurrentSpeed;

        var localX = isInitialSpawn
            ? Random.Range(GetMinX() - recyclePadding, GetMaxX() + recyclePadding)
            : GetRecycleSpawnX();
        var localY = Random.Range(GetMinY(), GetMaxY());

        cloud.Configure(
            this,
            sprite,
            new Vector2(localX, localY),
            speed,
            moveLeftToRight ? 1f : -1f,
            GetMinX(),
            GetMaxX(),
            recyclePadding);
    }

    private bool HasValidSetup()
    {
        if (cloudPrefab != null && HasAtLeastOneSprite())
        {
            missingSetupWarningShown = false;
            return true;
        }

        if (!missingSetupWarningShown)
        {
            Debug.LogWarning("CloudLoopController setup incomplete: assign a cloud prefab and at least one sprite.");
            missingSetupWarningShown = true;
        }

        return false;
    }

    private bool HasAtLeastOneSprite()
    {
        if (cloudSprites == null || cloudSprites.Length <= 0)
        {
            return false;
        }

        for (var i = 0; i < cloudSprites.Length; i++)
        {
            if (cloudSprites[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private Sprite GetRandomSprite()
    {
        if (cloudSprites == null || cloudSprites.Length <= 0)
        {
            return null;
        }

        for (var attempt = 0; attempt < cloudSprites.Length; attempt++)
        {
            var sprite = cloudSprites[Random.Range(0, cloudSprites.Length)];
            if (sprite != null)
            {
                return sprite;
            }
        }

        for (var i = 0; i < cloudSprites.Length; i++)
        {
            if (cloudSprites[i] != null)
            {
                return cloudSprites[i];
            }
        }

        return null;
    }

    private float GetRandomSpeed()
    {
        return Random.Range(GetMinSpeed(), GetMaxSpeed());
    }

    private float GetRecycleSpawnX()
    {
        return moveLeftToRight
            ? GetMinX() - recyclePadding
            : GetMaxX() + recyclePadding;
    }

    private float GetMinX()
    {
        return Mathf.Min(horizontalBounds.x, horizontalBounds.y);
    }

    private float GetMaxX()
    {
        return Mathf.Max(horizontalBounds.x, horizontalBounds.y);
    }

    private float GetMinY()
    {
        return Mathf.Min(verticalBounds.x, verticalBounds.y);
    }

    private float GetMaxY()
    {
        return Mathf.Max(verticalBounds.x, verticalBounds.y);
    }

    private float GetMinSpeed()
    {
        return Mathf.Max(0f, Mathf.Min(speedRange.x, speedRange.y));
    }

    private float GetMaxSpeed()
    {
        return Mathf.Max(GetMinSpeed(), Mathf.Max(speedRange.x, speedRange.y));
    }

    private void DrawDirectionGizmo(float minX, float maxX, float maxY)
    {
        Gizmos.color = DirectionLineColor;

        var directionY = maxY + 0.5f;
        var arrowStart = moveLeftToRight
            ? new Vector3(minX - recyclePadding, directionY, 0f)
            : new Vector3(maxX + recyclePadding, directionY, 0f);
        var arrowEnd = moveLeftToRight
            ? new Vector3(maxX + recyclePadding, directionY, 0f)
            : new Vector3(minX - recyclePadding, directionY, 0f);

        Gizmos.DrawLine(arrowStart, arrowEnd);
        DrawArrowHead(arrowEnd, moveLeftToRight ? 1f : -1f);
    }

    private void DrawArrowHead(Vector3 tipPosition, float directionSign)
    {
        const float arrowHeadLength = 0.45f;
        const float arrowHeadSpread = 0.18f;

        var backwardX = tipPosition.x - directionSign * arrowHeadLength;
        var upper = new Vector3(backwardX, tipPosition.y + arrowHeadSpread, tipPosition.z);
        var lower = new Vector3(backwardX, tipPosition.y - arrowHeadSpread, tipPosition.z);

        Gizmos.DrawLine(tipPosition, upper);
        Gizmos.DrawLine(tipPosition, lower);
    }
}
