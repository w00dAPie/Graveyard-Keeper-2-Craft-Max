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
        private static readonly MethodInfo OtherGroup = AccessTools.Method(
            typeof(GamepadNavigationController),
            "TryGetItemFromOtherGroup"
        );
        private static readonly MethodInfo NearestGrid = AccessTools.Method(
            typeof(GamepadNavigationController),
            "GetNearestItemWithLegacyGridAndMaxDistance"
        );
        private static readonly MethodInfo NearestCorridor = AccessTools.Method(
            typeof(GamepadNavigationController),
            "GetNearestItemWithCrossAxisCorridor"
        );
        private static readonly MethodInfo VerticalWrap = AccessTools.Method(
            typeof(GamepadNavigationController),
            "GetVerticalWrapItem"
        );
        private static readonly AccessTools.FieldRef<
            GamepadNavigationController,
            Dictionary<int, GamepadNavigationItem>
        > ReadLastFocus = AccessTools.FieldRefAccess<
            GamepadNavigationController,
            Dictionary<int, GamepadNavigationItem>
        >("lastFocusedItems");

        internal static bool IsUsable(GamepadNavigationItem item) =>
            item != null && item.Active && item.isActiveAndEnabled;

        internal static GamepadNavigationItem ResolveDirection(
            GamepadNavigationController controller,
            GamepadNavigationItem from,
            GUIDirection direction
        )
        {
            var items = GetItems(controller);
            var custom = from.GetCustomDirectionItem(direction);
            if (custom != null && items.Contains(custom))
                return custom;
            var candidates = new List<GamepadNavigationItem>();
            foreach (var item in items)
                if (
                    item != from
                    && IsUsable(item)
                    && item.group == from.group
                    && item.CorrectDirection(from.Pos, direction)
                )
                    candidates.Add(item);
            if (candidates.Count == 0)
            {
                var other = (GamepadNavigationItem)
                    OtherGroup.Invoke(controller, [from.group, from.Pos, direction]);
                if (other != null)
                    return other;
                if (
                    controller.loopVerticalNavigation
                    && from.group == 0
                    && (direction == GUIDirection.Up || direction == GUIDirection.Down)
                )
                    return (GamepadNavigationItem)
                        VerticalWrap.Invoke(controller, [from, from.group, direction]);
                return null;
            }
            var next = controller.checkMaxCrossAxisDistance
                ? (GamepadNavigationItem)
                    NearestCorridor.Invoke(controller, [candidates, from.Pos, direction])
                : (GamepadNavigationItem)
                    NearestGrid.Invoke(controller, [candidates, from.Pos, from.group, direction]);
            if (
                next != null
                && controller.restoreLastInGroup
                && next.group != from.group
                && controller.HaveSavedFocusForGroup(next.group)
            )
                return ReadLastFocus(controller)[next.group];
            return next;
        }

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
