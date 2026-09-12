using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace PetChickensMod
{
    public class ChickenMod : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            var harmony = new Harmony("com.yourname.7dtd.chickenmod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Persistent UI host — survives scene changes
            var uiHost = new GameObject("ChickenModUI");
            uiHost.AddComponent<ChickenRenameUI>();

            ModEvents.GameStartDone.RegisterHandler((ref ModEvents.SGameStartDoneData _) =>
            {
                Patch_CoopWatcher.Reset();
                ChickenNestManager.LoadNames();
            });
            ModEvents.GameShutdown.RegisterHandler((ref ModEvents.SGameShutdownData _) => ChickenNestManager.SaveNames());

            UnityEngine.Debug.Log("[ChickenMod] v1.1 loaded — Harmony patches applied.");
        }

    }
}
