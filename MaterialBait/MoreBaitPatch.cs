using HarmonyLib;

namespace MaterialBait;

[HarmonyPatch]
public static class MoreBaitPatch
{
    private static bool _deployingMaterialBait;
    private static int _numFishInBaitBallMax;
    private static int _numFishInBaitBallMin;

    // Has to go before Winch can
    [HarmonyPriority(Priority.First)]
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BaitAbility), nameof(BaitAbility.DeployBait))]
    public static void BaitAbility_DeployBait_Prefix(BaitAbility __instance, SpatialItemInstance baitInstance)
    {
        var itemData = baitInstance.GetItemData<SpatialItemData>();
        if (itemData.id == MaterialBait.MaterialBaitItemData.id && !_deployingMaterialBait)
        {
            _deployingMaterialBait = true;
            _numFishInBaitBallMax = GameManager.Instance.GameConfigData.numFishInBaitBallMax;
            _numFishInBaitBallMin = GameManager.Instance.GameConfigData.numFishInBaitBallMin;

            // Buff these values while we are casting our custom bait
            GameManager.Instance.GameConfigData.numFishInBaitBallMax = 8;
            GameManager.Instance.GameConfigData.numFishInBaitBallMin = 6;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BaitAbility), nameof(BaitAbility.DeployBait))]
    public static void BaitAbility_DeployBait_Postfix(BaitAbility __instance, SpatialItemInstance baitInstance)
    {
        if (_deployingMaterialBait)
        {
            _deployingMaterialBait = false;
            GameManager.Instance.GameConfigData.numFishInBaitBallMax = _numFishInBaitBallMax;
            GameManager.Instance.GameConfigData.numFishInBaitBallMin = _numFishInBaitBallMin;
        }
    }
}
