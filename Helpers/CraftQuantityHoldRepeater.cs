using LazyBearTechnology;
using UnityEngine;

namespace GK2CraftMax.Helpers
{
    internal sealed class CraftQuantityHoldRepeater : MonoBehaviour
    {
        private const float HoldDelay = 0.5f;
        private const float RepeatInterval = 0.2f;

        private UIBaseCraftSelectionWindow window;

        private int direction;
        private float holdStartedAt;
        private float nextRepeatAt;

        internal void Initialize(UIBaseCraftSelectionWindow craftWindow)
        {
            window = craftWindow;
            ResetHold();
        }

        private void Update()
        {
            if (
                window == null
                || !window.IsShownAndTop
                || !LazyInput.IsGamepadActive
                || !LazyInput.IsInputActive()
            )
            {
                ResetHold();
                return;
            }

            bool decrease = LazyInput.GetKey(GameKey.PrevTab);
            bool increase = LazyInput.GetKey(GameKey.NextTab);

            /*
             * Beide gleichzeitig = neutral.
             */
            if (decrease == increase)
            {
                ResetHold();
                return;
            }

            int newDirection = increase ? 1 : -1;

            /*
             * Neuer Tastendruck:
             *
             * Der normale GetGameKeyDelegates-Callback übernimmt
             * bereits den ersten ±10-Schritt.
             *
             * Hier starten wir deshalb nur den Hold-Timer.
             */
            if (direction != newDirection)
            {
                direction = newDirection;
                holdStartedAt = Time.unscaledTime;
                nextRepeatAt = holdStartedAt + HoldDelay;
                return;
            }

            if (Time.unscaledTime < nextRepeatAt)
                return;

            CraftQuantityShortcutHelper.TryChange(window, increase: direction > 0);

            nextRepeatAt = Time.unscaledTime + RepeatInterval;
        }

        private void ResetHold()
        {
            direction = 0;
            holdStartedAt = 0f;
            nextRepeatAt = 0f;
        }

        private void OnDisable()
        {
            ResetHold();
        }
    }
}
