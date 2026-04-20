public enum UpgradePurchaseFailureReason
{
    None = 0,
    InvalidState = 1,
    WrongPhase = 2,
    UpgradeNotFound = 3,
    AlreadyPurchased = 4,
    NotEnoughGold = 5,
    EffectNotSupported = 6,
    UpgradeNotOfferedToday = 7
}

public class UpgradePurchaseResult
{
    public bool IsSuccess { get; }
    public UpgradePurchaseFailureReason FailureReason { get; }
    public UpgradeDef UpgradeDef { get; }

    private UpgradePurchaseResult(bool isSuccess, UpgradePurchaseFailureReason failureReason, UpgradeDef upgradeDef)
    {
        IsSuccess = isSuccess;
        FailureReason = failureReason;
        UpgradeDef = upgradeDef;
    }

    public static UpgradePurchaseResult Success(UpgradeDef upgradeDef)
    {
        return new UpgradePurchaseResult(true, UpgradePurchaseFailureReason.None, upgradeDef);
    }

    public static UpgradePurchaseResult Failure(UpgradePurchaseFailureReason failureReason, UpgradeDef upgradeDef = null)
    {
        return new UpgradePurchaseResult(false, failureReason, upgradeDef);
    }
}
