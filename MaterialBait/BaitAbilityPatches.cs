using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Winch.Util;

namespace MaterialBait;

[HarmonyPatch]
public static class BaitAbilityPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BaitAbility), nameof(BaitAbility.Init))]
    private static void BaitAbility_Init(BaitAbility __instance)
    {
        // Register our custom bait as bait
        __instance.baitItems.Add(MaterialBait.MaterialBaitItemData);
        __instance.RefreshItemCyclingCollection();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerAbilityManager), nameof(PlayerAbilityManager.GetHasDependantItems))]
    public static void PlayerAbilityManager_GetHasDependentItems(PlayerAbilityManager __instance, AbilityData ability)
    {
        // Things are readonly so have to do this rather poorly
        // This makes the ability actually activate when we have only this bait in our inventory
        if (ability?.name == "bait")
        {
            if (!ability.linkedItems.Contains(MaterialBait.MaterialBaitItemData))
            {
                ability.linkedItems = ability.linkedItems.Add(MaterialBait.MaterialBaitItemData);
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BaitAbility), nameof(BaitAbility.DeployBait))]
    public static bool BaitAbility_DeployBait(BaitAbility __instance, SpatialItemInstance baitInstance)
    {
        if (baitInstance?.id == MaterialBait.MaterialBaitItemData.id)
        {
            // Spawn our custom bait POI
            SpawnCustomBait(__instance, baitInstance);

            return false;
        }
        else
        {
            return true;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BaitAbility), nameof(BaitAbility.GetFishForBait))]
    public static bool BaitAbility_GetFishForBait(BaitAbility __instance, SpatialItemData spatialItemData, ref List<FishItemData> __result)
    {
        if (spatialItemData?.id == MaterialBait.MaterialBaitItemData.id)
        {
            // Trick it into thinking there's fish so it allows us to use our custom bait
            __result = new List<FishItemData>() { new FishItemData() };

            return false;
        }
        else
        {
            return true;
        }
    }

    private static void SpawnCustomBait(BaitAbility baitAbility, SpatialItemInstance baitInstance)
    {
        BaitPOIDataModel baitPOIDataModel = new BaitPOIDataModel();
        SpatialItemData itemData = baitInstance.GetItemData<SpatialItemData>();
        baitPOIDataModel.doesRestock = false;
        // Instead of fish, get dredgable items
        List<HarvestableItemData> list = ItemUtil.HarvestableItemDataDict.Values
            .Where(x => x.harvestableType == HarvestableType.DREDGE && (x.itemSubtype == ItemSubtype.TRINKET || x.itemSubtype == ItemSubtype.MATERIAL))
            .OrderBy(_ => Guid.NewGuid()).ToList();
        if (list.Count == 0)
        {
            GameManager.Instance.UI.ShowNotification(NotificationType.ERROR, "notification.bait-failed");
            return;
        }

        // Double it because uh I wanna!
        int num = 2 * UnityEngine.Random.Range(GameManager.Instance.GameConfigData.NumFishInBaitBallMin, GameManager.Instance.GameConfigData.NumFishInBaitBallMax);

        int num2 = 0;
        Stack<HarvestableItemData> stack = new Stack<HarvestableItemData>();
        for (int i = 0; i < num; i++)
        {
            stack.Push(list[num2 % list.Count]);
            num2++;
        }
        baitPOIDataModel.itemStock = stack;
        baitPOIDataModel.startStock = (float)stack.Count;
        baitPOIDataModel.maxStock = baitPOIDataModel.startStock;
        baitPOIDataModel.usesTimeSpecificStock = false;
        Vector3 position = new Vector3(GameManager.Instance.Player.BoatModelProxy.DeployPosition.position.x, 0f, GameManager.Instance.Player.BoatModelProxy.DeployPosition.position.z);
        GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(baitAbility.baitPOIPrefab, position, Quaternion.identity, GameSceneInitializer.Instance.HarvestPoiContainer.transform);

        // Modify the gameobject to have dredging particles under the surface not fish
        var prefab = GameObject.FindObjectsOfType<HarvestPOI>(true).First(x => x.name.Contains("scrap")).transform.Find("MetalScrapParticles(Clone)").gameObject;
        
        gameObject.transform.eulerAngles = new Vector3(0f, GameManager.Instance.Player.BoatModelProxy.DeployPosition.eulerAngles.y, 0f);
        HarvestPOI component = gameObject.GetComponent<HarvestPOI>();
        if (component)
        {
            component.harvestParticlePrefab = prefab;
            component.Harvestable = baitPOIDataModel;
            component.HarvestPOIData = baitPOIDataModel;
            Cullable component2 = component.GetComponent<Cullable>();
            if (component2)
            {
                GameManager.Instance.CullingBrain.AddCullable(component2);
            }
        }

        GameManager.Instance.SaveData.Inventory.RemoveObjectFromGridData(baitInstance, true);
        GameEvents.Instance.TriggerItemInventoryChanged(baitAbility.currentlySelectedItem);
    }
}
