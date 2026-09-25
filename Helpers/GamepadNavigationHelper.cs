using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using LazyBearTechnology;
using UnityEngine;
using UnityEngine.Events;

namespace GK2CraftMax.Helpers
{
    internal static class GamepadNavigationHelper
    {
        private static readonly AccessTools.FieldRef<
            GamepadNavigationController,
            List<GamepadNavigationItem>
        > ReadItems = AccessTools.FieldRefAccess<
            GamepadNavigationController,
            List<GamepadNavigationItem>
        >("selectableItems");
        private static readonly ConditionalWeakTable<LazyButton, ButtonPress> PressCallbacks =
            new();

        private static class ControllerAccessor<T>
        {
            internal static readonly MethodInfo Getter = AccessTools.PropertyGetter(
                typeof(T),
                "GamepadNavigationController"
            );
        }

        private sealed class ButtonPress
        {
            private readonly LazyButton button;
            internal readonly UnityAction Callback;

            internal ButtonPress(LazyButton button)
            {
                this.button = button;
                Callback = Press;
            }

            private void Press()
            {
                if (button != null && button.interactable)
                    button.onClick.Invoke();
            }
        }

        internal static GamepadNavigationController GetController<T>(T window)
            where T : Object
        {
            if (window == null)
                return null;

            return (GamepadNavigationController)ControllerAccessor<T>.Getter.Invoke(window, null);
        }

        internal static List<GamepadNavigationItem> GetItems(GamepadNavigationController controller)
        {
            if (controller == null)
                return null;

            return ReadItems(controller);
        }

        internal static void Register(
            GamepadNavigationController controller,
            GamepadNavigationItem item,
            float guiScale
        )
        {
            if (controller == null || item == null)
            {
                return;
            }

            List<GamepadNavigationItem> items = GetItems(controller);

            if (items == null || items.Contains(item))
            {
                return;
            }

            items.Add(item);

            item.Init(items.Count - 1, controller, guiScale);
        }

        internal static void RegisterAndReinit(
            GamepadNavigationController controller,
            float guiScale,
            GamepadNavigationItem first,
            GamepadNavigationItem second
        )
        {
            List<GamepadNavigationItem> items = GetItems(controller);

            if (items == null)
                return;

            if (first != null && !items.Contains(first))
                items.Add(first);
            if (second != null && !items.Contains(second))
                items.Add(second);

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    items[i].Init(i, controller, guiScale);
                }
            }
        }

        internal static void Link(
            GamepadNavigationItem from,
            GUIDirection direction,
            GamepadNavigationItem to
        )
        {
            if (from == null || to == null)
                return;

            from.SetCustomDirectionItem(direction, to);
        }

        internal static void BindButtonPress(GamepadNavigationItem nav, LazyButton button)
        {
            if (nav == null || button == null)
                return;

            if (!PressCallbacks.TryGetValue(button, out ButtonPress press))
            {
                press = new ButtonPress(button);
                PressCallbacks.Add(button, press);
            }
            // The game may rebuild navigation callbacks; preserve rebinding and focus.
            nav.SetCallbacks(null, null, press.Callback);
        }
    }
}
