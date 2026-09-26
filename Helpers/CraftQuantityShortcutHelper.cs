using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using LazyBearTechnology;

namespace GK2CraftMax.Helpers
{
    internal static class CraftQuantityShortcutHelper
    {
        private static readonly AccessTools.FieldRef<
            UIBaseCraftSelectionWindow,
            UIBaseCraftSelectionWindowData
        > ReadData = AccessTools.FieldRefAccess<
            UIBaseCraftSelectionWindow,
            UIBaseCraftSelectionWindowData
        >("data");
        private static readonly AccessTools.FieldRef<
            UIBaseCraftSelectionWindow,
            LazyButton
        > ReadPlus = AccessTools.FieldRefAccess<UIBaseCraftSelectionWindow, LazyButton>(
            "plusCraftButton"
        );
        private static readonly AccessTools.FieldRef<
            UIBaseCraftSelectionWindow,
            LazyButton
        > ReadMinus = AccessTools.FieldRefAccess<UIBaseCraftSelectionWindow, LazyButton>(
            "minusCraftButton"
        );
        private static readonly MethodInfo CanChangeWithGamepad = AccessTools.Method(
            typeof(UIBaseCraftSelectionWindow),
            "CanChangeCraftCountWithGamepad"
        );
        private static readonly MethodInfo ChangeCount = AccessTools.Method(
            typeof(UIBaseCraftSelectionWindow),
            "ChangeCraftCount",
            new[] { typeof(int) }
        );

        internal static void Register(
            UIBaseCraftSelectionWindow window,
            Dictionary<GameKey, Func<bool>> callbacks
        )
        {
            // These actions are LB/RB in the shipped bindings. Keep them local to the
            // quantity dialog; the parent crafting window uses the same shoulders.
            if (!callbacks.ContainsKey(GameKey.PrevTab))
                callbacks.Add(GameKey.PrevTab, () => OnPressed(window, false));
            if (!callbacks.ContainsKey(GameKey.NextTab))
                callbacks.Add(GameKey.NextTab, () => OnPressed(window, true));
        }

        internal static void AddTips(UIBaseCraftSelectionWindow window, List<LazyGameKeyTip> tips)
        {
            if (tips == null)
                return;

            LazyButton plus = ReadPlus(window);
            LazyButton minus = ReadMinus(window);
            if (minus != null && minus.gameObject.activeSelf)
                tips.Add(
                    new LazyGameKeyTip(GameKey.PrevTab, "-10", minus.interactable, translate: false)
                );
            if (plus != null && plus.gameObject.activeSelf)
                tips.Add(
                    new LazyGameKeyTip(GameKey.NextTab, "+10", plus.interactable, translate: false)
                );
        }

        private static bool OnPressed(UIBaseCraftSelectionWindow window, bool increase)
        {
            if (
                window == null
                || !window.IsShownAndTop
                || !LazyInput.IsGamepadActive
                || !LazyInput.IsInputActive()
                || !(bool)CanChangeWithGamepad.Invoke(window, null)
            )
                return false;

            UIBaseCraftSelectionWindowData data = ReadData(window);
            if (data?.CraftDefinition == null || data.WgoData == null)
                return false;

            LazyButton button = increase ? ReadPlus(window) : ReadMinus(window);
            if (button == null || !button.isActiveAndEnabled || !button.interactable)
                return false;

            // Treat simultaneous shoulders as neutral, independently of callback order.
            GameKey opposite = increase ? GameKey.PrevTab : GameKey.NextTab;
            if (LazyInput.GetKey(opposite))
                return true;

            bool editsQueue = window is UIFuelCraftWindow || window is UISingleCraftWindow;
            List<CraftElementBase> queue = editsQueue ? data.CraftQueue : null;
            int queueCount = queue?.Count ?? 0;
            if (queueCount > 2)
                return true;

            CraftElementBase target = queueCount > 0 ? queue[queueCount - 1] : null;
            CraftElementBase other = queueCount == 2 ? queue[0] : null;
            if (
                (queueCount > 0 && target == null)
                || (queueCount == 2 && other == null)
                || (
                    target != null
                    && (target.IsInfinite || target.CraftId != data.CraftDefinition.id)
                )
                || (other != null && (other.IsInfinite || other.CraftId != data.CraftDefinition.id))
            )
                return true;

            int otherCount = other?.Count ?? 0;
            int current =
                window is UIFuelCraftWindow && target != null
                    ? target.Count + otherCount
                    : data.CraftsCount;
            // New selections stay at one. Existing queues can be cancelled down to zero,
            // except for a craft that has already started.
            int minimum = target == null || queue[0].IsStarted ? 1 : 0;
            int maximum = 999;
            if (window is UIFuelCraftWindow && target != null)
                maximum += otherCount; // Vanilla caps the edited entry, not the queue total.

            if (increase)
                maximum = GetMaximum(data, target, other, maximum, window is UIFuelCraftWindow);

            int delta = CraftQuantityStep.GetDelta(current, minimum, maximum, increase);
            if (delta != 0)
            {
                // Reflection preserves virtual dispatch, including immediate fuel queue edits.
                // This also refreshes counters and button states without rebuilding navigation.
                ChangeCount.Invoke(window, new object[] { delta });
            }
            return true;
        }

        private static int GetMaximum(
            UIBaseCraftSelectionWindowData data,
            CraftElementBase target,
            CraftElementBase other,
            int maximum,
            bool isFuel
        )
        {
            // Queue edits retain the queue entry's ingredient variants. The visible picker
            // may have changed since that entry was created, so use its actual requirements.
            List<NeedItemData> requirements = target?.Requirements ?? data.CurrentNeedItems;
            if (requirements == null)
                return 0;

            MultiInventory inventory = data.WgoData.GetCraftableMultiInventory();
            int committedCount = (other?.Count ?? 0) + (target?.IsStarted == true ? 1 : 0);
            int otherPending =
                other == null ? 0 : Math.Max(0, other.Count - (other.IsStarted ? 1 : 0));
            maximum = LimitByIngredients(
                maximum,
                requirements,
                other?.Requirements,
                otherPending,
                committedCount,
                data.WgoData,
                id => inventory.GetTotalCount(id)
            );

            // Requirements drawn from the workstation inventory use a separate pool.
            maximum = LimitByIngredients(
                maximum,
                data.CraftDefinition.needItemsFromWgo,
                other?.Def.needItemsFromWgo,
                otherPending,
                committedCount,
                data.WgoData,
                id => data.WgoData.Inventory.Data.GetTotalCountInInventory(id)
            );

            if (isFuel && data.CraftDefinition.isFuelCraft)
            {
                ItemDef fuel = data.CraftDefinition.FuelItemDef;
                OutputPreview output = data.CraftDefinition.GetOutputPreview(data.WgoData);
                if (fuel == null || output == null || output.count <= 0)
                    return 0;

                Item storage = data.WgoData.Inventory?.Data;
                if (storage == null || data.WgoData.Definition == null)
                    return 0;

                long capacity =
                    (long)data.WgoData.Definition.emptyCellStackCount * storage.InventorySize;
                long remaining = Math.Max(0L, capacity - storage.GetTotalCountInInventory(fuel.id));
                // Include started crafts in this limit: their fuel output still needs space.
                maximum = CraftQuantityStep.LimitByResource(maximum, remaining, output.count, 0, 0);
            }
            return maximum;
        }

        private static int LimitByIngredients(
            int maximum,
            List<NeedItemData> requirements,
            List<NeedItemData> reservedRequirements,
            int reservedCrafts,
            int committedCount,
            WgoData wgo,
            Func<string, int> available
        )
        {
            Dictionary<string, long> needed = SumRequirements(requirements, wgo);
            Dictionary<string, long> reserved = SumRequirements(reservedRequirements, wgo);
            foreach (KeyValuePair<string, long> requirement in needed)
            {
                reserved.TryGetValue(requirement.Key, out long reservedPerCraft);
                maximum = CraftQuantityStep.LimitByResource(
                    maximum,
                    available(requirement.Key),
                    requirement.Value,
                    reservedPerCraft * reservedCrafts,
                    committedCount
                );
            }
            return maximum;
        }

        private static Dictionary<string, long> SumRequirements(
            List<NeedItemData> items,
            WgoData wgo
        )
        {
            var counts = new Dictionary<string, long>();
            if (items == null)
                return counts;

            foreach (NeedItemData item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.Id))
                    continue;
                int required = item.GetCount(wgo);
                if (required <= 0)
                    continue;
                counts.TryGetValue(item.Id, out long count);
                counts[item.Id] = count + required;
            }
            return counts;
        }
    }
}
