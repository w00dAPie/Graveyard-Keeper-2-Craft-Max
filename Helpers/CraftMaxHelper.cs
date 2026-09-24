using System;
using HarmonyLib;
using LazyBearTechnology;
using UnityEngine;

namespace GK2CraftMax.Helpers
{
    internal static class CraftMaxHelper
    {
        internal const string MaxButtonName = "GK2CraftMax_Button";

        internal static void HandleRedraw(UIBaseCraftSelectionWindow window)
        {
            if (window == null)
                return;

            UIBaseCraftSelectionWindowData data = Traverse
                .Create(window)
                .Field("data")
                .GetValue<UIBaseCraftSelectionWindowData>();

            if (data == null || data.CraftDefinition == null)
            {
                SetMaxButtonActive(window, false);
                return;
            }

            /*
             * Normale Crafts dürfen MAX nur bekommen,
             * wenn Vanilla mehrere Crafts erlaubt.
             *
             * Fuel-Crafting wird separat behandelt.
             */
            if (!(window is UIFuelCraftWindow) && data.CraftDefinition.IsMultipleCraftsDisabled)
            {
                SetMaxButtonActive(window, false);
                return;
            }

            LazyButton plusButton = Traverse
                .Create(window)
                .Field("plusCraftButton")
                .GetValue<LazyButton>();

            if (plusButton == null)
                return;

            LazyButton maxButton = GetOrCreateMaxButton(window, plusButton);

            if (maxButton == null)
                return;

            maxButton.gameObject.SetActive(true);

            SetupGamepadNavigation(window, maxButton);
        }

        private static LazyButton GetOrCreateMaxButton(
            UIBaseCraftSelectionWindow window,
            LazyButton plusButton
        )
        {
            LazyButton maxButton = MaxButtonHelper.CloneButton(
                plusButton,
                MaxButtonName,
                () => SetMaximumCraftCount(window)
            );

            if (maxButton == null)
                return null;

            /*
             * Plus-Icon des geklonten Buttons
             * ausblenden.
             */
            Transform icon = maxButton.transform.Find("Content/Icon");

            if (icon != null)
                icon.gameObject.SetActive(false);

            MaxButtonHelper.PositionRightOf(maxButton, plusButton);

            /*
             * Beim normalen Craft-Button gibt es
             * kein brauchbares Textlabel.
             */
            MaxButtonHelper.CreateLabel(maxButton, "MAX");

            return maxButton;
        }

        private static void SetupGamepadNavigation(
            UIBaseCraftSelectionWindow window,
            LazyButton maxButton
        )
        {
            if (!LazyInput.IsGamepadActive)
                return;

            GamepadNavigationController controller = GamepadNavigationHelper.GetController(window);

            if (controller == null)
                return;

            GamepadNavigationItem maxNav = maxButton.GetComponent<GamepadNavigationItem>();

            if (maxNav == null)
                return;

            maxNav.Active = true;
            maxNav.enabled = true;

            var selectableItems = GamepadNavigationHelper.GetItems(controller);

            if (selectableItems == null)
                return;

            GamepadNavigationHelper.Register(controller, maxNav, window.transform.lossyScale.x);

            /*
             * Fuel Crafting:
             *
             * Result -> Zutaten -> MAX -> Result
             */
            if (window is UIFuelCraftWindow)
            {
                const int fuelGroup = 1;

                GamepadNavigationItem leftIngredient = null;
                GamepadNavigationItem rightIngredient = null;

                UICraftItemCell[] craftItemCells = window.GetComponentsInChildren<UICraftItemCell>(
                    true
                );

                foreach (UICraftItemCell cell in craftItemCells)
                {
                    if (cell == null || !cell.gameObject.activeInHierarchy)
                        continue;

                    GamepadNavigationItem ingredientNav = cell.GamepadNavigationItem;

                    if (
                        ingredientNav == null
                        || !ingredientNav.Active
                        || !ingredientNav.isActiveAndEnabled
                        || ingredientNav.group != fuelGroup
                    )
                    {
                        continue;
                    }

                    if (leftIngredient == null || ingredientNav.Pos.x < leftIngredient.Pos.x)
                    {
                        leftIngredient = ingredientNav;
                    }

                    if (rightIngredient == null || ingredientNav.Pos.x > rightIngredient.Pos.x)
                    {
                        rightIngredient = ingredientNav;
                    }
                }

                if (leftIngredient == null || rightIngredient == null)
                {
                    return;
                }

                GamepadNavigationItem craftResult = null;

                foreach (GamepadNavigationItem item in selectableItems)
                {
                    if (
                        item == null
                        || item == maxNav
                        || item == leftIngredient
                        || item == rightIngredient
                        || !item.Active
                        || !item.isActiveAndEnabled
                    )
                    {
                        continue;
                    }

                    /*
                     * Beim Fuel-Fenster kann das Result
                     * in einer anderen Navigationsgruppe liegen.
                     */
                    if (
                        item.Pos.x < leftIngredient.Pos.x
                        && Mathf.Abs(item.Pos.y - leftIngredient.Pos.y) < 10f
                    )
                    {
                        if (craftResult == null || item.Pos.x > craftResult.Pos.x)
                        {
                            craftResult = item;
                        }
                    }
                }

                if (craftResult == null)
                {
                    return;
                }

                maxNav.group = fuelGroup;

                GamepadNavigationHelper.Link(rightIngredient, GUIDirection.Right, maxNav);

                GamepadNavigationHelper.Link(maxNav, GUIDirection.Right, craftResult);

                GamepadNavigationHelper.Link(craftResult, GUIDirection.Left, maxNav);

                GamepadNavigationHelper.Link(maxNav, GUIDirection.Left, rightIngredient);

                return;
            }

            /*
             * Normales Crafting + Single Craft:
             *
             * Workbench
             * Kiln
             * Compost Pile
             */
            if (!(window is UICraftSelectionWindow) && !(window is UISingleCraftWindow))
            {
                return;
            }

            GamepadNavigationItem focused = controller.FocusedItem;

            if (focused == null)
                return;

            int activeGroup = focused.group;

            GamepadNavigationItem leftNormalIngredient = null;
            GamepadNavigationItem rightNormalIngredient = null;

            UICraftItemCell[] normalCraftItemCells =
                window.GetComponentsInChildren<UICraftItemCell>(true);

            foreach (UICraftItemCell cell in normalCraftItemCells)
            {
                if (cell == null || !cell.gameObject.activeInHierarchy)
                    continue;

                GamepadNavigationItem ingredientNav = cell.GamepadNavigationItem;

                if (
                    ingredientNav == null
                    || !ingredientNav.Active
                    || !ingredientNav.isActiveAndEnabled
                    || ingredientNav.group != activeGroup
                )
                {
                    continue;
                }

                if (
                    leftNormalIngredient == null
                    || ingredientNav.Pos.x < leftNormalIngredient.Pos.x
                )
                {
                    leftNormalIngredient = ingredientNav;
                }

                if (
                    rightNormalIngredient == null
                    || ingredientNav.Pos.x > rightNormalIngredient.Pos.x
                )
                {
                    rightNormalIngredient = ingredientNav;
                }
            }

            if (leftNormalIngredient == null || rightNormalIngredient == null)
            {
                return;
            }

            maxNav.group = activeGroup;

            GamepadNavigationItem normalCraftResult = null;

            foreach (GamepadNavigationItem item in selectableItems)
            {
                if (
                    item == null
                    || item == maxNav
                    || item == leftNormalIngredient
                    || item == rightNormalIngredient
                    || !item.Active
                    || !item.isActiveAndEnabled
                    || item.group != activeGroup
                )
                {
                    continue;
                }

                if (
                    item.Pos.x < leftNormalIngredient.Pos.x
                    && Mathf.Abs(item.Pos.y - leftNormalIngredient.Pos.y) < 10f
                )
                {
                    if (normalCraftResult == null || item.Pos.x > normalCraftResult.Pos.x)
                    {
                        normalCraftResult = item;
                    }
                }
            }

            if (normalCraftResult == null)
            {
                return;
            }

            /*
             * Ring schließen:
             *
             * Result -> Zutat 1 -> ... -> letzte Zutat
             * -> MAX -> Result
             */
            GamepadNavigationHelper.Link(rightNormalIngredient, GUIDirection.Right, maxNav);

            GamepadNavigationHelper.Link(maxNav, GUIDirection.Right, normalCraftResult);

            GamepadNavigationHelper.Link(normalCraftResult, GUIDirection.Left, maxNav);

            GamepadNavigationHelper.Link(maxNav, GUIDirection.Left, rightNormalIngredient);
        }

        /*
         * Wird vom OnStartCraft-Patch benutzt.
         *
         * true:
         * MAX ist fokussiert und wurde ausgeführt.
         *
         * false:
         * MAX ist nicht fokussiert.
         * Vanilla darf normal craften/place ausführen.
         */
        internal static bool TryActivateFocusedMax(UIBaseCraftSelectionWindow window)
        {
            if (window == null || !LazyInput.IsGamepadActive)
            {
                return false;
            }

            GamepadNavigationController controller = GamepadNavigationHelper.GetController(window);

            if (controller == null)
                return false;

            GamepadNavigationItem focused = controller.FocusedItem;

            if (
                focused == null
                || focused.gameObject == null
                || focused.gameObject.name != MaxButtonName
            )
            {
                return false;
            }

            LazyButton maxButton = focused.GetComponent<LazyButton>();

            if (maxButton == null)
            {
                return false;
            }

            /*
             * Exakt denselben onClick ausführen,
             * den auch die Maus benutzt.
             */
            maxButton.onClick.Invoke();

            LazyButton startCraftButton = Traverse
                .Create(window)
                .Field("startCraftButton")
                .GetValue<LazyButton>();

            if (startCraftButton != null && startCraftButton.interactable)
            {
                Traverse.Create(window).Method("OnStartCraftPressed").GetValue();
            }

            return true;
        }

        private static void SetMaximumCraftCount(UIBaseCraftSelectionWindow window)
        {
            UIBaseCraftSelectionWindowData data = Traverse
                .Create(window)
                .Field("data")
                .GetValue<UIBaseCraftSelectionWindowData>();

            if (data == null)
                return;

            if (data.CraftItemCellsData == null || data.CraftItemCellsData.Count == 0)
            {
                return;
            }

            int maximum = 999;

            /*
             * Maximale Anzahl anhand der
             * vorhandenen Zutaten bestimmen.
             */
            foreach (UICraftItemCellData cell in data.CraftItemCellsData)
            {
                if (cell == null || cell.currentItem == null || cell.MultiInventory == null)
                {
                    continue;
                }

                int required = cell.currentItem.GetCount(data.WgoData);

                if (required <= 0)
                    continue;

                int available = cell.MultiInventory.GetTotalCount(cell.currentItem.Id);

                int possible = available / required;

                maximum = Math.Min(maximum, possible);
            }

            /*
             * Fuel-Crafting:
             *
             * Zusätzlich prüfen, wie viel
             * Platz noch im Fuel-Container
             * vorhanden ist.
             */
            if (
                window is UIFuelCraftWindow
                && data.CraftDefinition != null
                && data.CraftDefinition.isFuelCraft
                && data.WgoData != null
                && data.WgoData.Definition != null
                && data.WgoData.Inventory != null
                && data.WgoData.Inventory.Data != null
            )
            {
                ItemDef fuelItemDef = data.CraftDefinition.FuelItemDef;

                OutputPreview outputPreview = data.CraftDefinition.GetOutputPreview(data.WgoData);

                if (fuelItemDef != null && outputPreview != null && outputPreview.count > 0)
                {
                    int currentFuel = data.WgoData.Inventory.Data.GetTotalCountInInventory(
                        fuelItemDef.id
                    );

                    int capacity =
                        data.WgoData.Definition.emptyCellStackCount
                        * data.WgoData.Inventory.Data.InventorySize;

                    int remainingCapacity = Math.Max(0, capacity - currentFuel);

                    int fuelPerCraft = outputPreview.count;

                    /*
                     * Bewusst Floor.
                     *
                     * 210 Platz / 20 Fuel
                     * = 10 vollständige Crafts.
                     */
                    int fuelMaximum = remainingCapacity > 0 ? remainingCapacity / fuelPerCraft : 0;

                    maximum = Math.Min(maximum, fuelMaximum);
                }
            }

            if (window is UIFuelCraftWindow)
            {
                maximum = Math.Max(0, Math.Min(999, maximum));
            }
            else
            {
                maximum = Math.Max(1, Math.Min(999, maximum));
            }

            /*
             * Kein vollständiger Fuel-Craft
             * mehr möglich.
             */
            if (window is UIFuelCraftWindow && maximum <= 0)
            {
                return;
            }

            int delta = maximum - data.CraftsCount;

            if (delta == 0)
                return;

            /*
             * Normales Crafting bzw. vorhandene
             * Fuel-Queue über Vanilla.
             */
            Traverse.Create(window).Method("ChangeCraftCount", delta).GetValue();
        }

        private static void SetMaxButtonActive(UIBaseCraftSelectionWindow window, bool active)
        {
            if (window == null)
                return;

            LazyButton plusButton = Traverse
                .Create(window)
                .Field("plusCraftButton")
                .GetValue<LazyButton>();

            if (plusButton == null || plusButton.transform.parent == null)
            {
                return;
            }

            Transform existing = plusButton.transform.parent.Find(MaxButtonName);

            if (existing != null)
            {
                existing.gameObject.SetActive(active);
            }
        }
    }
}
