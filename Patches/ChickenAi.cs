using HarmonyLib;
using System.Diagnostics;
using UnityEngine;

[HarmonyPatch(typeof(EntityAnimal))]
class Patch_ChickenAI
{
    private static readonly float MaxRadius = 10f;
    private static readonly float NightTime = 19000f;

    [HarmonyPatch("UpdateAITasks")]
    [HarmonyPostfix]
    static void ManageNestBehavior(EntityAnimal __instance)
    {
        if (__instance.EntityName != "Pet Chicken") return;

        Vector3 homePosition;

        // Assign nest if not already set
        if (!__instance.HasCustomVar("NestX"))
        {
            EntityPlayer owner = GameManager.Instance.World.GetClosestPlayer(__instance.position, 15);
            if (owner != null)
            {
                homePosition = owner.position;
                __instance.SetCustomVar("NestX", homePosition.x);
                __instance.SetCustomVar("NestY", homePosition.y);
                __instance.SetCustomVar("NestZ", homePosition.z);
            }
        }
        else
        {
            homePosition = new Vector3(
                __instance.GetCustomVar("NestX"),
                __instance.GetCustomVar("NestY"),
                __instance.GetCustomVar("NestZ")
            );
        }

        // Move back if too far from nest
        if (Vector3.Distance(__instance.position, homePosition) > MaxRadius)
        {
            __instance.PathTo(homePosition, true);
            Debug.Log("Chicken returning to nest");
        }

        // Return home at night
        if (GameManager.Instance.World.worldTime > NightTime)
        {
            __instance.PathTo(homePosition, true);
        }
    }
}