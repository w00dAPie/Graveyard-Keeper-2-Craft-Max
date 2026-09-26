using System;
using System.Collections.Generic;
using System.Reflection;
using GK2CraftMax.Helpers;
using HarmonyLib;
using LazyBearTechnology;

namespace GK2CraftMax.Patches
{
    [HarmonyPatch]
    internal static class CraftQuantityShortcutPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(UICraftSelectionWindow), "GetGameKeyDelegates");
            yield return AccessTools.Method(typeof(UIFuelCraftWindow), "GetGameKeyDelegates");
            yield return AccessTools.Method(typeof(UISingleCraftWindow), "GetGameKeyDelegates");
        }

        [HarmonyPostfix]
        private static void Postfix(
            UIBaseCraftSelectionWindow __instance,
            Dictionary<GameKey, Func<bool>> __result
        )
        {
            CraftQuantityShortcutHelper.Register(__instance, __result);
        }
    }

    [HarmonyPatch(typeof(UIBaseCraftSelectionWindow), "AddCraftCountGamepadTips")]
    internal static class CraftQuantityShortcutTipsPatch
    {
        [HarmonyPostfix]
        private static void Postfix(
            UIBaseCraftSelectionWindow __instance,
            List<LazyGameKeyTip> tips
        )
        {
            CraftQuantityShortcutHelper.AddTips(__instance, tips);
        }
    }
}
