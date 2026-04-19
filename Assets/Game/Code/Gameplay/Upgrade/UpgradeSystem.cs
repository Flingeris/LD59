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

        if (runState.PurchasedUpgradeIds.Contains(upgradeDef.Id))
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
        runState.PurchasedUpgradeIds.Add(upgradeDef.Id);
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
            || effectType == UpgradeEffectType.KeeperMoveSpeedBonus;
    }

    private static void ApplyEffect(UpgradeDef upgradeDef, RunState runState)
    {
        var effectValue = Mathf.Max(0, upgradeDef.EffectValue);

        switch (upgradeDef.EffectType)
        {
            case UpgradeEffectType.FaithIncomeBonus:
                runState.FaithCollectionPayoutAmount = Mathf.Max(0, runState.FaithCollectionPayoutAmount + effectValue);
                break;

            case UpgradeEffectType.CemeteryRepair:
                runState.CemeteryState = Mathf.Clamp(
                    runState.CemeteryState + effectValue,
                    0,
                    runState.CemeteryMaxState);
                break;

            case UpgradeEffectType.CemeteryMaxStateBonus:
                runState.CemeteryMaxState = Mathf.Max(0, runState.CemeteryMaxState + effectValue);
                runState.CemeteryState = Mathf.Clamp(
                    runState.CemeteryState + effectValue,
                    0,
                    runState.CemeteryMaxState);
                break;

            case UpgradeEffectType.BellFaithCostModifier:
                runState.BellFaithCostModifier -= effectValue;
                break;

            case UpgradeEffectType.StartingNightFaithBonus:
                runState.StartingNightFaith = Mathf.Max(0, runState.StartingNightFaith + effectValue);
                break;

            case UpgradeEffectType.KeeperMoveSpeedBonus:
                if (runState.Keeper != null)
                {
                    runState.Keeper.MoveSpeed = Mathf.Max(0f, runState.Keeper.MoveSpeed + effectValue);
                }

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
        if (runState.Keeper != null)
        {
            runState.Keeper.MoveSpeed = Mathf.Max(0f, runState.Keeper.MoveSpeed);
        }
    }
}
