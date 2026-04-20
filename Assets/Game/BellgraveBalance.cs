using System.Collections.Generic;
using UnityEngine;

public static class BellgraveBalance
{
    public static class Run
    {
        public const int InitialCemeteryState = 60;
        public const float InitialKeeperMoveSpeed = 3f;
        public const float KeeperArrivalDistance = 0.05f;

        public const int StartingNightFaith = 12;
        public const int FaithCollectionPayoutAmount = 4;
        public const float FaithCollectionIntervalSeconds = 2.5f;

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
                6,
                2.5f,
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
                10,
                5f,
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
                16,
                7f,
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
                3,
                0.9f,
                1.15f,
                18f,
                LoadRequired<GameObject>("ContentPrefabs/Units/Skel", "skel1"));
            return def;
        }

        public static UnitDef CreateZombie()
        {
            var def = ScriptableObject.CreateInstance<UnitDef>();
            def.InitializeRuntime(
                "zombie",
                26,
                5,
                1.25f,
                0.8f,
                24f,
                LoadRequired<GameObject>("ContentPrefabs/Units/Zomb", "zombie"));
            return def;
        }

        public static UnitDef CreateVampire()
        {
            var def = ScriptableObject.CreateInstance<UnitDef>();
            def.InitializeRuntime(
                "vampire",
                2,
                999,
                0.35f,
                2.1f,
                5f,
                LoadRequired<GameObject>("ContentPrefabs/Units/Vamp", "vampire"));
            return def;
        }

        public static EnemyDef CreateEn1()
        {
            var def = ScriptableObject.CreateInstance<EnemyDef>();
            def.InitializeRuntime(
                "en1",
                8,
                2,
                4,
                1.15f,
                1.8f,
                LoadRequired<GameObject>("ContentPrefabs/Enemies/En1", "en1"));
            return def;
        }

        public static EnemyDef CreateEnRunner()
        {
            var def = ScriptableObject.CreateInstance<EnemyDef>();
            def.InitializeRuntime(
                "en_runner",
                6,
                1,
                5,
                0.85f,
                2.7f,
                LoadRequired<GameObject>("ContentPrefabs/Enemies/En2", "en_runner"));
            return def;
        }

        public static EnemyDef CreateEnBrute()
        {
            var def = ScriptableObject.CreateInstance<EnemyDef>();
            def.InitializeRuntime(
                "en_brute",
                24,
                4,
                8,
                1.4f,
                0.9f,
                LoadRequired<GameObject>("ContentPrefabs/Enemies/En3", "en_brute"));
            return def;
        }

        public static UpgradeDef CreateMorningPrayers()
        {
            return CreateUpgrade(
                "upgrade_morning_prayers",
                "Morning Prayers",
                12,
                UpgradeEffectType.StartingNightFaithBonus,
                4);
        }

        public static UpgradeDef CreateSacredRepairs()
        {
            return CreateUpgrade(
                "upgrade_sacred_repairs",
                "Sacred Repairs",
                12,
                UpgradeEffectType.CemeteryRepair,
                6);
        }

        public static UpgradeDef CreateStoneBoundary()
        {
            return CreateUpgrade(
                "upgrade_stone_boundary",
                "Stone Boundary",
                20,
                UpgradeEffectType.CemeteryMaxStateBonus,
                12);
        }

        public static UpgradeDef CreateVigilHymn()
        {
            return CreateUpgrade(
                "upgrade_vigil_hymn",
                "Vigil Hymn",
                18,
                UpgradeEffectType.BellFaithCostModifier,
                -2);
        }

        public static UpgradeDef CreateGraveOfferings()
        {
            return CreateUpgrade(
                "upgrade_grave_offerings",
                "Grave Offerings",
                14,
                UpgradeEffectType.FaithIncomeBonus,
                2);
        }

        public static UpgradeDef CreateQuickenedBones()
        {
            return CreateUpgrade(
                "upgrade_quickened_bones",
                "Quickened Bones",
                16,
                UpgradeEffectType.KeeperMoveSpeedBonus,
                1);
        }

        public static UpgradeDef CreateLongerService()
        {
            return CreateUpgrade(
                "upgrade_longer_service",
                "Longer Service",
                16,
                UpgradeEffectType.UnitLifetimeModifier,
                4f,
                "skel1");
        }

        public static UpgradeDef CreateSharpenedBones()
        {
            return CreateUpgrade(
                "upgrade_sharpened_bones",
                "Sharpened Bones",
                18,
                UpgradeEffectType.UnitDamageModifier,
                1f,
                "skel1");
        }

        public static UpgradeDef CreateVampiricVigil()
        {
            return CreateUpgrade(
                "upgrade_vampiric_vigil",
                "Vampiric Vigil",
                18,
                UpgradeEffectType.UnitLifetimeModifier,
                1.5f,
                "vampire");
        }

        public static UpgradeDef CreateZombieFortitude()
        {
            return CreateUpgrade(
                "upgrade_zombie_fortitude",
                "Zombie Fortitude",
                17,
                UpgradeEffectType.UnitHpModifier,
                8f,
                "zombie");
        }

        public static UpgradeDef CreateSwifterPrayers()
        {
            return CreateUpgrade(
                "upgrade_swifter_prayers",
                "Swifter Prayers",
                17,
                UpgradeEffectType.FaithCollectionIntervalModifier,
                0.35f);
        }

        public static UpgradeDef CreateNightRepairRite()
        {
            return CreateUpgrade(
                "upgrade_night_repair_rite",
                "Night Repair Rite",
                20,
                UpgradeEffectType.NightInstantRepairCharge,
                8f);
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
            WaveAt(4f, Spawn("en1", 1)),
            WaveAt(16f, Spawn("en1", 2)),
            WaveAt(28f, Spawn("en1", 2)));

        public static readonly NightWaveBalance Night2 = new(
            48f,
            WaveAt(6f, Spawn("en1", 2)),
            WaveAt(18f, Spawn("en_runner", 2)),
            WaveAt(30f, Spawn("en1", 2), Spawn("en_runner", 1)),
            WaveAt(40f, Spawn("en1", 2)));

        public static readonly NightWaveBalance Night3 = new(
            56f,
            WaveAt(6f, Spawn("en1", 2)),
            WaveAt(16f, Spawn("en_runner", 2)),
            WaveAt(28f, Spawn("en1", 3)),
            WaveAt(40f, Spawn("en_brute", 1), Spawn("en1", 2)),
            WaveAt(50f, Spawn("en_runner", 2), Spawn("en1", 1)));

        public static readonly NightWaveBalance Night4 = new(
            60f,
            WaveAt(6f, Spawn("en_runner", 2)),
            WaveAt(18f, Spawn("en1", 3)),
            WaveAt(30f, Spawn("en_brute", 1)),
            WaveAt(42f, Spawn("en1", 2), Spawn("en_runner", 2)),
            WaveAt(52f, Spawn("en1", 2)));

        public static readonly NightWaveBalance Night5 = new(
            72f,
            WaveAt(6f, Spawn("en1", 2)),
            WaveAt(16f, Spawn("en_runner", 2)),
            WaveAt(28f, Spawn("en_brute", 1), Spawn("en1", 2)),
            WaveAt(40f, Spawn("en1", 3), Spawn("en_runner", 1)),
            WaveAt(52f, Spawn("en_brute", 1), Spawn("en_runner", 2)),
            WaveAt(64f, Spawn("en1", 3), Spawn("en_runner", 2)));

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