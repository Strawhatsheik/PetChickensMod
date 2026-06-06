using UnityEngine;
using HarmonyLib;

namespace PetChickensMod
{
    [HarmonyPatch(typeof(EntityAnimal))]
    public class Patch_ChickenAI
    {
        private static readonly float MaxRadius = 10f;
        private static readonly float NightTime = 19000f;

        [HarmonyPatch("UpdateAITasks")]
        [HarmonyPostfix]
        static void ManageNestBehavior(EntityAnimal __instance)
        {
            if (__instance.EntityName != "Pet Chicken") return;

            if (!__instance.HasCustomVar("NestX"))
            {
                Vector3 pos = __instance.position;
                bool found = false;
                for (int dx = -10; dx <= 10 && !found; dx++)
                {
                    for (int dy = -2; dy <= 2 && !found; dy++)
                    {
                        for (int dz = -10; dz <= 10 && !found; dz++)
                        {
                            int bx = (int)pos.x + dx;
                            int by = (int)pos.y + dy;
                            int bz = (int)pos.z + dz;
                            BlockValue bv = GameManager.Instance.World.GetBlock(bx, by, bz);
                            if (bv.Block.GetBlockName() == "chickenNest")
                            {
                                __instance.SetCustomVar("NestX", bx);
                                __instance.SetCustomVar("NestY", by);
                                __instance.SetCustomVar("NestZ", bz);
                                found = true;
                            }
                        }
                    }
                }
                if (!found) return;
            }

            Vector3 homePosition = new Vector3(
                __instance.GetCustomVar("NestX"),
                __instance.GetCustomVar("NestY"),
                __instance.GetCustomVar("NestZ")
            );

            if (Vector3.Distance(__instance.position, homePosition) > MaxRadius)
            {
                __instance.PathTo(homePosition, true);
            }

            if (GameManager.Instance.World.worldTime > NightTime)
            {
                __instance.PathTo(homePosition, true);
            }
        }
    }
}
