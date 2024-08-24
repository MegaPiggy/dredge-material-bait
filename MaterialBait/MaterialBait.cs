using HarmonyLib;
using Winch.Core.API.Events.Addressables;

namespace MaterialBait;

public class MaterialBait
{
    public const string MATERIAL_BAIT_ID = "xen-42.MaterialBait.bait";

    /// <summary>
    /// This method is run by Winch to initialize your mod
    /// </summary>
    public static void Initialize()
    {
#if DEBUG
        GameManager.Instance.OnGameStarted += () =>
        {
            GameManager.Instance.ItemManager.AddItemById(MATERIAL_BAIT_ID, GameManager.Instance.SaveData.Inventory, true);
        };
#endif

        new Harmony("MaterialBait").PatchAll();
    }

    private static void OnItemsLoaded(object sender, AddressablesLoadedEventArgs<ItemData> e)
    {
        foreach (ItemData item in e.Handle.Result)
        {
            if (item is SpatialItemData spatialItem && spatialItem.id == "bait")
            {
                spatialItem.hasSellOverride = true;
                spatialItem.sellOverrideValue = 10000000;
                break;
            }
        }
    }
}