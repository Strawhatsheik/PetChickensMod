using UnityEngine;
using HarmonyLib;

namespace PetChickensMod
{
    [HarmonyPatch(typeof(EntityAnimal))]
    public class Patch_ChickenScavenging
    {
        [HarmonyPatch("UpdateAITasks")]
        [HarmonyPostfix]
        static void CheckForGrass(EntityAnimal __instance)
        {
            if (__instance.EntityName != "Pet Chicken") return;

            int grassCount = 0;
            Vector3 pos = __instance.position;

            for (int x = -5; x <= 5; x++)
            {
                for (int z = -5; z <= 5; z++)
                {
                    BlockValue block = GameManager.Instance.World.GetBlock((int)pos.x + x, (int)pos.y, (int)pos.z + z);
                    if (block.Block.GetBlockName() == "terrainGrass")
                    {
                        grassCount++;
                    }
                }
            }

            if (grassCount >= 10)
            {
                __instance.SetCustomVar("Hunger", 0.5f);
            }
        }
    }
}
