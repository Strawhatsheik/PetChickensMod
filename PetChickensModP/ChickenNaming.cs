using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;

namespace PetChickensMod
{
    // Overrides the name shown in the targeting HUD for pet chickens.
    [HarmonyPatch(typeof(EntityAlive), "get_EntityName")]
    public class Patch_ChickenNameDisplay
    {
        [HarmonyPostfix]
        static void InjectCustomName(EntityAlive __instance, ref string __result)
        {
            if (!ChickenNestManager.IsPetChicken(__instance)) return;

            // Build the fallback string from the persisted CVar so world reloads
            // don't lose the number (e.g. CVar 3 → "Chicken 3").
            int num = (int)__instance.GetCVar("ChickenNumber");
            string cvarFallback = num > 0 ? num.ToString() : null;

            if (ChickenNestManager.TryGetName(__instance.entityId, cvarFallback, out string name))
                __result = name;
        }
    }

    // Console command: open F1 console and type  name Clucky
    // Works on the nearest pet chicken within 6 blocks of the player.
    // Registered automatically when the mod DLL is loaded.
    public class ConsoleCmdChickenName : ConsoleCmdAbstract
    {
        public override string GetDescription() => "Name the nearest pet chicken.";

        public override string GetHelp() =>
            "Usage: name <chicken name>\n" +
            "Renames the pet chicken closest to you (within 6 blocks).\n" +
            "Alias: chickname";

        public override string[] GetCommands() => new[] { "name", "chickname" };

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            if (_params.Count == 0)
            {
                SdtdConsole.Instance.Output("Usage: name <chicken name>");
                return;
            }

            string newName = string.Join(" ", _params).Trim();
            if (string.IsNullOrEmpty(newName))
            {
                SdtdConsole.Instance.Output("Name cannot be empty.");
                return;
            }

            World world = GameManager.Instance.World;
            EntityPlayerLocal player = world.GetPrimaryPlayer();
            if (player == null)
            {
                SdtdConsole.Instance.Output("No local player found.");
                return;
            }

            // Search for the nearest pet chicken within 6 blocks.
            var nearby = new List<Entity>();
            world.GetEntitiesInBounds(typeof(EntityAnimal),
                new Bounds(player.position, Vector3.one * 12f), nearby);

            Entity closest = null;
            float closestDist = 6f; // max range
            foreach (Entity e in nearby)
            {
                if (!ChickenNestManager.IsPetChicken(e)) continue;
                float d = Vector3.Distance(e.position, player.position);
                if (d < closestDist) { closest = e; closestDist = d; }
            }

            if (closest == null)
            {
                SdtdConsole.Instance.Output("No pet chicken within 6 blocks.");
                return;
            }

            ChickenNestManager.SetName(closest.entityId, newName);
            SdtdConsole.Instance.Output("Renamed to '" + newName + "'.");
        }
    }
}
