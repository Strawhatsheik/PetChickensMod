using HarmonyLib;
using System.Reflection;

namespace PetChickensMod
{
    public class ChickenMod : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            var harmony = new Harmony("com.yourname.7dtd.chickenmod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

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
