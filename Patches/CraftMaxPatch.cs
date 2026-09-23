using System;
using HarmonyLib;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2CraftMax.Patches
{
    [HarmonyPatch(typeof(UICraftSelectionWindow))]
    internal static class CraftMaxPatch
    {
        private const string MaxButtonName = "GK2CraftMax_Button";

        [HarmonyPostfix]
        [HarmonyPatch("Redraw")]
        private static void RedrawPostfix(UICraftSelectionWindow __instance)
        {
            if (__instance == null)
                return;

            UIBaseCraftSelectionWindowData data =
                Traverse.Create(__instance)
                    .Field("data")
                    .GetValue<UIBaseCraftSelectionWindowData>();

            if (data == null ||
                data.CraftDefinition == null ||
                data.CraftDefinition.IsMultipleCraftsDisabled)
            {
                SetMaxButtonActive(__instance, false);
                return;
            }

            LazyButton plusButton =
                Traverse.Create(__instance)
                    .Field("plusCraftButton")
                    .GetValue<LazyButton>();

            if (plusButton == null)
                return;

            LazyButton maxButton = GetOrCreateMaxButton(
                __instance,
                plusButton
            );

            maxButton.gameObject.SetActive(true);
        }

        private static LazyButton GetOrCreateMaxButton(
            UICraftSelectionWindow window,
            LazyButton plusButton)
        {
            Transform parent = plusButton.transform.parent;

            Transform existing = parent.Find(MaxButtonName);

            if (existing != null)
            {
                LazyButton existingButton =
                    existing.GetComponent<LazyButton>();

                if (existingButton != null)
                    return existingButton;
            }

            GameObject obj = UnityEngine.Object.Instantiate(
                plusButton.gameObject,
                parent
            );

            Transform icon = obj.transform.Find("Content/Icon");

            if (icon != null)
            {
                icon.gameObject.SetActive(false);
            }
            

            LazyButton maxButton =
                obj.GetComponent<LazyButton>();

            // Listener des geklonten "+"-Buttons entfernen.
            maxButton.onClick.RemoveAllListeners();

            // Position zunächst rechts neben dem Plus-Button.
            RectTransform plusRect =
                plusButton.transform as RectTransform;

            RectTransform maxRect =
                obj.transform as RectTransform;

            if (plusRect != null && maxRect != null)
            {
                maxRect.anchorMin = plusRect.anchorMin;
                maxRect.anchorMax = plusRect.anchorMax;
                maxRect.pivot = plusRect.pivot;
                maxRect.sizeDelta = plusRect.sizeDelta;

                maxRect.anchoredPosition =
                    plusRect.anchoredPosition +
                    new Vector2(
                        plusRect.rect.width + 10f,
                        0f
                    );
            }



            // Eigenes Text-Label erzeugen.
            GameObject labelObject = new GameObject(
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

            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label =
                labelObject.GetComponent<TextMeshProUGUI>();

            label.text = "MAX";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 18f;
            label.raycastTarget = false;

            // Gelb/Orange ähnlich der Vanilla-UI
            label.color = new Color32(255, 210, 45, 255);

            // Etwas nach unten
            labelRect.anchoredPosition = new Vector2(0f, -2f);

            maxButton.onClick.AddListener(
                () => SetMaximumCraftCount(window)
            );

            return maxButton;
        }

        private static void SetMaximumCraftCount(
            UICraftSelectionWindow window)
        {
            UIBaseCraftSelectionWindowData data =
                Traverse.Create(window)
                    .Field("data")
                    .GetValue<UIBaseCraftSelectionWindowData>();

            if (data == null ||
                data.CraftItemCellsData == null ||
                data.CraftItemCellsData.Count == 0)
                return;

            int maximum = 999;

            foreach (UICraftItemCellData cell
                     in data.CraftItemCellsData)
            {
                if (cell == null ||
                    cell.currentItem == null ||
                    cell.MultiInventory == null)
                    continue;

                int required =
                    cell.currentItem.GetCount(data.WgoData);

                if (required <= 0)
                    continue;

                int available =
                    cell.MultiInventory.GetTotalCount(
                        cell.currentItem.Id
                    );

                int possible =
                    available / required;

                maximum = Math.Min(maximum, possible);
            }

            maximum = Math.Max(1, Math.Min(999, maximum));

            int delta = maximum - data.CraftsCount;

            if (delta == 0)
                return;

            // Vanilla-Methode benutzen, damit Counter,
            // Requirements und Buttons korrekt aktualisiert werden.
            Traverse.Create(window)
                .Method("ChangeCraftCount", delta)
                .GetValue();
        }

        private static void SetMaxButtonActive(
            UICraftSelectionWindow window,
            bool active)
        {
            LazyButton plusButton =
                Traverse.Create(window)
                    .Field("plusCraftButton")
                    .GetValue<LazyButton>();

            if (plusButton == null)
                return;

            Transform existing =
                plusButton.transform.parent.Find(
                    MaxButtonName
                );

            if (existing != null)
                existing.gameObject.SetActive(active);
        }

        private static string GetPath(Transform transform)
        {
            string path = transform.name;

            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
    }


}