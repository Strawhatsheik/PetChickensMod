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

            ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
            ModEvents.GameShutdown.RegisterHandler(OnGameShutdown);

            UnityEngine.Debug.Log("[ChickenMod] Loaded.");
        }

        static void OnGameStartDone() => ChickenNestManager.LoadNames();
        static void OnGameShutdown()  => ChickenNestManager.SaveNames();
    }
}
