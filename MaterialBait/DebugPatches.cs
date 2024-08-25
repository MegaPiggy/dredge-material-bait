using DG.Tweening;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Winch.Core;

namespace MaterialBait;

[HarmonyPatch]
public static class DebugPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SerializableGrid), nameof(SerializableGrid.GetCellsAffectedByObjectAtPosition))]
    private static bool Test(SerializableGrid __instance, List<Vector2Int> dimensions, Vector3Int pos, ref List<GridCellData> __result)
    {
        try
        {
            if (__instance.cellsAffectedByQuery == null)
            {
                __instance.cellsAffectedByQuery = new List<GridCellData>();
            }
            else
            {
                __instance.cellsAffectedByQuery.Clear();
            }
            dimensions.ForEach((Vector2Int offset) =>
            {
                try
                {
                    int num = pos.x + offset.x;
                    int num2 = pos.y + offset.y;
                    if (pos.z == 90)
                    {
                        num = pos.x + offset.y;
                        num2 = pos.y - offset.x;
                    }
                    else if (pos.z == 180)
                    {
                        num = pos.x - offset.x;
                        num2 = pos.y - offset.y;
                    }
                    else if (pos.z == 270)
                    {
                        num = pos.x - offset.y;
                        num2 = pos.y + offset.x;
                    }
                    if (num >= 0 && num < __instance.gridConfiguration.columns && num2 >= 0 && num2 < __instance.gridConfiguration.rows && __instance.grid[num, num2] != null)
                    {
                        __instance.cellsAffectedByQuery.Add(__instance.grid[num, num2]);
                    }
                }
                catch (Exception ex)
                {
                    WinchCore.Log.Error(ex);
                }
            });
            __result = __instance.cellsAffectedByQuery;
        }
        catch (Exception e)
        {
            WinchCore.Log.Error(e);
            __result = new();
        }

        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(RecipeEntry), nameof(RecipeEntry.Init))]
    private static bool Test2(RecipeEntry __instance, RecipeData recipeData)
    {
        try
        {
            __instance.recipeData = recipeData;
            foreach (Transform transform in __instance.researchNotchContainer.Cast<Transform>().ToList<Transform>())
            {
                UnityEngine.Object.DestroyImmediate(transform.gameObject);
            }
            __instance.researchNotches = new List<ResearchNotch>();
            for (int i = 0; i < recipeData.researchRequired; i++)
            {
                ResearchNotch component = UnityEngine.Object.Instantiate<GameObject>(__instance.researchNotchPrefab, __instance.researchNotchContainer).GetComponent<ResearchNotch>();
                ResearchNotch researchNotch = component;
                researchNotch.OnAnimationCompleteAction = (Action)Delegate.Combine(researchNotch.OnAnimationCompleteAction, new Action(__instance.RefreshUI));
                __instance.researchNotches.Add(component);
            }
            __instance.mainImage.sprite = recipeData.GetSprite();
            __instance.targetSize = new Vector2(__instance.pixelsPerSquare * (float)recipeData.GetWidth(), __instance.pixelsPerSquare * (float)recipeData.GetHeight());
            __instance.mainImageContainer.sizeDelta = __instance.targetSize;
            if (recipeData is ItemRecipeData)
            {
                __instance.spatialItemTooltipRequester.enabled = true;
                __instance.spatialItemTooltipRequester.TooltipMode = __instance.tooltipMode;
                __instance.spatialItemTooltipRequester.SpatialItemData = (recipeData as ItemRecipeData).itemProduced;
                __instance.spatialItemTooltipRequester.RecipeData = recipeData;
                __instance.abilityTooltipRequester.enabled = false;
                __instance.upgradeTooltipRequester.enabled = false;
            }
            else if (recipeData is AbilityRecipeData)
            {
                __instance.spatialItemTooltipRequester.enabled = false;
                __instance.upgradeTooltipRequester.enabled = false;
                __instance.abilityTooltipRequester.enabled = true;
                __instance.abilityTooltipRequester.abilityData = (recipeData as AbilityRecipeData).abilityData;
                __instance.abilityTooltipRequester.RecipeData = recipeData;
            }
            __instance.canvasGroup.alpha = 0f;
            __instance.RefreshUI();
        }
        catch (Exception e)
        {
            WinchCore.Log.Error(e);
        }

        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(RecipeGridPanel), nameof(RecipeGridPanel.Show))]
    private static bool Test3(RecipeGridPanel __instance, RecipeData recipeData, bool exitOnFulfilled)
    {
        try
        {
            Debug.Log(string.Format("[RecipeGridPanel] Show({0})", recipeData));
            __instance.currentRecipe = recipeData;
            __instance.result = QuestGridResult.INCOMPLETE;
            __instance.currentQuestGridConfig = __instance.currentRecipe.questGridConfig;
            if (__instance.currentRecipe.cost > 0m)
            {
                __instance.bottomButton.LocalizedString.StringReference = __instance.bottomButtomStringWithCost;
            }
            else
            {
                __instance.bottomButton.LocalizedString.StringReference = __instance.bottomButtomStringFree;
            }
            __instance.bottomButton.LocalizedString.RefreshString();
            __instance.OnPlayerFundsChanged(GameManager.Instance.SaveData.Funds, 0m);
            __instance.currentGrid = null;
            __instance.currentGrid = GameManager.Instance.SaveData.GetGridByKey(__instance.currentQuestGridConfig.gridKey);
            if (__instance.currentGrid == null)
            {
                __instance.currentGrid = __instance.CreateGridWithConfig(__instance.currentQuestGridConfig, true);
            }
            GameManager.Instance.Player.CanMoveInstalledItems = __instance.currentQuestGridConfig.allowEquipmentInstallation;
            __instance.gridUI.OverrideGridCellColor = __instance.currentQuestGridConfig.overrideGridCellColor;
            __instance.gridUI.GridCellColor = __instance.currentQuestGridConfig.gridCellColor;
            __instance.gridUI.SetLinkedGrid(__instance.currentGrid);
            __instance.container.SetActive(false);
            __instance.container.SetActive(true);
            __instance.gridBackgroundTransform.DOSizeDelta(__instance.gridSize, __instance.gridExpandDurationSec, false).SetDelay(__instance.gridExpandDelaySec).SetEase(__instance.gridExpandEase).From(new Vector2(__instance.gridSize.x, 0f), true, false);
            if (__instance.currentRecipe is BuildingRecipeData)
            {
                __instance.buildingUI.Init(__instance.currentRecipe as BuildingRecipeData);
            }
            else if (__instance.currentRecipe != null)
            {
                __instance.itemUI.Init(__instance.currentRecipe);
            }
            if (__instance.currentQuestGridConfig.presetGridMode == PresetGridMode.SILHOUETTE || __instance.currentQuestGridConfig.presetGridMode == PresetGridMode.MYSTERY)
            {
                __instance.gridUI.ShowGridHints(__instance.currentQuestGridConfig.presetGrid.spatialItems, __instance.currentQuestGridConfig.presetGridMode);
            }
            __instance.OnStateChanged();
        }
        catch (Exception e)
        {
            WinchCore.Log.Error(e);
        }

        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(GridUI), nameof(GridUI.GenerateGrid))]
    private static bool Test4(GridUI __instance)
    {
        try
        {
            Debug.Log("[GridUI: " + __instance.name + "] GenerateGrid()");
            __instance.underlayContainer.gameObject.SetActive(__instance.linkedGrid.GridConfiguration.HasUnderlay);
            __instance.cellSize = GameManager.Instance.GridManager.cellSize;
            __instance.allCells = new GridCell[__instance.linkedGrid.GridConfiguration.columns, __instance.linkedGrid.GridConfiguration.rows];
            int num = 0;
            if (__instance.LinkedGridKey == GridKey.INVENTORY)
            {
                num = -1;
            }
            RectTransform component = __instance.GetComponent<RectTransform>();
            __instance.originPos = new Vector3(component.rect.width * 0.5f - (float)__instance.linkedGrid.GridConfiguration.columns * 0.5f * __instance.cellSize, -(component.rect.height * 0.5f) + ((float)__instance.linkedGrid.GridConfiguration.rows + (float)num) * 0.5f * __instance.cellSize, 0f);
            Color regularColor = Color.white;
            if (__instance.overrideGridCellColor)
            {
                regularColor = GameManager.Instance.LanguageManager.GetColor(__instance.gridCellColor);
            }
            GridCellData[,] grid = __instance.linkedGrid.Grid;
            int upperBound = grid.GetUpperBound(0);
            int upperBound2 = grid.GetUpperBound(1);
            for (int i = grid.GetLowerBound(0); i <= upperBound; i++)
            {
                for (int j = grid.GetLowerBound(1); j <= upperBound2; j++)
                {
                    GridCellData gridCellData = grid[i, j];
                    GameObject gameObject = __instance.gridCellPrefab.Spawn(__instance.cellContainer);
                    (gameObject.transform as RectTransform).anchoredPosition = new Vector2(__instance.originPos.x + (float)gridCellData.x * __instance.cellSize, __instance.originPos.y - (float)gridCellData.y * __instance.cellSize);
                    (gameObject.transform as RectTransform).sizeDelta = new Vector2(__instance.cellSize, __instance.cellSize);
                    GridCell component2 = gameObject.GetComponent<GridCell>();
                    __instance.allCells[gridCellData.x, gridCellData.y] = component2;
                    if (__instance.overrideGridCellColor)
                    {
                        component2.regularColor = regularColor;
                    }
                    component2.Init(gridCellData, string.Format("[{0}, {1}]", gridCellData.x, gridCellData.y), __instance, __instance.defaultGridObjectState, gridCellData.acceptedItemType, gridCellData.acceptedItemSubtype);
                }
            }
            __instance.CreateObjects();
        }
        catch (Exception e)
        {
            WinchCore.Log.Error(e);
        }

        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ItemProductPanel), nameof(ItemProductPanel.Show))]
    private static bool Test5(ItemProductPanel __instance)
    {
        try
        {
            Debug.Log("[ItemProductPanel] Show()");
            __instance.result = QuestGridResult.INCOMPLETE;
            GameManager.Instance.Player.CanMoveInstalledItems = __instance.questGridConfig.allowEquipmentInstallation;
            __instance.gridUI.OverrideGridCellColor = __instance.questGridConfig.overrideGridCellColor;
            __instance.gridUI.GridCellColor = __instance.questGridConfig.gridCellColor;
            __instance.gridUI.SetLinkedGrid(__instance.currentGrid);
            __instance.container.SetActive(false);
            __instance.container.SetActive(true);
            if (__instance.questGridConfig.presetGridMode == PresetGridMode.SILHOUETTE || __instance.questGridConfig.presetGridMode == PresetGridMode.MYSTERY)
            {
                __instance.gridUI.ShowGridHints(__instance.questGridConfig.presetGrid.spatialItems, __instance.questGridConfig.presetGridMode);
            }
        }
        catch (Exception e)
        {
            WinchCore.Log.Error(e);
        }

        return false;
    }
}
