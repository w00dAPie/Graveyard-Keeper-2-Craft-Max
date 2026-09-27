using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using LazyBearTechnology;

namespace GK2CraftMax.Helpers
{
    // A reversible splice. Native items, callbacks and unrelated directions remain owned by the game.
    internal sealed class NavigationInsertion
    {
        private sealed class Link
        {
            internal GamepadNavigationItem From;
            internal GUIDirection Direction;
            internal GamepadNavigationItem Previous;
            internal GamepadNavigationItem Installed;
        }

        private static readonly Dictionary<GUIDirection, FieldInfo> Fields = new()
        {
            { GUIDirection.Left, AccessTools.Field(typeof(GamepadNavigationItem), "leftItem") },
            { GUIDirection.Right, AccessTools.Field(typeof(GamepadNavigationItem), "rightItem") },
            { GUIDirection.Up, AccessTools.Field(typeof(GamepadNavigationItem), "upItem") },
            { GUIDirection.Down, AccessTools.Field(typeof(GamepadNavigationItem), "downItem") },
        };
        private readonly List<Link> links = new();
        private GamepadNavigationItem inserted;
        internal bool IsAttached => inserted != null && links.Count > 0;

        internal static GUIDirection Opposite(GUIDirection direction)
        {
            switch (direction)
            {
                case GUIDirection.Left:
                    return GUIDirection.Right;
                case GUIDirection.Right:
                    return GUIDirection.Left;
                case GUIDirection.Up:
                    return GUIDirection.Down;
                default:
                    return GUIDirection.Up;
            }
        }

        private static GamepadNavigationItem Read(
            GamepadNavigationItem item,
            GUIDirection direction
        )
        {
            // The public getter filters inactive targets; restoration must retain those too.
            return (GamepadNavigationItem)Fields[direction].GetValue(item);
        }

        private void Set(
            GamepadNavigationItem from,
            GUIDirection direction,
            GamepadNavigationItem to
        )
        {
            links.Add(
                new Link
                {
                    From = from,
                    Direction = direction,
                    Previous = Read(from, direction),
                    Installed = to,
                }
            );
            from.SetCustomDirectionItem(direction, to);
        }

        internal void Attach(
            GamepadNavigationController controller,
            GamepadNavigationItem anchor,
            GUIDirection direction,
            GamepadNavigationItem max
        )
        {
            Restore();

            if (controller == null || !GamepadNavigationHelper.IsUsable(anchor) || max == null)
            {
                return;
            }

            List<GamepadNavigationItem> items = GamepadNavigationHelper.GetItems(controller);

            if (items == null || !items.Contains(anchor))
                return;

            /*
             * MAX darf bei der Ermittlung des Vanilla-Nachfolgers
             * selbst noch kein geometrisches Navigationsziel sein.
             */
            max.Active = false;

            GUIDirection reverse = Opposite(direction);

            /*
             * Falls Vanilla an dieser Stelle bereits einen Nachfolger
             * besitzt, merken wir ihn uns.
             *
             * Am Ende einer linearen Reihe ist successor einfach null.
             */
            GamepadNavigationItem successor = GamepadNavigationHelper.ResolveDirection(
                controller,
                anchor,
                direction
            );

            /*
             * Nur wenn Vanilla wirklich eine gegenseitige Verbindung hatte:
             *
             * anchor -> successor
             * successor -> anchor
             *
             * dürfen wir auch die Rückrichtung einschleusen.
             */
            bool reciprocal =
                successor != null
                && successor != anchor
                && GamepadNavigationHelper.ResolveDirection(controller, successor, reverse)
                    == anchor;

            /*
             * Private Gruppe für MAX.
             *
             * Dadurch wird MAX nicht versehentlich von irgendeinem
             * anderen Vanilla-Control geometrisch ausgewählt.
             */
            int group = int.MinValue;

            var groups = new HashSet<int>();

            foreach (GamepadNavigationItem item in items)
            {
                if (item != null && item != max)
                    groups.Add(item.group);
            }

            foreach (var source in controller.navigationGroupSources)
            {
                groups.Add(source.group);

                foreach (var target in source.groupTargets)
                    groups.Add(target.group);
            }

            while (groups.Contains(group))
                group++;

            max.group = group;

            max.ResetCustomDirections();

            /*
             * Rückwärts von MAX geht immer zum Anchor.
             *
             * Beispiel:
             *
             * MAX -> Links -> Zutat 3
             */
            max.SetCustomDirectionItem(reverse, anchor);

            /*
             * Vorwärts von MAX nur dann weiterleiten,
             * wenn Vanilla hinter dem Anchor tatsächlich noch
             * einen Nachfolger hatte.
             *
             * Am Ende einer linearen Reihe bleibt Rechts auf MAX
             * absichtlich leer.
             */
            if (successor != null && successor != anchor)
            {
                max.SetCustomDirectionItem(direction, successor);
            }

            max.enabled = true;
            max.Active = true;

            inserted = max;

            /*
             * Anchor -> MAX
             */
            Set(anchor, direction, max);

            /*
             * Nur eine tatsächlich vorhandene Vanilla-Rückkante ersetzen.
             */
            if (reciprocal)
            {
                Set(successor, reverse, max);
            }
        }

        internal void Restore()
        {
            foreach (var link in links)
                if (link.From != null && Read(link.From, link.Direction) == link.Installed)
                    link.From.SetCustomDirectionItem(link.Direction, link.Previous);
            links.Clear();
            if (inserted != null)
            {
                inserted.ResetCustomDirections();
                inserted.Active = false;
            }
            inserted = null;
        }

        internal void AttachRing(
            GamepadNavigationController controller,
            GamepadNavigationItem first,
            GamepadNavigationItem last,
            GamepadNavigationItem max
        )
        {
            Restore();

            if (
                controller == null
                || !GamepadNavigationHelper.IsUsable(first)
                || !GamepadNavigationHelper.IsUsable(last)
                || max == null
            )
            {
                return;
            }

            List<GamepadNavigationItem> items = GamepadNavigationHelper.GetItems(controller);

            if (items == null || !items.Contains(first) || !items.Contains(last))
            {
                return;
            }

            /*
             * MAX darf während der Vorbereitung nicht als
             * geometrisches Vanilla-Ziel auftauchen.
             */
            max.Active = false;

            /*
             * Eigene unbenutzte Gruppe für MAX.
             *
             * Dadurch wird MAX ausschließlich über unsere
             * expliziten Links erreicht.
             */
            int group = int.MinValue;

            var groups = new HashSet<int>();

            foreach (GamepadNavigationItem item in items)
            {
                if (item != null && item != max)
                    groups.Add(item.group);
            }

            foreach (var source in controller.navigationGroupSources)
            {
                groups.Add(source.group);

                foreach (var target in source.groupTargets)
                    groups.Add(target.group);
            }

            while (groups.Contains(group))
                group++;

            max.group = group;

            max.ResetCustomDirections();

            /*
             * MAX schließt den Ring:
             *
             * last -> MAX -> first
             */
            max.SetCustomDirectionItem(GUIDirection.Left, last);

            max.SetCustomDirectionItem(GUIDirection.Right, first);

            max.enabled = true;
            max.Active = true;

            inserted = max;

            /*
             * Nur die beiden äußeren Vanilla-Enden verändern.
             *
             * Die kompletten inneren Verbindungen:
             *
             * first -> slot2 -> slot3 -> ... -> last
             *
             * bleiben unangetastet.
             */
            Set(last, GUIDirection.Right, max);

            Set(first, GUIDirection.Left, max);
        }
    }
}
