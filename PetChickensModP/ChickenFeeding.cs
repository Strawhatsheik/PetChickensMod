using UnityEngine;
using HarmonyLib;

namespace PetChickensMod
{
    [HarmonyPatch(typeof(EntityAnimal))]
    public class Patch_ChickenFeeding
    {
        // One in-game day = 24000 ticks (1000 ticks per hour).
        private const ulong TicksPerDay = 24000UL;

        [HarmonyPatch("UpdateAITasks")]
        [HarmonyPostfix]
        static void CheckForFood(EntityAnimal __instance)
        {
            if (!ChickenNestManager.IsPetChicken(__instance)) return;
            if (__instance.GetCVar("NestSet") == 0f) return;

            World world = GameManager.Instance.World;
            int currentDay = (int)(world.worldTime / TicksPerDay);
            int lastFedDay = (int)__instance.GetCVar("LastFedDay");

            // Default CVar is 0; use -1 sentinel stored as 0 by checking < 0 on first run.
            // On day 0 itself the first check fires because CVar default 0 != day 0+1 offset —
            // we store (currentDay + 1) so 0 always means "never checked".
            if (currentDay + 1 == lastFedDay) return; // Already handled today

            // Search for the trough near the nest (not near the chicken's wandering position).
            var nestPos = new Vector3i(
                (int)__instance.GetCVar("NestX"),
                (int)__instance.GetCVar("NestY"),
                (int)__instance.GetCVar("NestZ")
            );

            bool fed = TryConsumeFromTrough(__instance, world, nestPos);

            if (fed)
            {
                __instance.SetCVar("Hunger", 1.0f);
                ProduceInNest(__instance, world, nestPos);
            }
            else
            {
                // One missed day = -0.5 hunger. Two consecutive starved days → Hunger reaches 0 → flee.
                float hunger = Mathf.Max(0f, __instance.GetCVar("Hunger") - 0.5f);
                __instance.SetCVar("Hunger", hunger);
            }

            // Store (currentDay + 1) so the "never checked" default (0) is unambiguous.
            __instance.SetCVar("LastFedDay", (float)(currentDay + 1));
        }

        static bool TryConsumeFromTrough(EntityAnimal chicken, World world, Vector3i nestPos)
        {
            ItemValue cornMeal = ItemClass.GetItem("foodCornMeal", false);
            if (cornMeal.type == 0) return false;

            for (int x = -7; x <= 7; x++)
            for (int z = -7; z <= 7; z++)
            {
                int bx = nestPos.x + x;
                int bz = nestPos.z + z;
                BlockValue block = world.GetBlock(bx, nestPos.y, bz);
                if (block.Block.GetBlockName() != "cntChickenTrough") continue;

                TileEntity te = world.GetTileEntity(0, new Vector3i(bx, nestPos.y, bz));
                if (!(te is TileEntityLootContainer loot)) continue;
                if (!loot.HasItem(cornMeal)) continue;

                loot.RemoveItem(cornMeal);
                return true;
            }
            return false;
        }

        static void ProduceInNest(EntityAnimal chicken, World world, Vector3i nestPos)
        {
            TileEntity te = world.GetTileEntity(0, nestPos);
            if (!(te is TileEntityLootContainer loot)) return;

            if (Random.value < 0.30f)
                TryAddToLoot(loot, ItemClass.GetItem("foodEgg", false));

            if (Random.value < 0.20f)
                TryAddToLoot(loot, ItemClass.GetItem("resourceFeather", false));
        }

        static void TryAddToLoot(TileEntityLootContainer loot, ItemValue item)
        {
            if (item.type == 0) return;
            for (int i = 0; i < loot.items.Length; i++)
            {
                if (loot.items[i].IsEmpty())
                {
                    loot.items[i] = new ItemStack(item.Clone(), 1);
                    loot.SetModified();
                    return;
                }
                if (loot.items[i].itemValue.type == item.type)
                {
                    loot.items[i].count++;
                    loot.SetModified();
                    return;
                }
            }
        }
    }
}
