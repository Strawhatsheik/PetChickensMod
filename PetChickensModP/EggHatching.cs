using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;

namespace PetChickensMod
{
    [HarmonyPatch(typeof(TileEntityCollector), "UpdateTick")]
    public class Patch_EggHatching
    {
        private const int MaxChickensPerTrough = 6;

        // Tracks hatch-probability per nest, building up until an egg hatches.
        private static readonly Dictionary<string, float> hatchChances = new Dictionary<string, float>();

        [HarmonyPostfix]
        static void TryHatchEgg(TileEntityCollector __instance, World world)
        {
            Vector3i pos = __instance.ToWorldPos();
            if (world.GetBlock(pos.x, pos.y, pos.z).Block.GetBlockName() != "cntChickenNest") return;

            if (!(__instance is TileEntityLootContainer loot)) return;

            // Only hatch if there is actually an egg sitting in the nest.
            ItemValue eggItem = ItemClass.GetItem("foodEgg", false);
            if (eggItem.type == 0 || !loot.HasItem(eggItem)) return;

            // Cap: find the nearest trough and count how many nests around it are already owned.
            if (!FindNearbyTrough(world, pos, 10, out Vector3i troughPos)) return;
            if (ChickenNestManager.CountOwnedNestsNear(troughPos, 7, world) >= MaxChickensPerTrough) return;

            string key = pos.x + "," + pos.y + "," + pos.z;
            if (!hatchChances.TryGetValue(key, out float chance))
                chance = 0.01f;

            if (Random.value < chance)
            {
                int entityClassId = EntityClass.FromString("entityPetChicken");
                if (entityClassId != -1)
                {
                    Entity chick = EntityFactory.CreateEntity(entityClassId,
                        new Vector3(pos.x + 0.5f, pos.y + 0.1f, pos.z + 0.5f));
                    world.SpawnEntityInWorld(chick);

                    // Assign number and default name immediately after spawning.
                    int num = ChickenNestManager.NextChickenNumber();
                    chick.SetCVar("ChickenNumber", (float)num);
                    ChickenNestManager.SetName(chick.entityId, "Chicken " + num);
                }

                RemoveOneItem(loot, eggItem);
                hatchChances[key] = 0.01f; // Reset for the next egg
            }
            else
            {
                // Chance ramps up each tick an egg sits unhatched, capping at 50%.
                hatchChances[key] = Mathf.Min(chance + 0.001f, 0.5f);
            }
        }

        static bool FindNearbyTrough(World world, Vector3i nestPos, int radius, out Vector3i troughPos)
        {
            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -2; dy <= 2; dy++)
            for (int dz = -radius; dz <= radius; dz++)
            {
                int bx = nestPos.x + dx;
                int by = nestPos.y + dy;
                int bz = nestPos.z + dz;
                if (world.GetBlock(bx, by, bz).Block.GetBlockName() == "cntChickenTrough")
                {
                    troughPos = new Vector3i(bx, by, bz);
                    return true;
                }
            }
            troughPos = default;
            return false;
        }

        static void RemoveOneItem(TileEntityLootContainer loot, ItemValue item)
        {
            for (int i = 0; i < loot.items.Length; i++)
            {
                if (loot.items[i].itemValue.type != item.type) continue;
                loot.items[i].count--;
                if (loot.items[i].count <= 0)
                    loot.items[i] = new ItemStack(new ItemValue(0), 0);
                loot.SetModified();
                return;
            }
        }
    }
}
