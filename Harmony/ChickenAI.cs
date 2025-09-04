using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

[HarmonyPatch(typeof(EntityAnimal))]
class Patch_ChickenAI
{
    private static readonly float MaxRadius = 10f; // Max distance from nest
    private static readonly float NightTime = 19000f; // Game time for nightfall

    [HarmonyPatch("UpdateAITasks")]
    [HarmonyPostfix]
    static void ManageNestBehavior(EntityAnimal __instance)
    {
        if (__instance.EntityName == "Pet Chicken")
        {
            Vector3 homePosition;
            if (!__instance.Buffs.HasCustomVar("NestX")) // Check if nest is assigned
            {
                EntityPlayer owner = GameManager.Instance.World.GetClosestPlayer(__instance.position, 15);
                if (owner != null)
                {
                    BlockValue nestBlock = GameManager.Instance.World.GetBlock((int)owner.position.x, (int)owner.position.y, (int)owner.position.z);
                    if (nestBlock.Block.GetBlockName() == "chickenNest") // If near a nest, set home
                    {
                        __instance.Buffs.SetCustomVar("NestX", owner.position.x);
                        __instance.Buffs.SetCustomVar("NestY", owner.position.y);
                        __instance.Buffs.SetCustomVar("NestZ", owner.position.z);
                    }
                }
            }

            homePosition = new Vector3(
                __instance.Buffs.GetCustomVar("NestX"),
                __instance.Buffs.GetCustomVar("NestY"),
                __instance.Buffs.GetCustomVar("NestZ")
            );

            if (Vector3.Distance(__instance.position, homePosition) > MaxRadius)
            {
                __instance.moveHelper.MoveTo(homePosition, true);
            }

            if (GameManager.Instance.World.worldTime > NightTime) // Nightfall check
            {
                __instance.moveHelper.MoveTo(homePosition, true);
            }
        }
    }
}
[HarmonyPatch(typeof(EntityAnimal))]
class Patch_ChickenFeeding
{
    [HarmonyPatch("UpdateAITasks")]
    [HarmonyPostfix]
    static void CheckForTrough(EntityAnimal __instance)
    {
        if (__instance.EntityName == "Pet Chicken")
        {
            Vector3 pos = __instance.position;
            bool foundFood = false;

            // Scan nearby blocks for a trough
            for (int x = -5; x <= 5; x++)
            {
                for (int z = -5; z <= 5; z++)
                {
                    BlockValue block = GameManager.Instance.World.GetBlock((int)pos.x + x, (int)pos.y, (int)pos.z + z);
                    if (block.Block.GetBlockName() == "chickenTrough")
                    {
                        if (block.Buffs.GetCustomVar("CornMeal") > 0)
                        {
                            foundFood = true;
                            block.Buffs.SetCustomVar("CornMeal", block.Buffs.GetCustomVar("CornMeal") - 1); // Consume food
                            __instance.Buffs.SetCustomVar("Hunger", 1.0f); // Reset hunger
                        }
                    }
                }
            }

            if (!foundFood)
            {
                __instance.Buffs.SetCustomVar("Hunger", Mathf.Max(0, __instance.Buffs.GetCustomVar("Hunger") - 0.01f));
            }
        }
    }
}

[HarmonyPatch(typeof(EntityAnimal))]
class Patch_ChickenScavenging
{
    [HarmonyPatch("UpdateAITasks")]
    [HarmonyPostfix]
    static void CheckForGrass(EntityAnimal __instance)
    {
        if (__instance.EntityName == "Pet Chicken")
        {
            int grassCount = 0;
            Vector3 pos = __instance.position;

            // Scan for grass blocks
            for (int x = -5; x <= 5; x++)
            {
                for (int z = -5; z <= 5; z++)
                {
                    BlockValue block = GameManager.Instance.World.GetBlock((int)pos.x + x, (int)pos.y, (int)pos.z + z);
                    if (block.Block.GetBlockName() == "terrainGrass")
                    {
                        grassCount++;
                    }
                }
            }

            if (grassCount >= 10) // Enough grass to survive
            {
                __instance.Buffs.SetCustomVar("Hunger", 0.5f); // Not starving, but won't lay eggs
            }
        }
    }
}
[HarmonyPatch(typeof(BlockSecureLoot))]
class Patch_NestHunger
{
    [HarmonyPatch("UpdateBlock")]
    [HarmonyPostfix]
    static void ReduceEggsOverTime(BlockValue __instance)
    {
        if (__instance.Block.GetBlockName() == "chickenNest")
        {
            float hunger = __instance.Buffs.GetCustomVar("Hunger");
            if (hunger > 0)
            {
                __instance.Buffs.SetCustomVar("Hunger", hunger - 0.1f); // Decrease hunger over time
            }
            else
            {
                __instance.Buffs.SetCustomVar("EggChance", Mathf.Max(0, __instance.Buffs.GetCustomVar("EggChance") - 0.05f));
            }
        }
    }
}
[HarmonyPatch(typeof(EntityAnimal))]
class Patch_ChickenRunAway
{
    [HarmonyPatch("UpdateAITasks")]
    [HarmonyPostfix]
    static void CheckHungerAndFlee(EntityAnimal __instance)
    {
        if (__instance.EntityName == "Pet Chicken")
        {
            BlockValue nest = GameManager.Instance.World.GetBlock((int)__instance.Buffs.GetCustomVar("NestX"), 
                (int)__instance.Buffs.GetCustomVar("NestY"), 
                (int)__instance.Buffs.GetCustomVar("NestZ"));

            if (nest.Block.GetBlockName() == "chickenNest")
            {
                float hunger = nest.Buffs.GetCustomVar("Hunger");
                if (hunger <= 0 && Random.value < 0.1f) // 10% chance to run away
                {
                    __instance.moveHelper.MoveTo(__instance.position + new Vector3(20, 0, 20), true);
                }
            }
        }
    }
}
[HarmonyPatch(typeof(BlockSecureLoot))]
class Patch_EggHatching
{
    [HarmonyPatch("UpdateBlock")]
    [HarmonyPostfix]
    static void TryHatchEgg(BlockValue __instance)
    {
        if (__instance.Block.GetBlockName() == "chickenNest")
        {
            float hatchChance = __instance.Buffs.GetCustomVar("EggHatchChance");
            if (Random.value < hatchChance)
            {
                GameManager.Instance.World.SpawnEntityInWorld(new EntityAnimal("Pet Chicken"), __instance.ToWorldPos());
                __instance.Buffs.SetCustomVar("EggHatchChance", 0); // Reset hatch chance
            }
        }
    }
}


