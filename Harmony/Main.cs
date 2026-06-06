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
            UnityEngine.Debug.Log("[ChickenMod] Harmony patches applied successfully!");
        }
    }
}
