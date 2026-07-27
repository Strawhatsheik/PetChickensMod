using UnityEngine;
using HarmonyLib;

namespace PetChickensMod
{
    [HarmonyPatch(typeof(EntityAnimal))]
    public class Patch_ChickenFeeding
    {
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

            if (currentDay + 1 == lastFedDay) return;

            var nestPos = new Vector3i(
                (int)__instance.GetCVar("NestX"),
                (int)__instance.GetCVar("NestY"),
                (int)__instance.GetCVar("NestZ")
            );

            bool fed = TryConsumeFromTrough(world, nestPos);

            if (fed)
            {
                UnityEngine.Debug.Log($"[ChickenMod] Chicken {__instance.entityId} fed from trough near nest {nestPos}");
                __instance.SetCVar("Hunger", 1.0f);
                ProduceInNest(world, nestPos);
            }
            else
            {
                UnityEngine.Debug.Log($"[ChickenMod] Chicken {__instance.entityId} found no cornmeal — hunger dropping");
                float hunger = Mathf.Max(0f, __instance.GetCVar("Hunger") - 0.5f);
                __instance.SetCVar("Hunger", hunger);
            }

            __instance.SetCVar("LastFedDay", (float)(currentDay + 1));
        }

        static bool TryConsumeFromTrough(World world, Vector3i nestPos)
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

                TEFeatureStorage storage = GetStorage(world, new Vector3i(bx, nestPos.y, bz), out TileEntityComposite composite);
                if (storage == null || !storage.HasItem(cornMeal)) continue;

                storage.RemoveItem(cornMeal);
                composite.setModified();
                return true;
            }
            return false;
        }

        static void ProduceInNest(World world, Vector3i nestPos)
        {
            TEFeatureStorage storage = GetStorage(world, nestPos, out TileEntityComposite composite);
            if (storage == null) return;

            if (Random.value < 0.30f)
                TryAddToStorage(storage, composite, ItemClass.GetItem("foodEgg", false));

            if (Random.value < 0.20f)
                TryAddToStorage(storage, composite, ItemClass.GetItem("resourceFeather", false));
        }

        static TEFeatureStorage GetStorage(World world, Vector3i pos, out TileEntityComposite composite)
        {
            TileEntity te = world.GetTileEntity(pos);
            composite = te as TileEntityComposite;
            return composite?.GetFeature<TEFeatureStorage>();
        }

        static void TryAddToStorage(TEFeatureStorage storage, TileEntityComposite composite, ItemValue item)
        {
            if (item.type == 0) return;
            storage.AddItem(new ItemStack(item.Clone(), 1));
            composite.setModified();
            UnityEngine.Debug.Log($"[ChickenMod] Produced {item.ItemClass?.GetItemName()} in nest");
        }
    }
}
