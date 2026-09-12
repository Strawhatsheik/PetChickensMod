using System;
using System.IO;
using UnityEngine;

namespace PetChickensMod
{
    public class ChickenMod : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            // File-write proof that InitMod ran (bypasses Unity log buffering)
            try
            {
                File.WriteAllText(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "chicken_debug.txt"),
                    "InitMod called at " + DateTime.Now);
            }
            catch { }

            UnityEngine.Debug.Log("[ChickenMod] InitMod called — DLL loaded OK");
            try
            {
                var uiHost = new GameObject("ChickenModUI");
                uiHost.AddComponent<ChickenRenameUI>();

                ModEvents.GameStartDone.RegisterHandler((ref ModEvents.SGameStartDoneData _) =>
                {
                    CoopWatcher.Reset();
                    ChickenNestManager.LoadNames();
                });
                ModEvents.GameShutdown.RegisterHandler((ref ModEvents.SGameShutdownData _) => ChickenNestManager.SaveNames());

                UnityEngine.Debug.Log("[ChickenMod] v1.2 loaded — no-Harmony build.");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[ChickenMod] InitMod FAILED: " + e);
            }
        }
    }
}
