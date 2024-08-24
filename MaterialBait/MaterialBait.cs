using HarmonyLib;

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
}