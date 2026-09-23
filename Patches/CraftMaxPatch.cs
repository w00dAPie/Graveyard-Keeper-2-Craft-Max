using System;
using HarmonyLib;
using LazyBearTechnology;
using TMPro;
using UnityEngine;

namespace GK2CraftMax.Patches
{
    [HarmonyPatch(typeof(UICraftSelectionWindow), "Redraw")]
    internal static class CraftSelectionWindowPatch
    {
        [HarmonyPostfix]
        private static void Postfix(UICraftSelectionWindow __instance)
        {
            CraftMaxHelper.HandleRedraw(__instance);
        }
    }

    [HarmonyPatch(typeof(UIFuelCraftWindow), "Redraw")]
    internal static class FuelCraftWindowPatch
    {
        [HarmonyPostfix]
        private static void Postfix(UIFuelCraftWindow __instance)
        {
            CraftMaxHelper.HandleRedraw(__instance);
        }
    }

    internal static class CraftMaxHelper
    {
        internal const string MaxButtonName = "GK2CraftMax_Button";

        internal static void HandleRedraw(
            UIBaseCraftSelectionWindow window)
        {
            if (window == null)
                return;

            UIBaseCraftSelectionWindowData data =
                Traverse.Create(window)
                    .Field("data")
                    .GetValue<UIBaseCraftSelectionWindowData>();

            if (data == null ||
                data.CraftDefinition == null)
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
            if (!(window is UIFuelCraftWindow) &&
                data.CraftDefinition.IsMultipleCraftsDisabled)
            {
                SetMaxButtonActive(window, false);
                return;
            }

            LazyButton plusButton =
                Traverse.Create(window)
                    .Field("plusCraftButton")
                    .GetValue<LazyButton>();

            if (plusButton == null)
                return;

            LazyButton maxButton =
                GetOrCreateMaxButton(
                    window,
                    plusButton
                );

            if (maxButton == null)
                return;

            maxButton.gameObject.SetActive(true);

            SetupGamepadNavigation(
                window,
                plusButton,
                maxButton
            );
        }

        private static LazyButton GetOrCreateMaxButton(
            UIBaseCraftSelectionWindow window,
            LazyButton plusButton)
        {
            Transform parent =
                plusButton.transform.parent;

            if (parent == null)
                return null;

            Transform existing =
                parent.Find(MaxButtonName);

            if (existing != null)
            {
                LazyButton existingButton =
                    existing.GetComponent<LazyButton>();

                if (existingButton != null)
                    return existingButton;
            }

            GameObject obj =
                UnityEngine.Object.Instantiate(
                    plusButton.gameObject,
                    parent
                );

            obj.name = MaxButtonName;

            /*
             * Plus-Icon des geklonten Buttons
             * ausblenden.
             */
            Transform icon =
                obj.transform.Find("Content/Icon");

            if (icon != null)
                icon.gameObject.SetActive(false);

            LazyButton maxButton =
                obj.GetComponent<LazyButton>();

            if (maxButton == null)
            {
                UnityEngine.Object.Destroy(obj);
                return null;
            }

            /*
             * Vanilla-Listener des geklonten
             * Plus-Buttons entfernen.
             */
            maxButton.onClick.RemoveAllListeners();

            /*
             * MAX rechts neben + positionieren.
             */
            RectTransform plusRect =
                plusButton.transform as RectTransform;

            RectTransform maxRect =
                obj.transform as RectTransform;

            if (plusRect != null &&
                maxRect != null)
            {
                maxRect.anchorMin =
                    plusRect.anchorMin;

                maxRect.anchorMax =
                    plusRect.anchorMax;

                maxRect.pivot =
                    plusRect.pivot;

                maxRect.sizeDelta =
                    plusRect.sizeDelta;

                maxRect.anchoredPosition =
                    plusRect.anchoredPosition +
                    new Vector2(
                        plusRect.rect.width + 10f,
                        0f
                    );
            }

            /*
             * MAX-Beschriftung erzeugen.
             */
            GameObject labelObject =
                new GameObject(
                    "GK2CraftMax_Label",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI)
                );

            labelObject.transform.SetParent(
                obj.transform,
                false
            );

            RectTransform labelRect =
                labelObject.GetComponent<RectTransform>();

            labelRect.anchorMin =
                Vector2.zero;

            labelRect.anchorMax =
                Vector2.one;

            labelRect.offsetMin =
                Vector2.zero;

            labelRect.offsetMax =
                Vector2.zero;

            labelRect.anchoredPosition =
                new Vector2(0f, -2f);

            TextMeshProUGUI label =
                labelObject.GetComponent<TextMeshProUGUI>();

            label.text = "MAX";

            label.alignment =
                TextAlignmentOptions.Center;

            label.fontSize = 18f;

            label.raycastTarget = false;

            label.color =
                new Color32(
                    255,
                    210,
                    45,
                    255
                );

            /*
             * EIN gemeinsamer MAX-Pfad.
             *
             * Maus und Controller landen beide
             * letztlich hier.
             */
            maxButton.onClick.AddListener(
                () =>
                {
                    SetMaximumCraftCount(window);
                }
            );

            /*
             * WICHTIG:
             *
             * Kein SyncOnSelectWithButton().
             *
             * Den A-Button behandeln wir zentral
             * über OnStartCraft.
             *
             * Dadurch kann A nicht gleichzeitig
             * MAX und Place auslösen.
             */
            return maxButton;
        }

        private static void SetupGamepadNavigation(
            UIBaseCraftSelectionWindow window,
            LazyButton plusButton,
            LazyButton maxButton)
        {
            if (!LazyInput.IsGamepadActive)
                return;

            GamepadNavigationController controller =
                Traverse.Create(window)
                    .Property("GamepadNavigationController")
                    .GetValue<GamepadNavigationController>();

            if (controller == null)
                return;

            GamepadNavigationItem plusNav =
                plusButton.GetComponent<GamepadNavigationItem>();

            GamepadNavigationItem maxNav =
                maxButton.GetComponent<GamepadNavigationItem>();

            if (plusNav == null ||
                maxNav == null)
            {
                return;
            }

            maxNav.Active = true;
            maxNav.enabled = true;

            var selectableItems =
                Traverse.Create(controller)
                    .Field("selectableItems")
                    .GetValue<
                        System.Collections.Generic
                            .List<GamepadNavigationItem>
                    >();

            if (selectableItems == null)
                return;

            /*
             * Kein ReinitItems().
             *
             * MAX wird direkt in die bereits
             * initialisierte Vanilla-Liste
             * aufgenommen.
             */
            if (!selectableItems.Contains(maxNav))
            {
                selectableItems.Add(maxNav);

                maxNav.Init(
                    selectableItems.Count - 1,
                    controller,
                    window.transform.lossyScale.x
                );
            }

            /*
             * Fuel verwendet Navigationsgruppe 1.
             *
             * Dort suchen wir das am weitesten
             * rechts liegende Vanilla-Item.
             */
            if (window is UIFuelCraftWindow)
            {
                GamepadNavigationItem rightItem = null;

                foreach (
                    GamepadNavigationItem item
                    in selectableItems)
                {
                    if (item == null ||
                        item == maxNav ||
                        !item.Active ||
                        !item.isActiveAndEnabled ||
                        item.group != 1)
                    {
                        continue;
                    }

                    if (rightItem == null ||
                        item.Pos.x > rightItem.Pos.x)
                    {
                        rightItem = item;
                    }
                }

                if (rightItem != null)
                {
                    maxNav.group =
                        rightItem.group;

                    rightItem.SetCustomDirectionItem(
                        GUIDirection.Right,
                        maxNav
                    );

                    maxNav.SetCustomDirectionItem(
                        GUIDirection.Left,
                        rightItem
                    );
                }
            }
            else
            {
                GamepadNavigationItem focused =
                    controller.FocusedItem;

                if (focused != null)
                {
                    int activeGroup = focused.group;

                    GamepadNavigationItem rightItem = null;

                    foreach (GamepadNavigationItem item in selectableItems)
                    {
                        if (item == null ||
                            item == maxNav ||
                            !item.Active ||
                            !item.isActiveAndEnabled ||
                            item.group != activeGroup)
                        {
                            continue;
                        }

                        if (rightItem == null ||
                            item.Pos.x > rightItem.Pos.x)
                        {
                            rightItem = item;
                        }
                    }

                    if (rightItem != null)
                    {
                        maxNav.group = activeGroup;

                        rightItem.SetCustomDirectionItem(
                            GUIDirection.Right,
                            maxNav

                        );

                        maxNav.SetCustomDirectionItem(
                            GUIDirection.Left,
                            rightItem
                        );
                    }
                }
            }
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
        internal static bool TryActivateFocusedMax(
            UIBaseCraftSelectionWindow window)
        {
            if (window == null ||
                !LazyInput.IsGamepadActive)
            {
                return false;
            }

            GamepadNavigationController controller =
                Traverse.Create(window)
                    .Property("GamepadNavigationController")
                    .GetValue<GamepadNavigationController>();

            if (controller == null)
                return false;

            GamepadNavigationItem focused =
                controller.FocusedItem;

            if (focused == null ||
                focused.gameObject == null ||
                focused.gameObject.name != MaxButtonName)
            {
                return false;
            }

            LazyButton maxButton =
                focused.GetComponent<LazyButton>();

            if (maxButton == null)
            {

                return false;
            }

            /*
             * Exakt denselben onClick ausführen,
             * den auch die Maus benutzt.
             */
            maxButton.onClick.Invoke();

            LazyButton startCraftButton =
                Traverse.Create(window)
                    .Field("startCraftButton")
                    .GetValue<LazyButton>();

            if (startCraftButton != null &&
                startCraftButton.interactable)
            {
                Traverse.Create(window)
                    .Method("OnStartCraftPressed")
                    .GetValue();
            }

            return true;
        }

        private static void SetMaximumCraftCount(
            UIBaseCraftSelectionWindow window)
        {
            UIBaseCraftSelectionWindowData data =
                Traverse.Create(window)
                    .Field("data")
                    .GetValue<UIBaseCraftSelectionWindowData>();

            if (data == null)
                return;

            if (data.CraftItemCellsData == null ||
                data.CraftItemCellsData.Count == 0)
            {
                return;
            }

            int maximum = 999;

            /*
             * Maximale Anzahl anhand der
             * vorhandenen Zutaten bestimmen.
             */
            foreach (
                UICraftItemCellData cell
                in data.CraftItemCellsData)
            {
                if (cell == null ||
                    cell.currentItem == null ||
                    cell.MultiInventory == null)
                {
                    continue;
                }

                int required =
                    cell.currentItem.GetCount(
                        data.WgoData
                    );

                if (required <= 0)
                    continue;

                int available =
                    cell.MultiInventory
                        .GetTotalCount(
                            cell.currentItem.Id
                        );

                int possible =
                    available / required;

                maximum =
                    Math.Min(
                        maximum,
                        possible
                    );
            }

            /*
             * Fuel-Crafting:
             *
             * Zusätzlich prüfen, wie viel
             * Platz noch im Fuel-Container
             * vorhanden ist.
             */
            if (window is UIFuelCraftWindow &&
                data.CraftDefinition != null &&
                data.CraftDefinition.isFuelCraft &&
                data.WgoData != null &&
                data.WgoData.Definition != null &&
                data.WgoData.Inventory != null &&
                data.WgoData.Inventory.Data != null)
            {
                ItemDef fuelItemDef =
                    data.CraftDefinition.FuelItemDef;

                OutputPreview outputPreview =
                    data.CraftDefinition
                        .GetOutputPreview(
                            data.WgoData
                        );

                if (fuelItemDef != null &&
                    outputPreview != null &&
                    outputPreview.count > 0)
                {
                    int currentFuel =
                        data.WgoData
                            .Inventory
                            .Data
                            .GetTotalCountInInventory(
                                fuelItemDef.id
                            );

                    int capacity =
                        data.WgoData
                            .Definition
                            .emptyCellStackCount
                        *
                        data.WgoData
                            .Inventory
                            .Data
                            .InventorySize;

                    int remainingCapacity =
                        Math.Max(
                            0,
                            capacity - currentFuel
                        );

                    int fuelPerCraft =
                        outputPreview.count;

                    /*
                     * Bewusst Floor.
                     *
                     * 210 Platz / 20 Fuel
                     * = 10 vollständige Crafts.
                     */
                    int fuelMaximum =
                        remainingCapacity > 0
                            ? remainingCapacity /
                              fuelPerCraft
                            : 0;

                    maximum =
                        Math.Min(
                            maximum,
                            fuelMaximum
                        );

                }
            }

            if (window is UIFuelCraftWindow)
            {
                maximum =
                    Math.Max(
                        0,
                        Math.Min(
                            999,
                            maximum
                        )
                    );
            }
            else
            {
                maximum =
                    Math.Max(
                        1,
                        Math.Min(
                            999,
                            maximum
                        )
                    );
            }

            /*
             * Kein vollständiger Fuel-Craft
             * mehr möglich.
             */
            if (window is UIFuelCraftWindow &&
                maximum <= 0)
            {
                return;
            }

            int delta =
                maximum - data.CraftsCount;

            if (delta == 0)
                return;

            /*
             * Normales Crafting bzw. vorhandene
             * Fuel-Queue über Vanilla.
             */
            Traverse.Create(window)
                .Method(
                    "ChangeCraftCount",
                    delta
                )
                .GetValue();
        }

        private static void SetMaxButtonActive(
            UIBaseCraftSelectionWindow window,
            bool active)
        {
            if (window == null)
                return;

            LazyButton plusButton =
                Traverse.Create(window)
                    .Field("plusCraftButton")
                    .GetValue<LazyButton>();

            if (plusButton == null ||
                plusButton.transform.parent == null)
            {
                return;
            }

            Transform existing =
                plusButton.transform.parent
                    .Find(MaxButtonName);

            if (existing != null)
            {
                existing.gameObject.SetActive(active);
            }
        }
    }

    /*
     * ZENTRALE A-BUTTON-LOGIK
     *
     * MAX fokussiert:
     *     A -> MAX
     *     Vanilla Craft/Place wird blockiert.
     *
     * MAX nicht fokussiert:
     *     Vanilla OnStartCraft läuft unverändert.
     */
    [HarmonyPatch(
        typeof(UIBaseCraftSelectionWindow),
        "OnStartCraft"
    )]
    internal static class CraftMaxStartCraftPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(
            UIBaseCraftSelectionWindow __instance,
            ref bool __result)
        {
            if (!CraftMaxHelper.TryActivateFocusedMax(
                    __instance))
            {
                /*
                 * MAX nicht fokussiert.
                 *
                 * Vanilla:
                 * OnStartCraftPressed()
                 * -> Craft / Place
                 */
                return true;
            }

            /*
             * MAX wurde ausgeführt.
             *
             * GameKey gilt als verarbeitet,
             * aber das originale OnStartCraft()
             * darf NICHT mehr laufen.
             */
            __result = true;

            return false;
        }
    }    
}