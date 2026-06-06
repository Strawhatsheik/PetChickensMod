using UnityEngine;
using HarmonyLib;

namespace PetChickensMod
{
    [HarmonyPatch(typeof(EntityAnimal))]
    public class Patch_ChickenFeeding
    {
        [HarmonyPatch("UpdateAITasks")]
        [HarmonyPostfix]
        static void CheckForFood(EntityAnimal __instance)
        {
            if (__instance.EntityName != "Pet Chicken") return;

            Vector3 pos = __instance.position;
            bool foundFood = false;
            World world = GameManager.Instance.World;

            for (int x = -5; x <= 5; x++)
            {
                for (int z = -5; z <= 5; z++)
                {
                    int bx = (int)pos.x + x;
                    int bz = (int)pos.z + z;
                    BlockValue block = world.GetBlock(bx, (int)pos.y, bz);
                    if (block.Block.GetBlockName() == "chickenTrough")
                    {
                        TileEntity te = world.GetTileEntity(0, new Vector3i(bx, (int)pos.y, bz));
                        TileEntityLootContainer loot = te as TileEntityLootContainer;
                        if (loot != null)
                        {
                            float corn = loot.GetCustomVar("CornMeal");
                            if (corn > 0)
                            {
                                foundFood = true;
                                loot.SetCustomVar("CornMeal", corn - 1);
                                __instance.SetCustomVar("Hunger", 1.0f);
                            }
                        }
                    }
                }
            }

            if (!foundFood)
            {
                float hunger = __instance.GetCustomVar("Hunger");
                hunger = Mathf.Max(0, hunger - 0.01f);
                __instance.SetCustomVar("Hunger", hunger);
            }
        }
    }
}
