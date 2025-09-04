using HarmonyLib;

public class ChickenMod
{
    public static void Init()
    {
        var harmony = new Harmony("com.yourmod.chickenmod");
        harmony.PatchAll();
    }
}
