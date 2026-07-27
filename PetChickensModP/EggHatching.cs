using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;

namespace PetChickensMod
{
    [HarmonyPatch(typeof(TileEntityComposite), "UpdateTick")]
    public class Patch_EggHatching
    {
        private const int MaxChickensPerTrough = 6;

        private static readonly Dictionary<string, float> hatchChances = new Dictionary<string, float>();

        [HarmonyPostfix]
        static void TryHatchEgg(TileEntityComposite __instance, World world)
        {
            Vector3i pos = __instance.ToWorldPos();
            if (world.GetBlock(pos.x, pos.y, pos.z).Block.GetBlockName() != "cntChickenNest") return;

            TEFeatureStorage storage = __instance.GetFeature<TEFeatureStorage>();
            if (storage == null) return;

            ItemValue eggItem = ItemClass.GetItem("foodEgg", false);
            if (eggItem.type == 0 || !storage.HasItem(eggItem)) return;

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

                    int num = ChickenNestManager.NextChickenNumber();
                    if (chick is EntityAlive ea)
                    {
                        ea.SetCVar("ChickenNumber", (float)num);
                        ChickenNestManager.SetName(chick.entityId, "Chicken " + num);
                    }
                }

                storage.RemoveItem(eggItem);
                __instance.setModified();
                hatchChances[key] = 0.01f;
                UnityEngine.Debug.Log($"[ChickenMod] Egg hatched at {pos}!");
            }
            else
            {
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
    }
}
