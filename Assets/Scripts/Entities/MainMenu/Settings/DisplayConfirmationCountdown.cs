using System;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    /// <summary>
    /// Timer behind the display Keep/Revert prompt. Callers pass unscaled time, so it runs while the game is paused.
    /// </summary>
    public sealed class DisplayConfirmationCountdown
    {
        public const float DURATION_SECONDS = 15f;

        private static readonly SettingsPromptAction[] ACTIONS =
            { SettingsPromptAction.Revert, SettingsPromptAction.Keep };

        private float _revertTime;
        private int _shownSeconds;

        public void Start(float now)
        {
            _revertTime = now + DURATION_SECONDS;
            _shownSeconds = 0;
        }

        public bool IsExpired(float now)
        {
            return now >= _revertTime;
        }

        /// <summary>Returns a new prompt only when the displayed whole seconds change.</summary>
        public bool TryUpdatePrompt(float now, out SettingsPrompt prompt)
        {
            int secondsLeft = (int)Math.Ceiling(_revertTime - now);
            if (secondsLeft == _shownSeconds)
            {
                prompt = SettingsPrompt.None;
                return false;
            }

            _shownSeconds = secondsLeft;
            prompt = new SettingsPrompt(
                SettingsPromptKind.DisplayConfirmation,
                $"Keep these display settings?\nReverting in {secondsLeft} s.",
                ACTIONS);
            return true;
        }
    }
}
