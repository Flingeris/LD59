using System;
using System.Collections.Generic;
using UnityEngine;

public class NightPoiSystem
{
    private static readonly NightPoiType[] RequiredPoiTypes =
    {
        NightPoiType.Bells,
        NightPoiType.FaithPoint,
        NightPoiType.RepairPoint
    };

    private readonly Dictionary<string, NightPointOfInterest> poisById = new(StringComparer.Ordinal);
    private readonly Dictionary<NightPoiType, NightPointOfInterest> poisByType = new();

    public int RegisteredPoiCount => poisById.Count;

    public void RebuildFromScene(IReadOnlyList<NightPointOfInterest> scenePois)
    {
        poisById.Clear();
        poisByType.Clear();

        if (scenePois == null || scenePois.Count == 0)
        {
            Debug.LogWarning("Night POI setup incomplete: no points of interest found in scene");
            return;
        }

        for (var i = 0; i < scenePois.Count; i++)
        {
            var poi = scenePois[i];
            if (poi == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(poi.Id))
            {
                Debug.LogWarning($"Night POI setup incomplete: scene object '{poi.name}' has empty id");
                continue;
            }

            if (poisById.ContainsKey(poi.Id))
            {
                Debug.LogWarning($"Night POI setup incomplete: duplicate poi id '{poi.Id}'");
                continue;
            }

            if (poi.TryGetWorldInteractionValidationError(out var validationError))
            {
                Debug.LogWarning(
                    $"Night POI setup incomplete for '{poi.Id}' on '{poi.name}': {validationError}");
            }

            poisById.Add(poi.Id, poi);

            if (!poisByType.ContainsKey(poi.Type))
            {
                poisByType.Add(poi.Type, poi);
            }
        }

        for (var i = 0; i < RequiredPoiTypes.Length; i++)
        {
            var requiredPoiType = RequiredPoiTypes[i];
            if (!poisByType.ContainsKey(requiredPoiType))
            {
                Debug.LogWarning($"Night POI setup incomplete: missing required poi type '{requiredPoiType}'");
            }
        }
    }

    public bool TryGetPoiById(string poiId, out NightPointOfInterest poi)
    {
        if (string.IsNullOrWhiteSpace(poiId))
        {
            poi = null;
            return false;
        }

        return poisById.TryGetValue(poiId, out poi);
    }

    public bool TryGetPoiByType(NightPoiType poiType, out NightPointOfInterest poi)
    {
        return poisByType.TryGetValue(poiType, out poi);
    }

    public bool TryResolveKeeperInteraction(
        Vector2 keeperPosition,
        out NightPointOfInterest poi,
        out KeeperInteractionState interactionState)
    {
        poi = null;
        interactionState = KeeperInteractionState.None;

        var bestDistance = float.MaxValue;
        foreach (var registeredPoi in poisById.Values)
        {
            if (registeredPoi == null || !registeredPoi.IsKeeperInInteractionRange(keeperPosition))
            {
                continue;
            }

            var distance = Vector2.SqrMagnitude(registeredPoi.GetKeeperTargetWorldPosition() - keeperPosition);
            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            poi = registeredPoi;
        }

        if (poi == null)
        {
            return false;
        }

        interactionState = ToKeeperInteractionState(poi.Type);
        return true;
    }

    public static KeeperInteractionState ToKeeperInteractionState(NightPoiType poiType)
    {
        return poiType switch
        {
            NightPoiType.Bells => KeeperInteractionState.Bells,
            NightPoiType.FaithPoint => KeeperInteractionState.FaithPoint,
            NightPoiType.RepairPoint => KeeperInteractionState.RepairPoint,
            _ => KeeperInteractionState.None
        };
    }
}
