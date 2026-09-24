using BepInEx;
using HarmonyLib;

namespace GK2CraftMax
{
    [BepInPlugin(
        "de.w00dst0ckOo.gk2.craftmax",
        "Graveyard Keeper 2 - Craft Max",
        "0.4.0"
    )]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony _harmony;

        private void Awake()
        {
            Logger.LogInfo("GK2 Craft Max 0.4.0 loading...");

            _harmony = new Harmony(
                "de.w00dst0ckOo.gk2.craftmax"
            );

            _harmony.PatchAll();

            Logger.LogInfo("GK2 Craft Max loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}