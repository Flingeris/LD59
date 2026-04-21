using System.Collections.Generic;
using UnityEngine;

public static class BellgraveBalance
{
    public static class Run
    {
        public const int InitialCemeteryState = 55;
        public const float InitialKeeperMoveSpeed = 2.4f;
        public const float KeeperArrivalDistance = 0.05f;

        public const int StartingNightFaith = 10;
        public const int FaithCollectionPayoutAmount = 4;
        public const float FaithCollectionIntervalSeconds = 3f;

        public const int NightCemeteryRepairAmount = 1;
        public const float NightCemeteryRepairIntervalSeconds = 1f;

        public const int TargetSurvivedDays = 6;
    }

    public static class Content
    {
        public static IEnumerable<ContentDef> BuildRuntimeDefs()
        {
            yield return CreateBellSmall();
            yield return CreateBellZombie();
            yield return CreateBellVampire();

            yield return CreateSkel1();
            yield return CreateZombie();
            yield return CreateVampire();

            yield return CreateEn1();
            yield return CreateEnRunner();
            yield return CreateEnBrute();

            yield return CreateMorningPrayers();
            yield return CreateSacredRepairs();
            yield return CreateStoneBoundary();
            yield return CreateVigilHymn();
            yield return CreateGraveOfferings();
            yield return CreateQuickenedBones();
            yield return CreateLongerService();
            yield return CreateSharpenedBones();
            yield return CreateVampiricVigil();
            yield return CreateZombieFortitude();
            yield return CreateSwifterPrayers();
            yield return CreateNightRepairRite();
        }

        public static BellDef CreateBellSmall()
        {
            var def = ScriptableObject.CreateInstance<BellDef>();
            def.InitializeRuntime(
                "bell_small",
                "Bone Bell",
                7,
                2.75f,
                "skel1",
                null,
                LoadRequired<GameObject>("ContentPrefabs/Bells/Bell1", "bell_small"));
            return def;
        }

        public static BellDef CreateBellZombie()
        {
            var def = ScriptableObject.CreateInstance<BellDef>();
            def.InitializeRuntime(
                "bell_zombie",
                "Grave Bell",
                14,
                6f,
                "zombie",
                null,
                LoadRequired<GameObject>("ContentPrefabs/Bells/Bell2", "bell_zombie"));
            return def;
        }

        public static BellDef CreateBellVampire()
        {
            var def = ScriptableObject.CreateInstance<BellDef>();
            def.InitializeRuntime(
                "bell_vampire",
                "Blood Bell",
                24,
                10f,
                "vampire",
                null,
                LoadRequired<GameObject>("ContentPrefabs/Bells/Bell3", "bell_vampire"));
            return def;
        }

        public static UnitDef CreateSkel1()
        {
            var def = ScriptableObject.CreateInstance<UnitDef>();
            def.InitializeRuntime(
                "skel1",
                10,
                4,
                0.95f,
                1.1f,
                17.5f,
                LoadRequired<GameObject>("ContentPrefabs/Units/Skel", "skel1"));
            return def;
        }

        public static UnitDef CreateZombie()
        {
            var def = ScriptableObject.CreateInstance<UnitDef>();
            def.InitializeRuntime(
                "zombie",
                30,
                6,
                1.45f,
                0.7f,
                20f,
                LoadRequired<GameObject>("ContentPrefabs/Units/Zomb", "zombie"));
            return def;
        }

        public static UnitDef CreateVampire()
        {
            var def = ScriptableObject.CreateInstance<UnitDef>();
            def.InitializeRuntime(
                "vampire",
                20,
                999,
                0.55f,
                1.9f,
                2.5f,
                LoadRequired<GameObject>("ContentPrefabs/Units/Vamp", "vampire"));
            return def;
        }

        public static EnemyDef CreateEn1()
        {
            var def = ScriptableObject.CreateInstance<EnemyDef>();
            def.InitializeRuntime(
                "en1",
                9,
                2,
                5,
                1.05f,
                1.85f,
                LoadRequired<GameObject>("ContentPrefabs/Enemies/En1", "en1"));
            return def;
        }

        public static EnemyDef CreateEnRunner()
        {
            var def = ScriptableObject.CreateInstance<EnemyDef>();
            def.InitializeRuntime(
                "en_runner",
                7,
                1,
                6,
                0.8f,
                2.8f,
                LoadRequired<GameObject>("ContentPrefabs/Enemies/En2", "en_runner"));
            return def;
        }

        public static EnemyDef CreateEnBrute()
        {
            var def = ScriptableObject.CreateInstance<EnemyDef>();
            def.InitializeRuntime(
                "en_brute",
                28,
                4,
                10,
                1.2f,
                1.0f,
                LoadRequired<GameObject>("ContentPrefabs/Enemies/En3", "en_brute"));
            return def;
        }

        public static UpgradeDef CreateMorningPrayers()
        {
            return CreateUpgrade(
                "upgrade_morning_prayers",
                "Morning Prayers",
                18,
                UpgradeEffectType.StartingNightFaithBonus,
                3);
        }

        public static UpgradeDef CreateSacredRepairs()
        {
            return CreateUpgrade(
                "upgrade_sacred_repairs",
                "Sacred Repairs",
                22,
                UpgradeEffectType.CemeteryRepair,
                5);
        }

        public static UpgradeDef CreateStoneBoundary()
        {
            return CreateUpgrade(
                "upgrade_stone_boundary",
                "Stone Boundary",
                30,
                UpgradeEffectType.CemeteryMaxStateBonus,
                10);
        }

        public static UpgradeDef CreateVigilHymn()
        {
            return CreateUpgrade(
                "upgrade_vigil_hymn",
                "Vigil Hymn",
                24,
                UpgradeEffectType.BellFaithCostModifier,
                -1);
        }

        public static UpgradeDef CreateGraveOfferings()
        {
            return CreateUpgrade(
                "upgrade_grave_offerings",
                "Grave Offerings",
                18,
                UpgradeEffectType.FaithIncomeBonus,
                1);
        }

        public static UpgradeDef CreateQuickenedBones()
        {
            return CreateUpgrade(
                "upgrade_quickened_bones",
                "Quickened Bones",
                18,
                UpgradeEffectType.KeeperMoveSpeedBonus,
                0.5f);
        }

        public static UpgradeDef CreateLongerService()
        {
            return CreateUpgrade(
                "upgrade_longer_service",
                "Longer Service",
                24,
                UpgradeEffectType.UnitLifetimeModifier,
                3f,
                "skel1");
        }

        public static UpgradeDef CreateSharpenedBones()
        {
            return CreateUpgrade(
                "upgrade_sharpened_bones",
                "Sharpened Bones",
                24,
                UpgradeEffectType.UnitDamageModifier,
                1f,
                "skel1");
        }

        public static UpgradeDef CreateVampiricVigil()
        {
            return CreateUpgrade(
                "upgrade_vampiric_vigil",
                "Vampiric Vigil",
                32,
                UpgradeEffectType.UnitLifetimeModifier,
                0.5f,
                "vampire");
        }

        public static UpgradeDef CreateZombieFortitude()
        {
            return CreateUpgrade(
                "upgrade_zombie_fortitude",
                "Zombie Fortitude",
                24,
                UpgradeEffectType.UnitHpModifier,
                6f,
                "zombie");
        }

        public static UpgradeDef CreateSwifterPrayers()
        {
            return CreateUpgrade(
                "upgrade_swifter_prayers",
                "Swifter Prayers",
                18,
                UpgradeEffectType.FaithCollectionIntervalModifier,
                0.25f);
        }

        public static UpgradeDef CreateNightRepairRite()
        {
            return CreateUpgrade(
                "upgrade_night_repair_rite",
                "Night Repair Rite",
                30,
                UpgradeEffectType.NightInstantRepairCharge,
                6f);
        }

        private static UpgradeDef CreateUpgrade(
            string id,
            string displayName,
            int price,
            UpgradeEffectType effectType,
            float effectValue,
            string targetUnitId = null)
        {
            var def = ScriptableObject.CreateInstance<UpgradeDef>();
            def.InitializeRuntime(id, displayName, price, effectType, effectValue, targetUnitId);
            return def;
        }
    }

    public static class Nights
    {
        public static readonly NightWaveBalance Night1 = new(
            38f,
            WaveAt(6f, Spawn("en1", 1)),
            WaveAt(20f, Spawn("en1", 1)),
            WaveAt(31f, Spawn("en1", 2)));

        public static readonly NightWaveBalance Night2 = new(
            50f,
            WaveAt(6f, Spawn("en1", 2)),
            WaveAt(18f, Spawn("en_runner", 2)),
            WaveAt(30f, Spawn("en1", 2), Spawn("en_runner", 1)),
            WaveAt(42f, Spawn("en1", 3)));

        public static readonly NightWaveBalance Night3 = new(
            58f,
            WaveAt(6f, Spawn("en1", 2)),
            WaveAt(16f, Spawn("en_runner", 2)),
            WaveAt(28f, Spawn("en1", 3)),
            WaveAt(40f, Spawn("en_brute", 1), Spawn("en1", 2)),
            WaveAt(50f, Spawn("en_runner", 2), Spawn("en1", 1)));

        public static readonly NightWaveBalance Night4 = new(
            64f,
            WaveAt(6f, Spawn("en_runner", 2)),
            WaveAt(18f, Spawn("en1", 3)),
            WaveAt(30f, Spawn("en_brute", 1)),
            WaveAt(42f, Spawn("en1", 2), Spawn("en_runner", 2)),
            WaveAt(54f, Spawn("en_brute", 1), Spawn("en1", 1)));

        public static readonly NightWaveBalance Night5 = new(
            74f,
            WaveAt(6f, Spawn("en1", 2)),
            WaveAt(16f, Spawn("en_runner", 2)),
            WaveAt(28f, Spawn("en_brute", 1), Spawn("en1", 2)),
            WaveAt(40f, Spawn("en1", 3), Spawn("en_runner", 2)),
            WaveAt(54f, Spawn("en_brute", 1), Spawn("en_runner", 2)),
            WaveAt(66f, Spawn("en1", 3), Spawn("en_runner", 2)));

        public static readonly NightWaveBalance Night6 = new(
            84f,
            WaveAt(6f, Spawn("en1", 2)),
            WaveAt(14f, Spawn("en_runner", 2)),
            WaveAt(24f, Spawn("en1", 3)),
            WaveAt(34f, Spawn("en_runner", 2), Spawn("en1", 2)),
            WaveAt(46f, Spawn("en_brute", 1), Spawn("en1", 2)),
            WaveAt(58f, Spawn("en_runner", 3)),
            WaveAt(68f, Spawn("en_brute", 1), Spawn("en1", 3)),
            WaveAt(78f, Spawn("en_brute", 1), Spawn("en_runner", 2), Spawn("en1", 2)));

        private static NightSpawnBalance Spawn(string enemyId, int count)
        {
            return new NightSpawnBalance(enemyId, count);
        }

        private static TimedNightSpawnBalance WaveAt(float triggerTime, params NightSpawnBalance[] spawns)
        {
            return new TimedNightSpawnBalance(triggerTime, spawns);
        }
    }

    private static T LoadRequired<T>(string path, string ownerId) where T : Object
    {
        var asset = Resources.Load<T>(path);
        if (asset == null)
        {
            Debug.LogError($"[BellgraveBalance] Missing resource '{path}' for '{ownerId}'");
        }

        return asset;
    }
}

public readonly struct NightWaveBalance
{
    public readonly float DurationSeconds;
    public readonly IReadOnlyList<TimedNightSpawnBalance> Entries;

    public NightWaveBalance(float durationSeconds, params TimedNightSpawnBalance[] entries)
    {
        DurationSeconds = durationSeconds;
        Entries = entries;
    }
}

public readonly struct TimedNightSpawnBalance
{
    public readonly float TriggerTime;
    public readonly IReadOnlyList<NightSpawnBalance> Spawns;

    public TimedNightSpawnBalance(float triggerTime, params NightSpawnBalance[] spawns)
    {
        TriggerTime = triggerTime;
        Spawns = spawns;
    }
}

public readonly struct NightSpawnBalance
{
    public readonly string EnemyId;
    public readonly int Count;

    public NightSpawnBalance(string enemyId, int count)
    {
        EnemyId = enemyId;
        Count = count;
    }
}