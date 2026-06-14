using UnityEngine;
using HarmonyLib;

namespace PetChickensMod
{
    [HarmonyPatch(typeof(EntityAnimal))]
    public class Patch_ChickenRunAway
    {
        [HarmonyPatch("UpdateAITasks")]
        [HarmonyPostfix]
        static void CheckHungerAndFlee(EntityAnimal __instance)
        {
            if (!ChickenNestManager.IsPetChicken(__instance)) return;

            float hunger = __instance.GetCVar("Hunger");

            if (hunger <= 0 && Random.value < 0.1f)
            {
                Vector3 fleePos = __instance.position + new Vector3(20, 0, 20);
                __instance.FindPath(fleePos, __instance.GetMoveSpeedPanic(), false, null);
            }
        }
    }
}
