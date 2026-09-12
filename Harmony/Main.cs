using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace PetChickensMod
{
    public class ChickenMod : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            UnityEngine.Debug.Log("[ChickenMod] InitMod called — DLL loaded OK");
            try
            {
                var harmony = new Harmony("com.yourname.7dtd.chickenmod");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

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
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[ChickenMod] InitMod FAILED: " + e);
            }
        }
    }
}
