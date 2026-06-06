using UnityEngine;
using HarmonyLib;

namespace PetChickensMod
{
    [HarmonyPatch(typeof(BlockSecureLoot))]
    public class Patch_EggHatching
    {
        [HarmonyPatch("UpdateBlock")]
        [HarmonyPostfix]
        static void TryHatchEgg(BlockSecureLoot __instance)
        {
            if (__instance.GetBlockName() != "chickenNest") return;

            float hatchChance = __instance.GetCustomVar("EggHatchChance");
            if (Random.value < hatchChance)
            {
                GameManager.Instance.World.SpawnEntityInWorld(new EntityAnimal("Pet Chicken"), __instance.position);
                __instance.SetCustomVar("EggHatchChance", 0);
            }
        }
    }
}
