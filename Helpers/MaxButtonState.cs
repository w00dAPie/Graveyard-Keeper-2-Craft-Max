using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LazyBearTechnology;
using TMPro;
using UnityEngine;

namespace GK2CraftMax.Helpers
{
    // Ephemeron keys let a pooled window retain its UI state without a static ID map
    // retaining destroyed windows. Current window data is never stored here.
    internal sealed class MaxButtonState
    {
        private static readonly ConditionalWeakTable<Component, MaxButtonState> States = new();
        private LazyButton template;
        private Transform parent;
        private bool resolved;
        internal LazyButton Button;
        internal Transform Icon;
        internal TMP_Text Label;
        internal GamepadNavigationItem Navigation;
        internal bool Initialized;
        internal readonly List<UICraftItemCell> CraftCells = new();

        internal static MaxButtonState For(Component window)
        {
            if (!States.TryGetValue(window, out MaxButtonState state))
            {
                state = new MaxButtonState();
                States.Add(window, state);
            }
            return state;
        }

        internal LazyButton Resolve(LazyButton source, string name)
        {
            Transform sourceParent = source != null ? source.transform.parent : null;
            bool buttonDestroyed = Button == null && !ReferenceEquals(Button, null);
            if (
                template != source
                || parent != sourceParent
                || buttonDestroyed
                || (Button != null && Button.transform.parent != sourceParent)
            )
            {
                template = source;
                parent = sourceParent;
                Button = null;
                Icon = null;
                Label = null;
                Navigation = null;
                Initialized = false;
                resolved = false;
                CraftCells.Clear();
            }
            if (!resolved && sourceParent != null)
            {
                Transform existing = sourceParent.Find(name);
                Button = existing != null ? existing.GetComponent<LazyButton>() : null;
                resolved = true;
            }
            if (Icon == null && !ReferenceEquals(Icon, null))
                Initialized = false;
            return Button;
        }
    }
}
