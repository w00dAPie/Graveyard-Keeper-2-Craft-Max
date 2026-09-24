using System.Collections.Generic;
using HarmonyLib;
using LazyBearTechnology;

namespace GK2CraftMax.Helpers
{
    internal static class GamepadNavigationHelper
    {
        internal static GamepadNavigationController GetController(object window)
        {
            if (window == null)
                return null;

            return Traverse
                .Create(window)
                .Property("GamepadNavigationController")
                .GetValue<GamepadNavigationController>();
        }

        internal static List<GamepadNavigationItem> GetItems(GamepadNavigationController controller)
        {
            if (controller == null)
                return null;

            return Traverse
                .Create(controller)
                .Field("selectableItems")
                .GetValue<List<GamepadNavigationItem>>();
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
            params GamepadNavigationItem[] newItems
        )
        {
            List<GamepadNavigationItem> items = GetItems(controller);

            if (items == null)
                return;

            foreach (GamepadNavigationItem item in newItems)
            {
                if (item != null && !items.Contains(item))
                {
                    items.Add(item);
                }
            }

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

            nav.SetCallbacks(
                null,
                null,
                () =>
                {
                    if (button.interactable)
                    {
                        button.onClick.Invoke();
                    }
                }
            );
        }
    }
}
