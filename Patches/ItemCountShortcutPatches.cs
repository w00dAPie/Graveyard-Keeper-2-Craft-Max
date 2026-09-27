using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LazyBearTechnology;

namespace GK2CraftMax.Patches
{
    [HarmonyPatch(typeof(UIItemCountWindow), "GetGameKeyDelegates")]
    internal static class ItemCountShortcutPatches
    {
        private const int FastStep = SmartSlider.GAMEPAD_FAST_SLIDER_CHANGE_VALUE;

        private static readonly AccessTools.FieldRef<UIItemCountWindow, SmartSlider> ReadSlider =
            AccessTools.FieldRefAccess<UIItemCountWindow, SmartSlider>("slider");

        private static readonly MethodInfo ChangeValue = AccessTools.Method(
            typeof(SmartSlider),
            "ChangeValue",
            [typeof(int), typeof(bool)]
        );

        [HarmonyPostfix]
        private static void Postfix(
            UIItemCountWindow __instance,
            ref Dictionary<GameKey, Func<bool>> __result
        )
        {
            if (__instance == null || __result == null)
                return;

            __result[GameKey.PrevTab] = () => Change(__instance, -FastStep);
            __result[GameKey.NextTab] = () => Change(__instance, FastStep);
        }

        private static bool Change(UIItemCountWindow window, int delta)
        {
            if (window == null || !window.IsShownAndTop)
                return false;

            SmartSlider slider = ReadSlider(window);

            if (slider == null || ChangeValue == null)
                return false;

            /*
             * Wenn beide Shoulder-Buttons gleichzeitig gedrückt werden,
             * nichts verändern.
             */
            GameKey opposite = delta > 0 ? GameKey.PrevTab : GameKey.NextTab;

            if (LazyInput.GetKey(opposite))
                return true;

            int before = slider.Value;

            /*
             * Vanilla SmartSlider verwenden.
             *
             * Dadurch bleiben Min, Max, SnapStep, InputField,
             * Callback und Preisberechnung vollständig erhalten.
             */
            ChangeValue.Invoke(slider, [delta, true]);

            return slider.Value != before;
        }
    }

    [HarmonyPatch(typeof(UIItemCountWindow), "PrintTips")]
    internal static class ItemCountShortcutTipsPatch
    {
        private static readonly MethodInfo AddTransferTipsMethod = AccessTools.Method(
            typeof(ItemCountShortcutTipsPatch),
            nameof(AddTransferTips)
        );

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            foreach (CodeInstruction instruction in instructions)
            {
                /*
                 * Vanilla IL:
                 *
                 * ldarg.0
                 * ldfld lazyButtonTips
                 * ldloc.0          <- vorhandene Tip-Liste
                 * ldstr "  "
                 * callvirt Print(list, "  ")
                 *
                 * Direkt vor ldstr liegt oben auf dem Stack die Tip-Liste.
                 * DUP + AddTransferTips() ergänzt genau diese Vanilla-Liste.
                 */
                if (
                    instruction.opcode == OpCodes.Ldstr
                    && instruction.operand is string separator
                    && separator == "  "
                )
                {
                    yield return new CodeInstruction(OpCodes.Dup);

                    yield return new CodeInstruction(OpCodes.Call, AddTransferTipsMethod);
                }

                yield return instruction;
            }
        }

        private static void AddTransferTips(List<LazyGameKeyTip> tips)
        {
            if (tips == null)
                return;

            tips.Add(
                new LazyGameKeyTip(
                    GameKey.PrevTab,
                    "-10",
                    active: true,
                    gamepadOnly: true,
                    translate: false
                )
            );

            tips.Add(
                new LazyGameKeyTip(
                    GameKey.NextTab,
                    "+10",
                    active: true,
                    gamepadOnly: true,
                    translate: false
                )
            );
        }
    }

    [HarmonyPatch(typeof(SmartSlider), "Update")]
    internal static class ItemCountShortcutHoldPatch
    {
        private sealed class RepeatState
        {
            internal int Direction;
            internal float NextRepeatTime;
        }

        private const float InitialDelay = 0.5f;
        private const float RepeatInterval = 0.2f;

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<
            SmartSlider,
            RepeatState
        > States = new();

        private static readonly MethodInfo ChangeValue = AccessTools.Method(
            typeof(SmartSlider),
            "ChangeValue",
            [typeof(int), typeof(bool)]
        );

        [HarmonyPostfix]
        private static void Postfix(SmartSlider __instance)
        {
            if (__instance == null || ChangeValue == null)
                return;

            /*
             * Nur SmartSlider im UIItemCountWindow anfassen.
             * Andere Slider im Spiel bleiben komplett Vanilla.
             */
            UIItemCountWindow window = __instance.GetComponentInParent<UIItemCountWindow>();

            if (
                window == null
                || !window.IsShownAndTop
                || !LazyInput.IsGamepadActive
                || !LazyInput.IsInputActive()
            )
            {
                Reset(__instance);
                return;
            }

            bool decrease = LazyInput.GetKey(GameKey.PrevTab);
            bool increase = LazyInput.GetKey(GameKey.NextTab);

            /*
             * Beide gleichzeitig = neutral.
             */
            if (decrease == increase)
            {
                Reset(__instance);
                return;
            }

            int direction = increase ? 1 : -1;

            RepeatState state = States.GetOrCreateValue(__instance);

            /*
             * Neuer Tastendruck:
             *
             * Den ersten ±10-Schritt macht bereits unser
             * GetGameKeyDelegates-Patch.
             *
             * Wir starten hier nur den Hold-Timer.
             */
            if (state.Direction != direction)
            {
                state.Direction = direction;
                state.NextRepeatTime = UnityEngine.Time.unscaledTime + InitialDelay;

                return;
            }

            if (UnityEngine.Time.unscaledTime < state.NextRepeatTime)
                return;

            /*
             * Nach der Halteverzögerung:
             *
             * alle 0,2 Sekunden ±10.
             */
            ChangeValue.Invoke(
                __instance,
                [direction * SmartSlider.GAMEPAD_FAST_SLIDER_CHANGE_VALUE, true]
            );

            state.NextRepeatTime = UnityEngine.Time.unscaledTime + RepeatInterval;
        }

        private static void Reset(SmartSlider slider)
        {
            if (!States.TryGetValue(slider, out RepeatState state))
                return;

            state.Direction = 0;
            state.NextRepeatTime = 0f;
        }
    }
}
