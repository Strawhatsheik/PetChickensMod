using HarmonyLib;
using System;
using System.Diagnostics;
using UnityEngine;

[HarmonyPatch(typeof(EntityAnimal))]
class Patch_ChickenRunAway
{
    [HarmonyPatch("UpdateAITasks")]
    [HarmonyPostfix]
    static void CheckHungerAndFlee(EntityAnimal __instance)
    {
        if (__instance.EntityName != "Pet Chicken") return;

        float hunger = __instance.GetCustomVar("Hunger");

        if (hunger <= 0 && Random.value < 0.1f) // 10% chance to run away
        {
            Vector3 fleePos = __instance.position + new Vector3(20, 0, 20);
            __instance.PathTo(fleePos, true);
            Debug.Log("Chicken ran away due to hunger!");
        }
    }
}