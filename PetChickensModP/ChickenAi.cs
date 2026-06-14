using UnityEngine;
using HarmonyLib;

namespace PetChickensMod
{
    [HarmonyPatch(typeof(EntityAnimal))]
    public class Patch_ChickenAI
    {
        private const float MaxWanderRadius = 10f;
        private const float NightTime = 19000f;  // 7 PM in game ticks (1 tick = 1 game-minute)

        [HarmonyPatch("UpdateAITasks")]
        [HarmonyPostfix]
        static void ManageNestBehavior(EntityAnimal __instance)
        {
            if (!ChickenNestManager.IsPetChicken(__instance)) return;

            if (__instance.GetCVar("NestSet") == 0f)
            {
                TryAssignNest(__instance);
                if (__instance.GetCVar("NestSet") == 0f) return;
            }

            var homePos = new Vector3(
                __instance.GetCVar("NestX"),
                __instance.GetCVar("NestY"),
                __instance.GetCVar("NestZ")
            );

            bool isNight = GameManager.Instance.World.worldTime % 24000 > NightTime;
            bool tooFar = Vector3.Distance(__instance.position, homePos) > MaxWanderRadius;

            if (isNight || tooFar)
                __instance.FindPath(homePos, __instance.GetMoveSpeed(), false, null);
        }

        static void TryAssignNest(EntityAnimal chicken)
        {
            World world = GameManager.Instance.World;
            Vector3 pos = chicken.position;

            for (int dx = -10; dx <= 10; dx++)
            for (int dy = -2; dy <= 2; dy++)
            for (int dz = -10; dz <= 10; dz++)
            {
                int bx = (int)pos.x + dx;
                int by = (int)pos.y + dy;
                int bz = (int)pos.z + dz;

                if (world.GetBlock(bx, by, bz).Block.GetBlockName() != "cntChickenNest") continue;

                var nestPos = new Vector3i(bx, by, bz);
                if (!ChickenNestManager.TryClaimNest(nestPos, chicken.entityId, world)) continue;

                chicken.SetCVar("NestX", bx);
                chicken.SetCVar("NestY", by);
                chicken.SetCVar("NestZ", bz);
                chicken.SetCVar("NestSet", 1f);
                return;
            }
        }

        // Free the nest when the chicken dies so another can take it.
        [HarmonyPatch("Kill")]
        [HarmonyPostfix]
        static void ReleaseClaim(EntityAnimal __instance)
        {
            if (!ChickenNestManager.IsPetChicken(__instance)) return;
            ChickenNestManager.ReleaseChicken(__instance.entityId);
        }
    }
}
