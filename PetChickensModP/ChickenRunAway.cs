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
            if (__instance.EntityName != "Pet Chicken") return;

            float hunger = __instance.GetCustomVar("Hunger");

            if (hunger <= 0 && Random.value < 0.1f)
            {
                Vector3 fleePos = __instance.position + new Vector3(20, 0, 20);
                __instance.PathTo(fleePos, true);
            }
        }
    }
}
