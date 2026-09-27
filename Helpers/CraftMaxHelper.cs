using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using LazyBearTechnology;
using UnityEngine;

namespace GK2CraftMax.Helpers
{
    internal static class CraftMaxHelper
    {
        internal const string MaxButtonName = "GK2CraftMax_Button";
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
        > ReadPlusButton = AccessTools.FieldRefAccess<UIBaseCraftSelectionWindow, LazyButton>(
            "plusCraftButton"
        );
        private static readonly AccessTools.FieldRef<
            UIBaseCraftSelectionWindow,
            LazyButton
        > ReadStartButton = AccessTools.FieldRefAccess<UIBaseCraftSelectionWindow, LazyButton>(
            "startCraftButton"
        );

        // Invoke retains virtual dispatch to SingleCraft/Fuel overrides.
        private static readonly MethodInfo StartCraft = AccessTools.Method(
            typeof(UIBaseCraftSelectionWindow),
            "OnStartCraftPressed"
        );
        private static readonly MethodInfo ChangeCraftCount = AccessTools.Method(
            typeof(UIBaseCraftSelectionWindow),
            "ChangeCraftCount",
            [typeof(int)]
        );

        internal static void HandleRedraw(UIBaseCraftSelectionWindow window)
        {
            if (window == null)
                return;

            UIBaseCraftSelectionWindowData data = ReadData(window);

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

            LazyButton plusButton = ReadPlusButton(window);

            if (plusButton == null)
                return;

            LazyButton maxButton = GetOrCreateMaxButton(window, plusButton);

            if (maxButton == null)
                return;

            if (!maxButton.gameObject.activeSelf)
                maxButton.gameObject.SetActive(true);

            MaxButtonState state = MaxButtonState.For(window);
            try
            {
                SetupGamepadNavigation(window, maxButton, state);
            }
            finally
            {
                // Keep capacity, not references to a previous window hierarchy.
                state.CraftCells.Clear();
            }
        }

        private static LazyButton GetOrCreateMaxButton(
            UIBaseCraftSelectionWindow window,
            LazyButton plusButton
        )
        {
            MaxButtonState state = MaxButtonState.For(window);
            LazyButton maxButton = state.Resolve(plusButton, MaxButtonName);
            if (maxButton == null)
            {
                maxButton = MaxButtonHelper.CloneButton(plusButton, MaxButtonName);
                if (maxButton != null)
                    BindMaximumClick(window, maxButton);
                state.Button = maxButton;
            }

            if (maxButton == null)
                return null;

            /*
             * Plus-Icon des geklonten Buttons
             * ausblenden.
             */
            if (!state.Initialized)
            {
                state.Icon = maxButton.transform.Find("Content/Icon");
                if (state.Icon != null && state.Icon.gameObject.activeSelf)
                    state.Icon.gameObject.SetActive(false);
            }

            MaxButtonHelper.PositionRightOf(maxButton, plusButton);

            /*
             * Beim normalen Craft-Button gibt es
             * kein brauchbares Textlabel.
             */
            if (!state.Initialized || state.Label == null)
                state.Label = MaxButtonHelper.CreateLabel(maxButton, "MAX");
            state.Initialized = true;

            return maxButton;
        }

        private static void BindMaximumClick(UIBaseCraftSelectionWindow window, LazyButton button)
        {
            // This capturing delegate is allocated only when a button is created.
            button.onClick.AddListener(() => SetMaximumCraftCount(window));
        }

        private static void SetupGamepadNavigation(
            UIBaseCraftSelectionWindow window,
            LazyButton maxButton,
            MaxButtonState state
        )
        {
            if (window == null || maxButton == null || state == null)
                return;

            /*
             * Alten CraftMax-Splice entfernen.
             *
             * Vanilla besitzt danach wieder seine originale Navigation.
             */
            state.Insertion.Restore();

            if (!LazyInput.IsGamepadActive)
            {
                if (state.Navigation != null)
                    state.Navigation.Active = false;

                return;
            }

            GamepadNavigationController controller = GamepadNavigationHelper.GetController(window);

            if (controller == null)
                return;

            if (state.Navigation == null)
                state.Navigation = MaxButtonHelper.EnsureNavigationItem(maxButton);

            GamepadNavigationItem maxNav = state.Navigation;

            if (maxNav == null)
                return;

            maxNav.Active = false;

            List<GamepadNavigationItem> selectableItems = GamepadNavigationHelper.GetItems(
                controller
            );

            if (selectableItems == null)
                return;

            GamepadNavigationHelper.Register(controller, maxNav, window.transform.lossyScale.x);

            /*
             * Alle sichtbaren Craft-Item-Zellen bestimmen.
             *
             * Wichtig:
             * KEIN group == focused.group Filter mehr.
             *
             * Gerade solche Annahmen können bei speziellen Fenstern
             * wie dem Distillation Cube Slots 2/3 herausfiltern.
             */
            List<UICraftItemCell> craftItemCells = state.CraftCells;

            window.GetComponentsInChildren(true, craftItemCells);

            GamepadNavigationItem leftIngredient = null;
            GamepadNavigationItem rightIngredient = null;

            foreach (UICraftItemCell cell in craftItemCells)
            {
                if (cell == null || !cell.gameObject.activeInHierarchy)
                {
                    continue;
                }

                GamepadNavigationItem nav = cell.GamepadNavigationItem;

                if (!GamepadNavigationHelper.IsUsable(nav) || !selectableItems.Contains(nav))
                {
                    continue;
                }

                if (leftIngredient == null || nav.Pos.x < leftIngredient.Pos.x)
                {
                    leftIngredient = nav;
                }

                if (rightIngredient == null || nav.Pos.x > rightIngredient.Pos.x)
                {
                    rightIngredient = nav;
                }
            }

            if (leftIngredient == null || rightIngredient == null)
            {
                maxNav.Active = false;
                return;
            }

            /*
             * Craft/Result liegt bei diesen Fenstern links vor der
             * ersten Zutatenzelle.
             *
             * Wir wählen das nächstgelegene aktive Navigationselement
             * links auf derselben horizontalen Reihe.
             */
            GamepadNavigationItem craftResult = null;

            foreach (GamepadNavigationItem item in selectableItems)
            {
                if (
                    item == null
                    || item == maxNav
                    || item == leftIngredient
                    || item == rightIngredient
                    || !GamepadNavigationHelper.IsUsable(item)
                )
                {
                    continue;
                }

                if (
                    item.Pos.x < leftIngredient.Pos.x
                    && Mathf.Abs(item.Pos.y - leftIngredient.Pos.y) < 10f
                )
                {
                    /*
                     * Den nächsten Kandidaten links von Zutat 1 wählen.
                     */
                    if (craftResult == null || item.Pos.x > craftResult.Pos.x)
                    {
                        craftResult = item;
                    }
                }
            }

            if (craftResult == null)
            {
                maxNav.Active = false;
                return;
            }

            /*
             * Vanilla besitzt weiterhin:
             *
             * Craft -> Zutat1 -> Zutat2 -> Zutat3
             *
             * Wir ergänzen ausschließlich:
             *
             * Zutat3 -> MAX -> Craft
             *
             * und rückwärts:
             *
             * Craft -> MAX -> Zutat3
             */
            state.Insertion.AttachRing(controller, craftResult, rightIngredient, maxNav);

            if (!state.Insertion.IsAttached)
            {
                maxNav.Active = false;
                return;
            }

            GamepadNavigationHelper.BindButtonPress(maxNav, maxButton);
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

            LazyButton startCraftButton = ReadStartButton(window);

            if (startCraftButton != null && startCraftButton.interactable)
            {
                StartCraft.Invoke(window, null);
            }

            return true;
        }

        private static void SetMaximumCraftCount(UIBaseCraftSelectionWindow window)
        {
            UIBaseCraftSelectionWindowData data = ReadData(window);

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
            ChangeCraftCount.Invoke(window, [delta]);
        }

        private static void SetMaxButtonActive(UIBaseCraftSelectionWindow window, bool active)
        {
            if (window == null)
                return;

            LazyButton plusButton = ReadPlusButton(window);

            if (plusButton == null || plusButton.transform.parent == null)
                return;

            MaxButtonState state = MaxButtonState.For(window);

            LazyButton existing = state.Resolve(plusButton, MaxButtonName);

            if (!active)
            {
                /*
                 * MAX verschwindet:
                 * zuerst ausschließlich unsere Navigationsänderung entfernen.
                 */
                state.Insertion.Restore();

                if (state.Navigation != null)
                    state.Navigation.Active = false;

                state.Context = null;
                state.CraftCells.Clear();
            }

            if (existing != null && existing.gameObject.activeSelf != active)
            {
                existing.gameObject.SetActive(active);
            }
        }
    }
}
