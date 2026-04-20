using System.Collections.Generic;
using UnityEngine;

public class UpgradeSystem
{
    public UpgradePurchaseResult TryPurchaseUpgrade(string upgradeId, RunState runState)
    {
        if (runState == null)
        {
            return UpgradePurchaseResult.Failure(UpgradePurchaseFailureReason.InvalidState);
        }

        var upgradeDef = CMS.Get<UpgradeDef>(upgradeId);
        if (upgradeDef == null)
        {
            return UpgradePurchaseResult.Failure(UpgradePurchaseFailureReason.UpgradeNotFound);
        }

        runState.PurchasedUpgradeIds ??= new HashSet<string>();

        if (!upgradeDef.IsRepeatable && runState.PurchasedUpgradeIds.Contains(upgradeDef.Id))
        {
            return UpgradePurchaseResult.Failure(UpgradePurchaseFailureReason.AlreadyPurchased, upgradeDef);
        }

        if (!SupportsEffectType(upgradeDef.EffectType))
        {
            return UpgradePurchaseResult.Failure(UpgradePurchaseFailureReason.EffectNotSupported, upgradeDef);
        }

        var price = Mathf.Max(0, upgradeDef.Price);
        if (runState.Gold < price)
        {
            return UpgradePurchaseResult.Failure(UpgradePurchaseFailureReason.NotEnoughGold, upgradeDef);
        }

        runState.Gold -= price;
        if (!upgradeDef.IsRepeatable)
        {
            runState.PurchasedUpgradeIds.Add(upgradeDef.Id);
        }

        ApplyEffect(upgradeDef, runState);
        ClampRunState(runState);

        return UpgradePurchaseResult.Success(upgradeDef);
    }

    public static bool SupportsEffectType(UpgradeEffectType effectType)
    {
        return effectType == UpgradeEffectType.FaithIncomeBonus
            || effectType == UpgradeEffectType.CemeteryRepair
            || effectType == UpgradeEffectType.CemeteryMaxStateBonus
            || effectType == UpgradeEffectType.BellFaithCostModifier
            || effectType == UpgradeEffectType.StartingNightFaithBonus
            || effectType == UpgradeEffectType.KeeperMoveSpeedBonus
            || effectType == UpgradeEffectType.UnitDamageModifier
            || effectType == UpgradeEffectType.UnitLifetimeModifier
            || effectType == UpgradeEffectType.UnitHpModifier
            || effectType == UpgradeEffectType.FaithCollectionIntervalModifier
            || effectType == UpgradeEffectType.NightInstantRepairCharge;
    }

    private static void ApplyEffect(UpgradeDef upgradeDef, RunState runState)
    {
        var effectValue = Mathf.Max(0f, upgradeDef.EffectValue);
        var effectIntValue = Mathf.RoundToInt(effectValue);

        switch (upgradeDef.EffectType)
        {
            case UpgradeEffectType.FaithIncomeBonus:
                runState.FaithCollectionPayoutAmount =
                    Mathf.Max(0, runState.FaithCollectionPayoutAmount + effectIntValue);
                break;

            case UpgradeEffectType.CemeteryRepair:
                runState.CemeteryState = Mathf.Clamp(
                    runState.CemeteryState + effectIntValue,
                    0,
                    runState.CemeteryMaxState);
                break;

            case UpgradeEffectType.CemeteryMaxStateBonus:
                runState.CemeteryMaxState = Mathf.Max(0, runState.CemeteryMaxState + effectIntValue);
                runState.CemeteryState = Mathf.Clamp(
                    runState.CemeteryState + effectIntValue,
                    0,
                    runState.CemeteryMaxState);
                break;

            case UpgradeEffectType.BellFaithCostModifier:
                runState.BellFaithCostModifier -= effectIntValue;
                break;

            case UpgradeEffectType.StartingNightFaithBonus:
                runState.StartingNightFaith = Mathf.Max(0, runState.StartingNightFaith + effectIntValue);
                break;

            case UpgradeEffectType.KeeperMoveSpeedBonus:
                if (runState.Keeper != null)
                {
                    runState.Keeper.MoveSpeed = Mathf.Max(0f, runState.Keeper.MoveSpeed + effectValue);
                }

                break;

            case UpgradeEffectType.UnitDamageModifier:
                runState.AddUnitDamageModifier(upgradeDef.TargetUnitId, effectIntValue);
                break;

            case UpgradeEffectType.UnitLifetimeModifier:
                runState.AddUnitLifetimeModifier(upgradeDef.TargetUnitId, effectValue);
                break;

            case UpgradeEffectType.UnitHpModifier:
                runState.AddUnitHpModifier(upgradeDef.TargetUnitId, effectIntValue);
                break;

            case UpgradeEffectType.FaithCollectionIntervalModifier:
                runState.FaithCollectionIntervalSeconds =
                    Mathf.Max(0f, runState.FaithCollectionIntervalSeconds - effectValue);
                break;

            case UpgradeEffectType.NightInstantRepairCharge:
                runState.InstantNightRepairChargesPerNight =
                    Mathf.Max(0, runState.InstantNightRepairChargesPerNight + 1);
                runState.InstantNightRepairAmount = Mathf.Max(0, runState.InstantNightRepairAmount + effectIntValue);
                break;
        }
    }

    private static void ClampRunState(RunState runState)
    {
        runState.Gold = Mathf.Max(0, runState.Gold);
        runState.StartingNightFaith = Mathf.Max(0, runState.StartingNightFaith);
        runState.FaithCollectionPayoutAmount = Mathf.Max(0, runState.FaithCollectionPayoutAmount);
        runState.FaithCollectionIntervalSeconds = Mathf.Max(0f, runState.FaithCollectionIntervalSeconds);
        runState.FaithCollectionTimerProgress = Mathf.Max(0f, runState.FaithCollectionTimerProgress);
        runState.CemeteryMaxState = Mathf.Max(0, runState.CemeteryMaxState);
        runState.CemeteryState = Mathf.Clamp(runState.CemeteryState, 0, runState.CemeteryMaxState);
        runState.InstantNightRepairChargesPerNight = Mathf.Max(0, runState.InstantNightRepairChargesPerNight);
        runState.RemainingInstantNightRepairCharges = Mathf.Clamp(
            runState.RemainingInstantNightRepairCharges,
            0,
            runState.InstantNightRepairChargesPerNight);
        runState.InstantNightRepairAmount = Mathf.Max(0, runState.InstantNightRepairAmount);
        if (runState.Keeper != null)
        {
            runState.Keeper.MoveSpeed = Mathf.Max(0f, runState.Keeper.MoveSpeed);
        }
    }
}
