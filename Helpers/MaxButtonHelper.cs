using LazyBearTechnology;
using TMPro;
using UnityEngine;

namespace GK2CraftMax.Helpers
{
    internal static class MaxButtonHelper
    {
        internal static LazyButton CloneButton(LazyButton template, string objectName)
        {
            if (template == null || template.transform.parent == null)
            {
                return null;
            }

            Transform parent = template.transform.parent;

            Transform existing = parent.Find(objectName);

            if (existing != null)
            {
                return existing.GetComponent<LazyButton>();
            }

            GameObject obj = UnityEngine.Object.Instantiate(template.gameObject, parent);

            obj.name = objectName;

            LazyButton button = obj.GetComponent<LazyButton>();

            if (button == null)
            {
                UnityEngine.Object.Destroy(obj);
                return null;
            }

            button.onClick.RemoveAllListeners();

            return button;
        }

        internal static void PositionRightOf(
            LazyButton button,
            LazyButton referenceButton,
            float spacing = 10f
        )
        {
            if (button == null || referenceButton == null)
            {
                return;
            }

            RectTransform referenceRect = referenceButton.transform as RectTransform;

            RectTransform buttonRect = button.transform as RectTransform;

            if (referenceRect == null || buttonRect == null)
            {
                return;
            }

            if (!buttonRect.anchorMin.Equals(referenceRect.anchorMin))
                buttonRect.anchorMin = referenceRect.anchorMin;

            if (!buttonRect.anchorMax.Equals(referenceRect.anchorMax))
                buttonRect.anchorMax = referenceRect.anchorMax;

            if (!buttonRect.pivot.Equals(referenceRect.pivot))
                buttonRect.pivot = referenceRect.pivot;

            if (!buttonRect.sizeDelta.Equals(referenceRect.sizeDelta))
                buttonRect.sizeDelta = referenceRect.sizeDelta;

            Vector2 position =
                referenceRect.anchoredPosition
                + new Vector2(referenceRect.rect.width + spacing, 0f);
            if (!buttonRect.anchoredPosition.Equals(position))
                buttonRect.anchoredPosition = position;
        }

        internal static GamepadNavigationItem EnsureNavigationItem(LazyButton button)
        {
            if (button == null)
                return null;

            GamepadNavigationItem nav = button.GetComponent<GamepadNavigationItem>();

            if (nav == null)
            {
                nav = button.gameObject.AddComponent<GamepadNavigationItem>();
            }

            return nav;
        }

        internal static TMP_Text SetExistingLabel(LazyButton button, string text)
        {
            if (button == null)
                return null;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);

            if (label != null)
                label.text = text;
            return label;
        }

        internal static TMP_Text CreateLabel(LazyButton button, string text)
        {
            if (button == null)
                return null;

            Transform existing = button.transform.Find("GK2CraftMax_Label");

            if (existing != null)
                return existing.GetComponent<TMP_Text>();

            GameObject labelObject = new GameObject(
                "GK2CraftMax_Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );

            labelObject.transform.SetParent(button.transform, false);

            RectTransform rect = labelObject.GetComponent<RectTransform>();

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            rect.anchoredPosition = new Vector2(0f, -2f);

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();

            label.text = text;
            label.alignment = TextAlignmentOptions.Center;

            label.fontSize = 18f;
            label.raycastTarget = false;

            label.color = new Color32(255, 210, 45, 255);
            return label;
        }
    }
}
