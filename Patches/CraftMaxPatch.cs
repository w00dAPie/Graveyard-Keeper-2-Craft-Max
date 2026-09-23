using System;
using HarmonyLib;
using LazyBearTechnology;
using TMPro;
using UnityEngine;

namespace GK2CraftMax.Patches
{
    // Normales Crafting-Fenster
    [HarmonyPatch(typeof(UICraftSelectionWindow), "Redraw")]
    internal static class CraftSelectionWindowPatch
    {
        [HarmonyPostfix]
        private static void Postfix(
            UICraftSelectionWindow __instance)
        {
            CraftMaxHelper.HandleRedraw(__instance);
        }
    }


    // Fuel-Crafting, z.B. Feuerholzschuppen
    [HarmonyPatch(typeof(UIFuelCraftWindow), "Redraw")]
    internal static class FuelCraftWindowPatch
    {
        [HarmonyPostfix]
        private static void Postfix(
            UIFuelCraftWindow __instance)
        {
            CraftMaxHelper.HandleRedraw(__instance);
        }
    }


    internal static class CraftMaxHelper
    {
        private const string MaxButtonName =
            "GK2CraftMax_Button";


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
             * Bei normalen Craft-Fenstern respektieren wir
             * IsMultipleCraftsDisabled.
             *
             * UIFuelCraftWindow hat eigene Logik für die
             * Mengensteuerung und wird deshalb separat behandelt.
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

        }


        private static LazyButton GetOrCreateMaxButton(
            UIBaseCraftSelectionWindow window,
            LazyButton plusButton)
        {
            Transform parent =
                plusButton.transform.parent;

            if (parent == null)
                return null;

            /*
             * Falls MAX bereits existiert, verwenden wir
             * denselben Button wieder.
             */
            Transform existing =
                parent.Find(MaxButtonName);

            if (existing != null)
            {
                LazyButton existingButton =
                    existing.GetComponent<LazyButton>();

                if (existingButton != null)
                {
                    return existingButton;
                }
            }

            /*
             * Vanilla-Plus-Button klonen.
             */
            GameObject obj =
                UnityEngine.Object.Instantiate(
                    plusButton.gameObject,
                    parent
                );

            obj.name = MaxButtonName;

            /*
             * Nur das Plus-Icon verstecken.
             * Der Vanilla-Hintergrund bleibt erhalten.
             */
            Transform icon =
                obj.transform.Find("Content/Icon");

            if (icon != null)
            {
                icon.gameObject.SetActive(false);
            }

            LazyButton maxButton =
                obj.GetComponent<LazyButton>();

            if (maxButton == null)
            {

                UnityEngine.Object.Destroy(obj);
                return null;
            }

            /*
             * Listener des geklonten Plus-Buttons entfernen.
             */
            maxButton.onClick.RemoveAllListeners();


            /*
             * MAX rechts neben den Plus-Button setzen.
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
             * Eigenes MAX-Label.
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
             * Unser eigener Click-Handler.
             */
            maxButton.onClick.AddListener(
                () => SetMaximumCraftCount(window)
            );

            return maxButton;
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
            * Maximale Anzahl anhand der verfügbaren Zutaten.
            */
            foreach (UICraftItemCellData cell
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
                    cell.MultiInventory.GetTotalCount(
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
            * Zusätzlich zum Zutatenlimit darf nur so viel
            * hergestellt werden, wie noch in den Fuel-Container passt.
            *
            * Vanilla berechnet die Kapazität als:
            *
            * emptyCellStackCount * InventorySize
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
                    data.CraftDefinition.GetOutputPreview(
                        data.WgoData
                    );

                if (fuelItemDef != null &&
                    outputPreview != null &&
                    outputPreview.count > 0)
                {
                    int currentFuel =
                        data.WgoData.Inventory.Data
                            .GetTotalCountInInventory(
                                fuelItemDef.id
                            );

                    int capacity =
                        data.WgoData.Definition.emptyCellStackCount *
                        data.WgoData.Inventory.Data.InventorySize;

                    int remainingCapacity =
                        Math.Max(
                            0,
                            capacity - currentFuel
                        );

                    int fuelPerCraft =
                        outputPreview.count;

                    /*
                    * Aufrunden ist gewollt.
                    *
                    * Beispiel:
                    *
                    * 500 Kapazität
                    * 290 vorhanden
                    * 210 frei
                    * 20 Fuel pro Craft
                    *
                    * 210 / 20 = 10,5
                    * => 11 Crafts
                    */
                    int fuelMaximum =
                        remainingCapacity > 0
                            ? remainingCapacity / fuelPerCraft
                            : 0;

                    maximum =
                        Math.Min(
                            maximum,
                            fuelMaximum
                        );
                }
            }

            /*
            * Normale Crafts beginnen bei mindestens 1.
            *
            * Fuel darf 0 ergeben, wenn der Container
            * bereits vollständig gefüllt ist.
            */
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
            * Ist der Fuel-Container bereits voll,
            * verändern wir die Auswahl nicht.
            */
            if (window is UIFuelCraftWindow &&
                maximum <= 0)
            {

                return;
            }

            /*
            * Differenz zur aktuell gewählten Menge.
            */

            if (window is UIFuelCraftWindow)
            {

                for (int i = 0; i < data.CraftQueue.Count; i++)
                {
                    CraftElementBase element =
                        data.CraftQueue[i];

                }
            }

            int delta =
                maximum - data.CraftsCount;

            if (delta == 0)
                return;

            /*
            * Vanillas ChangeCraftCount verwenden.
            *
            * Bei UIFuelCraftWindow wird dadurch dessen
            * eigene Queue-Logik verwendet.
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
                plusButton.transform.parent.Find(
                    MaxButtonName
                );

            if (existing != null)
            {
                existing.gameObject.SetActive(
                    active
                );
            }
        }


        private static string GetPath(
            Transform transform)
        {
            if (transform == null)
                return "NULL";

            string path =
                transform.name;

            while (transform.parent != null)
            {
                transform =
                    transform.parent;

                path =
                    transform.name +
                    "/" +
                    path;
            }

            return path;
        }
    }
}