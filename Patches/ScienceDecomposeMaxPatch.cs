using System.Reflection;
using GK2CraftMax.Helpers;
using HarmonyLib;
using LazyBearTechnology;
using UnityEngine;

namespace GK2CraftMax.Patches
{
    [HarmonyPatch(typeof(UIResourceBasedCraftWindow), "Redraw")]
    internal static class ScienceDecomposeMaxPatch
    {
        private const string MaxButtonName = "GK2CraftMax_ScienceMaxButton";
        private static readonly AccessTools.FieldRef<
            UIResourceBasedCraftWindow,
            UIResourceBasedCraftWindowData
        > ReadData = AccessTools.FieldRefAccess<
            UIResourceBasedCraftWindow,
            UIResourceBasedCraftWindowData
        >("data");
        private static readonly AccessTools.FieldRef<
            UIResourceBasedCraftWindow,
            LazyButton
        > ReadCraftButton = AccessTools.FieldRefAccess<UIResourceBasedCraftWindow, LazyButton>(
            "craftBtn"
        );
        private static readonly AccessTools.FieldRef<
            UIResourceBasedCraftWindowData,
            SurveyDef
        > ReadSurvey = AccessTools.FieldRefAccess<UIResourceBasedCraftWindowData, SurveyDef>(
            "currentSurveyDef"
        );
        private static readonly AccessTools.FieldRef<
            UIResourceBasedCraftWindow,
            UIItemCell
        > ReadIngredient = AccessTools.FieldRefAccess<UIResourceBasedCraftWindow, UIItemCell>(
            "mainIngredient"
        );

        private static readonly MethodInfo OnStartSurveyMethod = AccessTools.Method(
            typeof(UIResourceBasedCraftWindowData),
            "OnStartSurvey"
        );

        [HarmonyPostfix]
        private static void Postfix(UIResourceBasedCraftWindow __instance)
        {
            if (__instance == null)
                return;

            UIResourceBasedCraftWindowData data = ReadData(__instance);

            LazyButton craftButton = ReadCraftButton(__instance);

            if (data == null || craftButton == null)
                return;

            SurveyDef surveyDef = ReadSurvey(data);

            bool showMax =
                data.SelectedItem != null
                && !data.SelectedItem.IsEmpty
                && surveyDef != null
                && surveyDef.isScienceFuelCraft;

            if (!showMax)
            {
                LazyButton existing = MaxButtonState
                    .For(__instance)
                    .Resolve(craftButton, MaxButtonName);
                if (existing != null && existing.gameObject.activeSelf)
                    existing.gameObject.SetActive(false);
                return;
            }

            LazyButton maxButton = GetOrCreateMaxButton(__instance, craftButton);

            if (maxButton == null)
                return;

            if (!maxButton.gameObject.activeSelf)
                maxButton.gameObject.SetActive(true);

            maxButton.interactable = data.CanStartCraft();

            if (LazyInput.IsGamepadActive)
            {
                SetupGamepadNavigation(__instance, maxButton);
            }
        }

        private static LazyButton GetOrCreateMaxButton(
            UIResourceBasedCraftWindow window,
            LazyButton craftButton
        )
        {
            MaxButtonState state = MaxButtonState.For(window);
            LazyButton maxButton = state.Resolve(craftButton, MaxButtonName);
            if (maxButton == null)
            {
                maxButton = MaxButtonHelper.CloneButton(craftButton, MaxButtonName);
                if (maxButton != null)
                    BindMaximumClick(window, maxButton);
                state.Button = maxButton;
            }

            if (maxButton == null)
                return null;

            MaxButtonHelper.PositionRightOf(maxButton, craftButton);

            if (!state.Initialized || state.Label == null)
                state.Label = MaxButtonHelper.SetExistingLabel(maxButton, "MAX");
            if (state.Navigation == null)
                state.Navigation = MaxButtonHelper.EnsureNavigationItem(maxButton);
            state.Initialized = true;

            return maxButton;
        }

        private static void BindMaximumClick(UIResourceBasedCraftWindow window, LazyButton button)
        {
            button.onClick.AddListener(() => DecomposeMaximum(window));
        }

        private static void DecomposeMaximum(UIResourceBasedCraftWindow window)
        {
            /*
             * MAX ist ausschließlich eine Aktion des
             * vom Spieler geöffneten Survey-Fensters.
             */
            if (window == null || MainGame.PlayerController == null || MainGame.PlayerData == null)
            {
                return;
            }

            UIResourceBasedCraftWindowData data = ReadData(window);

            if (data == null || data.SelectedItem == null || data.SelectedItem.IsEmpty)
            {
                return;
            }

            SurveyDef surveyDef = ReadSurvey(data);

            /*
             * Normale Surveys niemals über MAX
             * verarbeiten.
             */
            if (surveyDef == null || !surveyDef.isScienceFuelCraft)
            {
                return;
            }

            if (OnStartSurveyMethod == null)
            {
                return;
            }

            int safety = 999;

            while (data.CanStartCraft() && safety-- > 0)
            {
                int before = data.MainIngredientCount;

                OnStartSurveyMethod.Invoke(data, null);

                int after = data.MainIngredientCount;

                /*
                 * Vanilla muss mindestens ein Item
                 * verbraucht haben.
                 *
                 * Falls nicht, brechen wir ab, damit
                 * keine Endlosschleife entstehen kann.
                 */
                if (after >= before)
                {
                    Debug.LogWarning(
                        "[CraftMax] Science MAX stopped: "
                            + $"item count did not decrease "
                            + $"({before} -> {after})."
                    );

                    break;
                }

                /*
                 * OnUpdateData() entfernt SelectedItem,
                 * sobald davon nichts mehr vorhanden ist.
                 */
                if (data.SelectedItem == null || data.SelectedItem.IsEmpty)
                {
                    break;
                }
            }
        }

        private static void SetupGamepadNavigation(
            UIResourceBasedCraftWindow window,
            LazyButton maxButton
        )
        {
            if (window == null || maxButton == null)
                return;

            UIItemCell mainIngredient = ReadIngredient(window);

            if (mainIngredient == null)
                return;

            GamepadNavigationController controller = GamepadNavigationHelper.GetController(window);

            if (controller == null)
                return;

            GamepadNavigationItem ingredientNav = mainIngredient.GamepadNavigationItem;

            MaxButtonState state = MaxButtonState.For(window);
            if (state.Navigation == null)
                state.Navigation = MaxButtonHelper.EnsureNavigationItem(maxButton);
            GamepadNavigationItem maxNav = state.Navigation;

            if (ingredientNav == null || maxNav == null)
            {
                return;
            }

            maxNav.Active = true;
            maxNav.enabled = true;
            maxNav.group = ingredientNav.group;

            GamepadNavigationHelper.RegisterAndReinit(
                controller,
                window.transform.lossyScale.x,
                ingredientNav,
                maxNav
            );

            GamepadNavigationHelper.Link(ingredientNav, GUIDirection.Down, maxNav);

            GamepadNavigationHelper.Link(maxNav, GUIDirection.Up, ingredientNav);

            GamepadNavigationHelper.BindButtonPress(maxNav, maxButton);

            controller.SetFocusedItem(ingredientNav);
        }
    }
}
