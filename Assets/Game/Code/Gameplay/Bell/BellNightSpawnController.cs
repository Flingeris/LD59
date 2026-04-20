using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BellNightSpawnController : MonoBehaviour
{
    [System.Serializable]
    private class BellNightSpawnRule
    {
        public string bellId;
        [Min(1)] public int unlockNightIndex = 1;
        public Transform spawnPoint;
    }

    [SerializeField] private BellNightSpawnRule[] spawnRules;

    private readonly HashSet<string> spawnedBellIds = new();
    private bool duplicateRuleWarningShown;

    public bool TrySpawnForNight(int nightIndex)
    {
        if (nightIndex <= 0 || spawnRules == null || spawnRules.Length == 0)
        {
            return false;
        }

        WarnAboutDuplicateRulesIfNeeded();

        var didSpawnAnyBell = false;
        for (var i = 0; i < spawnRules.Length; i++)
        {
            var rule = spawnRules[i];
            if (rule == null || rule.unlockNightIndex != nightIndex || string.IsNullOrWhiteSpace(rule.bellId))
            {
                continue;
            }

            if (IsBellAlreadyPresent(rule.bellId))
            {
                spawnedBellIds.Add(rule.bellId);
                continue;
            }

            if (!TrySpawnBell(rule))
            {
                continue;
            }

            didSpawnAnyBell = true;
        }

        return didSpawnAnyBell;
    }

    private bool TrySpawnBell(BellNightSpawnRule rule)
    {
        var bellDef = CMS.Get<BellDef>(rule.bellId);
        if (bellDef == null)
        {
            Debug.LogWarning($"Bell night spawn skipped: BellDef '{rule.bellId}' not found");
            return false;
        }

        if (bellDef.WorldPrefab == null)
        {
            Debug.LogWarning($"Bell night spawn skipped: BellDef '{rule.bellId}' has no world prefab");
            return false;
        }

        if (rule.spawnPoint == null)
        {
            Debug.LogWarning($"Bell night spawn skipped: '{rule.bellId}' has no spawn point");
            return false;
        }

        var spawnedObject = Instantiate(
            bellDef.WorldPrefab,
            rule.spawnPoint.position,
            rule.spawnPoint.rotation);
        spawnedObject.name = $"Bell_{rule.bellId}";

        var bellWorldObject = spawnedObject.GetComponent<BellWorldObject>();
        if (bellWorldObject == null)
        {
            Debug.LogWarning(
                $"Bell night spawn created '{rule.bellId}', but prefab '{bellDef.WorldPrefab.name}' is missing BellWorldObject");
            Destroy(spawnedObject);
            return false;
        }

        bellWorldObject.InitializeRuntime(rule.bellId);
        spawnedBellIds.Add(rule.bellId);
        return true;
    }

    private bool IsBellAlreadyPresent(string bellId)
    {
        if (spawnedBellIds.Contains(bellId))
        {
            return true;
        }

        var bells = FindObjectsByType<BellWorldObject>();
        for (var i = 0; i < bells.Length; i++)
        {
            var bell = bells[i];
            if (bell != null && bell.BellId == bellId)
            {
                return true;
            }
        }

        return false;
    }

    private void WarnAboutDuplicateRulesIfNeeded()
    {
        if (duplicateRuleWarningShown || spawnRules == null || spawnRules.Length == 0)
        {
            return;
        }

        duplicateRuleWarningShown = true;
        var keys = new HashSet<string>();
        for (var i = 0; i < spawnRules.Length; i++)
        {
            var rule = spawnRules[i];
            if (rule == null || string.IsNullOrWhiteSpace(rule.bellId))
            {
                continue;
            }

            var key = $"{rule.bellId}@{rule.unlockNightIndex}";
            if (!keys.Add(key))
            {
                Debug.LogWarning($"Bell night spawn has duplicate rule '{key}' on '{name}'");
            }
        }
    }
}
