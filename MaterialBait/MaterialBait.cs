using HarmonyLib;
using System;
using System.Linq;
using Winch.Core;
using Winch.Util;

namespace MaterialBait;

public class MaterialBait
{
    public static ItemRecipeData BaitRecipeData { get; private set; }
    public static SpatialItemData MaterialBaitItemData => ItemUtil.GetSpatialItemData("xen-42.MaterialBait.bait");

    /// <summary>
    /// This method is run by Winch to initialize your mod
    /// </summary>
    public static void Initialize()
    {
        GameManager.Instance.OnGameStarted += OnGameStarted;
        ApplicationEvents.Instance.OnGameLoaded += OnGameLoaded;

        new Harmony("MaterialBait").PatchAll();
    }

    private static void OnGameStarted()
    {
#if DEBUG
        if (!GameManager.Instance.SaveData.Inventory.GetAllItemsOfType<SpatialItemInstance>(ItemType.GENERAL)
            .Any(x => x._itemData is SpatialItemData spatialData && spatialData.id == MaterialBaitItemData.id))
        {
            GameManager.Instance.ItemManager.AddItemById(MaterialBaitItemData.id, GameManager.Instance.SaveData.Inventory, true);
        }
#endif
    }

    private static void OnGameLoaded()
    {
        SetUpRecipeInFactory();
    }

    private static void SetUpRecipeInFactory()
    {
        try
        {
            // Add our custom recipe to the Factory on the Iron Rig
            var factory = DockUtil.GetConstructableDestinationData("destination.tir-factory");
            var tier4Factory = factory.GetRecipeListTier(BuildingTierId.FACTORY_TIER_4);
            tier4Factory.recipes.Add(RecipeUtil.GetItemRecipeData("xen-42.MaterialBait.bait-recipe"));
        }
        catch (Exception ex)
        {
            WinchCore.Log.Error(ex);
        }
    }
}