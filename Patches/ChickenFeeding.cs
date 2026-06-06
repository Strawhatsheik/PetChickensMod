using HarmonyLib;
using System.Diagnostics;
using UnityEngine;


[HarmonyPatch(typeof(EntityAnimal))]
class Patch_ChickenFeeding
{
    [HarmonyPatch("UpdateAITasks")]
    [HarmonyPostfix]
    static void CheckForFood(EntityAnimal __instance)
    {
        if (__instance.EntityName != "Pet Chicken") return;

        Vector3 pos = __instance.position;
        bool foundFood = false;

        // Scan nearby blocks (5x5) for a chicken trough
        for (int x = -5; x <= 5; x++)
        {
            for (int z = -5; z <= 5; z++)
            {
                BlockValue block = GameManager.Instance.World.GetBlock((int)pos.x + x, (int)pos.y, (int)pos.z + z);
                if (block.Block.GetBlockName() == "chickenTrough")
                {
                    int corn = block.Buffs.GetCustomVar("CornMeal"); // check custom var on block
                    if (corn > 0)
                    {
                        foundFood = true;
                        block.Buffs.SetCustomVar("CornMeal", corn - 1);
                        __instance.SetCustomVar("Hunger", 1.0f); // reset hunger
                    }
                }
            }
        }

        if (!foundFood)
        {
            float hunger = __instance.GetCustomVar("Hunger");
            hunger = Mathf.Max(0, hunger - 0.01f); // slowly increase hunger
            __instance.SetCustomVar("Hunger", hunger);
        }
    }
}
[HarmonyPatch(typeof(EntityAnimal))]