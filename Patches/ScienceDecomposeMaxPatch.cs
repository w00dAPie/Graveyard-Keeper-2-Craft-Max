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
        private const string MaxButtonName =
            "GK2CraftMax_ScienceMaxButton";

        private static readonly MethodInfo OnStartSurveyMethod =
            AccessTools.Method(
                typeof(UIResourceBasedCraftWindowData),
                "OnStartSurvey"
            );

        [HarmonyPostfix]
        private static void Postfix(
            UIResourceBasedCraftWindow __instance)
        {
            if (__instance == null)
                return;

            UIResourceBasedCraftWindowData data =
                Traverse.Create(__instance)
                    .Field("data")
                    .GetValue<UIResourceBasedCraftWindowData>();

            LazyButton craftButton =
                Traverse.Create(__instance)
                    .Field("craftBtn")
                    .GetValue<LazyButton>();

            if (data == null || craftButton == null)
                return;

            SurveyDef surveyDef =
                Traverse.Create(data)
                    .Field("currentSurveyDef")
                    .GetValue<SurveyDef>();

            bool showMax =
                data.SelectedItem != null &&
                !data.SelectedItem.IsEmpty &&
                surveyDef != null &&
                surveyDef.isScienceFuelCraft;

            LazyButton maxButton =
                GetOrCreateMaxButton(
                    __instance,
                    craftButton
                );

            if (maxButton == null)
                return;

            maxButton.gameObject.SetActive(showMax);

            if (!showMax)
                return;

            maxButton.interactable =
                data.CanStartCraft();

            if (LazyInput.IsGamepadActive)
            {
                SetupGamepadNavigation(
                    __instance,
                    maxButton
                );
            }

        }

        private static LazyButton GetOrCreateMaxButton(
            UIResourceBasedCraftWindow window,
            LazyButton craftButton)
        {
            LazyButton maxButton =
                MaxButtonHelper.CloneButton(
                    craftButton,
                    MaxButtonName,
                    () => DecomposeMaximum(window)
                );

            if (maxButton == null)
                return null;

            MaxButtonHelper.PositionRightOf(
                maxButton,
                craftButton
            );

            MaxButtonHelper.SetExistingLabel(
                maxButton,
                "MAX"
            );

            MaxButtonHelper.EnsureNavigationItem(
                maxButton
            );

            return maxButton;
        }

        private static void DecomposeMaximum(
            UIResourceBasedCraftWindow window)
        {
            /*
             * MAX ist ausschließlich eine Aktion des
             * vom Spieler geöffneten Survey-Fensters.
             */
            if (window == null ||
                MainGame.PlayerController == null ||
                MainGame.PlayerData == null)
            {
                return;
            }

            UIResourceBasedCraftWindowData data =
                Traverse.Create(window)
                    .Field("data")
                    .GetValue<UIResourceBasedCraftWindowData>();

            if (data == null ||
                data.SelectedItem == null ||
                data.SelectedItem.IsEmpty)
            {
                return;
            }

            SurveyDef surveyDef =
                Traverse.Create(data)
                    .Field("currentSurveyDef")
                    .GetValue<SurveyDef>();

            /*
             * Normale Surveys niemals über MAX
             * verarbeiten.
             */
            if (surveyDef == null ||
                !surveyDef.isScienceFuelCraft)
            {
                return;
            }

            if (OnStartSurveyMethod == null)
            {
                return;
            }

            int safety = 999;

            while (data.CanStartCraft() &&
                   safety-- > 0)
            {
                int before =
                    data.MainIngredientCount;

                OnStartSurveyMethod.Invoke(
                    data,
                    null
                );

                int after =
                    data.MainIngredientCount;

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
                        "[CraftMax] Science MAX stopped: " +
                        $"item count did not decrease " +
                        $"({before} -> {after})."
                    );

                    break;
                }

                /*
                 * OnUpdateData() entfernt SelectedItem,
                 * sobald davon nichts mehr vorhanden ist.
                 */
                if (data.SelectedItem == null ||
                    data.SelectedItem.IsEmpty)
                {
                    break;
                }
            }
        }

        private static void SetupGamepadNavigation(
            UIResourceBasedCraftWindow window,
            LazyButton maxButton)
        {
            if (window == null || maxButton == null)
                return;

            UIItemCell mainIngredient =
                Traverse.Create(window)
                    .Field("mainIngredient")
                    .GetValue<UIItemCell>();

            if (mainIngredient == null)
                return;

            GamepadNavigationController controller =
                GamepadNavigationHelper
                    .GetController(window);

            if (controller == null)
                return;

            GamepadNavigationItem ingredientNav =
                mainIngredient.GamepadNavigationItem;

            GamepadNavigationItem maxNav =
                MaxButtonHelper
                    .EnsureNavigationItem(maxButton);

            if (ingredientNav == null ||
                maxNav == null)
            {
                return;
            }

            maxNav.Active = true;
            maxNav.enabled = true;
            maxNav.group = ingredientNav.group;

            GamepadNavigationHelper
                .RegisterAndReinit(
                    controller,
                    window.transform.lossyScale.x,
                    ingredientNav,
                    maxNav
                );

            GamepadNavigationHelper.Link(
                ingredientNav,
                GUIDirection.Down,
                maxNav
            );

            GamepadNavigationHelper.Link(
                maxNav,
                GUIDirection.Up,
                ingredientNav
            );

            GamepadNavigationHelper.BindButtonPress(
                maxNav,
                maxButton
            );

            controller.SetFocusedItem(
                ingredientNav
            );
        }
    }
}