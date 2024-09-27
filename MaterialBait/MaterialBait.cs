using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Winch.Core;
using Winch.Util;

namespace MaterialBait;

public class MaterialBait
{
    public static ItemRecipeData BaitRecipeData { get; private set; }
    public static SpatialItemData MaterialBaitItemData => ItemUtil.GetModdedItemData("xen-42.MaterialBait.bait") as SpatialItemData;

    // Ingredients to make our bait
    private static SpatialItemData _bait, _darkSplash;

    private static QuestGridConfig _crabBaitRecipeQuestGrid;

    /// <summary>
    /// This method is run by Winch to initialize your mod
    /// </summary>
    public static void Initialize()
    {
        GameManager.Instance.OnGameStarted += () =>
        {
#if DEBUG
            if (!GameManager.Instance.SaveData.Inventory.GetAllItemsOfType<SpatialItemInstance>(ItemType.GENERAL)
                .Any(x => x._itemData is SpatialItemData spatialData && spatialData.id == MaterialBaitItemData.id))
            {
                GameManager.Instance.ItemManager.AddItemById(MaterialBaitItemData.id, GameManager.Instance.SaveData.Inventory, true);
            }
#endif

            SetUpRecipeInFactory();
        };

        Winch.Core.API.DredgeEvent.AddressableEvents.ItemsLoaded.On += OnItemsLoaded;
        Winch.Core.API.DredgeEvent.AddressableEvents.QuestGridConfigsLoaded.On += OnQuestGridConfigsLoaded;

        new Harmony("MaterialBait").PatchAll();
    }

    private static void OnQuestGridConfigsLoaded(object sender, Winch.Core.API.Events.Addressables.AddressablesLoadedEventArgs<QuestGridConfig> e)
    {
        if (_crabBaitRecipeQuestGrid != null)
        {
            return;
        }

        foreach (var item in e.Handle.Result)
        {
            if (item.gridKey == GridKey.TIR_BAIT_CRAB_RECIPE)
            {
                _crabBaitRecipeQuestGrid = item;
                break;
            }
        }

        TryCreateRecipe();
    }

    private static void OnItemsLoaded(object sender, Winch.Core.API.Events.Addressables.AddressablesLoadedEventArgs<ItemData> e)
    {
        if (_bait != null && _darkSplash != null)
        {
            return;
        }

        foreach (var item in e.Handle.Result)
        {
            if (item is SpatialItemData spatialItem)
            {
                if (spatialItem.id == "bait")
                {
                    _bait = spatialItem;
                    if (_darkSplash != null) break;
                }
                else if (spatialItem.id == "dark-splash")
                {
                    _darkSplash = spatialItem;
                    if (_bait != null) break;
                }
            }
        }

        TryCreateRecipe();
    }

    private static void TryCreateRecipe()
    {
        // Once both ingredients have loaded we can make our recipe
        if (_bait != null && _darkSplash != null && _crabBaitRecipeQuestGrid != null)
        {
            try
            {
                CreateRecipe();
            }
            catch (Exception ex)
            {
                WinchCore.Log.Error(ex);
            }
        }
    }

    private static void CreateRecipe()
    {
        BaitRecipeData = ScriptableObject.CreateInstance<ItemRecipeData>();
        UnityEngine.Object.DontDestroyOnLoad(BaitRecipeData);

        var questGridConfig = ScriptableObject.CreateInstance<QuestGridConfig>();
        UnityEngine.Object.DontDestroyOnLoad(questGridConfig);

        BaitRecipeData.itemProduced = MaterialBaitItemData;
        BaitRecipeData.name = "Recipe_BaitMaterial";
        BaitRecipeData.onRecipeBuiltDialogueNodeName = "Factory_Item_Constructed";
        BaitRecipeData.onRecipeShownDialogueNodeName = "Factory_Item_RecipeShown";
        BaitRecipeData.quantityProduced = 1;
        BaitRecipeData.recipeId = "bait-mb-recipe";
        BaitRecipeData.cost = 40;
        BaitRecipeData.questGridConfig = questGridConfig;

        // Quest Grid
        questGridConfig.name = "Item_BaitMaterial";
        questGridConfig.allowEquipmentInstallation = true;
        questGridConfig.allowManualExit = true;
        questGridConfig.allowStorageAccess = false;
        questGridConfig.completeConditions = new List<CompletedGridCondition>()
        {
            new ItemCountCondition() { item = _bait, count = 1 },
            new ItemCountCondition() { item = _darkSplash, count = 2 }
        };

        questGridConfig.createItemsIfEmpty = false;
        questGridConfig.presetGridMode = PresetGridMode.SILHOUETTE;
        questGridConfig.isSaved = true;
        questGridConfig.gridKey = Enums.BAIT_MATERIAL_RECIPE;
        questGridConfig.presetGrid = new SerializableGrid();
        questGridConfig.presetGrid.spatialItems = new List<SpatialItemInstance>
        {
            new SpatialItemInstance() { x = 0, y = 0, id = _bait.id },
            new SpatialItemInstance() { x = 1, y = 0, id = _darkSplash.id },
            new SpatialItemInstance() { x = 1, y = 1, id = _darkSplash.id },
        };

        //questGridConfig.exitPromptOverride = _crabBaitRecipeQuestGrid.exitPromptOverride;
        //questGridConfig.helpStringOverride = _crabBaitRecipeQuestGrid.helpStringOverride;
        //questGridConfig.titleString = _crabBaitRecipeQuestGrid.titleString;

        var gridConfiguration = ScriptableObject.CreateInstance<GridConfiguration>();
        UnityEngine.Object.DontDestroyOnLoad(gridConfiguration);

        questGridConfig.gridConfiguration = gridConfiguration;
        // Grid configuration
        gridConfiguration.canAddItemsInQuestMode = true;
        gridConfiguration.MainItemSubtype = ItemSubtype.GENERAL | ItemSubtype.MATERIAL;
        gridConfiguration.MainItemType = ItemType.GENERAL;
        gridConfiguration.name = "Recipe_BaitMaterial";
        gridConfiguration.columns = 2;
        gridConfiguration.rows = 2;

        GameManager.Instance.GameConfigData.gridConfigs[questGridConfig.gridKey] = gridConfiguration;
    }

    private static void SetUpRecipeInFactory()
    {
        try
        {
            // Add our custom recipe to the Factory on the Iron Rig
            var tier4Factory = GameObject.FindObjectsOfType<ConstructableDestination>().First(x => x.name == "Factory")
                .constructableDestinationData.tiers[3] as RecipeListDestinationTier;
            tier4Factory.recipes.Add(BaitRecipeData);
        }
        catch (Exception ex)
        {
            WinchCore.Log.Error(ex);
        }
    }
}