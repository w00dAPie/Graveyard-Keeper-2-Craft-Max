using GK2CraftMax.Helpers;
using HarmonyLib;
using LazyBearTechnology;

namespace GK2CraftMax.Patches
{
    [HarmonyPatch(typeof(UICraftSelectionWindow), "Redraw")]
    internal static class CraftSelectionWindowPatch
    {
        [HarmonyPostfix]
        private static void Postfix(UICraftSelectionWindow __instance)
        {
            CraftMaxHelper.HandleRedraw(__instance);
        }
    }

    [HarmonyPatch(typeof(UIFuelCraftWindow), "Redraw")]
    internal static class FuelCraftWindowPatch
    {
        [HarmonyPostfix]
        private static void Postfix(UIFuelCraftWindow __instance)
        {
            CraftMaxHelper.HandleRedraw(__instance);
        }
    }

    [HarmonyPatch(typeof(UISingleCraftWindow), "Redraw")]
    internal static class SingleCraftWindowPatch
    {
        [HarmonyPostfix]
        private static void Postfix(UISingleCraftWindow __instance)
        {
            CraftMaxHelper.HandleRedraw(__instance);
        }
    }

    /*
     * ZENTRALE A-BUTTON-LOGIK
     *
     * MAX fokussiert:
     *     A -> MAX
     *     Vanilla Craft/Place wird blockiert.
     *
     * MAX nicht fokussiert:
     *     Vanilla OnStartCraft läuft unverändert.
     */
    [HarmonyPatch(typeof(UIBaseCraftSelectionWindow), "OnStartCraft")]
    internal static class CraftMaxStartCraftPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(UIBaseCraftSelectionWindow __instance, ref bool __result)
        {
            if (!CraftMaxHelper.TryActivateFocusedMax(__instance))
            {
                /*
                 * MAX nicht fokussiert.
                 *
                 * Vanilla:
                 * OnStartCraftPressed()
                 * -> Craft / Place
                 */
                return true;
            }

            /*
             * MAX wurde ausgeführt.
             *
             * GameKey gilt als verarbeitet,
             * aber das originale OnStartCraft()
             * darf NICHT mehr laufen.
             */
            __result = true;

            return false;
        }
    }
}
