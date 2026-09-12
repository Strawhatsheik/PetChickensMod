using UnityEngine;
using HarmonyLib;

namespace PetChickensMod
{
    [HarmonyPatch(typeof(EntityAnimal))]
    public class Patch_ChickenAI
    {
        private const float WanderRadius = 15f;   // blocks from coop during day
        private const float ReturnRadius = 3f;    // blocks from coop at night
        private const float SearchRadius = 20f;   // how far to look for a coop on spawn
        private const ulong NightStartTick = 19000UL; // 7 PM

        [HarmonyPatch("UpdateAITasks")]
        [HarmonyPostfix]
        static void ManageCoopBehavior(EntityAnimal __instance)
        {
            if (!ChickenNestManager.IsPetChicken(__instance)) return;

            // First tick: find and claim a coop
            if (__instance.GetCVar("CoopSet") == 0f)
            {
                TryClaimNearestCoop(__instance);
                if (__instance.GetCVar("CoopSet") == 0f) return;
            }

            var coopPos = new Vector3(
                __instance.GetCVar("CoopX"),
                __instance.GetCVar("CoopY"),
                __instance.GetCVar("CoopZ")
            );

            bool isNight = GameManager.Instance.World.worldTime % 24000UL > NightStartTick;
            float distFromCoop = Vector3.Distance(__instance.position, coopPos);
            float threshold = isNight ? ReturnRadius : WanderRadius;

            if (distFromCoop > threshold)
                __instance.FindPath(coopPos, __instance.GetMoveSpeed(), false, null);
        }

        static void TryClaimNearestCoop(EntityAnimal chicken)
        {
            World world = GameManager.Instance.World;
            Vector3 pos = chicken.position;
            float bestDist = float.MaxValue;
            Vector3i bestCoop = default;
            bool found = false;

            for (int dx = -(int)SearchRadius; dx <= (int)SearchRadius; dx++)
            for (int dy = -2; dy <= 2; dy++)
            for (int dz = -(int)SearchRadius; dz <= (int)SearchRadius; dz++)
            {
                int bx = (int)pos.x + dx;
                int by = (int)pos.y + dy;
                int bz = (int)pos.z + dz;

                if (world.GetBlock(bx, by, bz).Block.GetBlockName() != "cntChickenCoop") continue;

                float d = new Vector3(dx, dy, dz).magnitude;
                if (d >= bestDist) continue;

                var candidate = new Vector3i(bx, by, bz);
                if (!ChickenNestManager.TryClaimCoop(candidate, chicken.entityId, world)) continue;

                bestDist = d;
                bestCoop = candidate;
                found = true;
            }

            if (!found) return;

            chicken.SetCVar("CoopX", bestCoop.x);
            chicken.SetCVar("CoopY", bestCoop.y);
            chicken.SetCVar("CoopZ", bestCoop.z);
            chicken.SetCVar("CoopSet", 1f);
            UnityEngine.Debug.Log($"[ChickenMod] Chicken {chicken.entityId} claimed coop at {bestCoop}");
        }

        [HarmonyPatch("Kill")]
        [HarmonyPostfix]
        static void ReleaseClaim(EntityAnimal __instance)
        {
            if (!ChickenNestManager.IsPetChicken(__instance)) return;
            ChickenNestManager.ReleaseChicken(__instance.entityId);
        }
    }
}
