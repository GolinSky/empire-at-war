using EmpireAtWar.Entities.MainMenu.Settings;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class DisplayConfirmationCountdownTests
    {
        private const float START_TIME = 100f;

        [Test]
        public void Start_ShowsFullDurationWithRevertFirst()
        {
            DisplayConfirmationCountdown countdown = new DisplayConfirmationCountdown();
            countdown.Start(START_TIME);

            Assert.That(countdown.TryUpdatePrompt(START_TIME, out SettingsPrompt prompt), Is.True);
            Assert.That(prompt.Kind, Is.EqualTo(SettingsPromptKind.DisplayConfirmation));
            Assert.That(prompt.Message, Does.Contain("15 s"));
            Assert.That(prompt.Actions, Is.EqualTo(new[] { SettingsPromptAction.Revert, SettingsPromptAction.Keep }));
        }

        [Test]
        public void TryUpdatePrompt_ChangesOnlyWhenWholeSecondsChange()
        {
            DisplayConfirmationCountdown countdown = new DisplayConfirmationCountdown();
            countdown.Start(START_TIME);
            countdown.TryUpdatePrompt(START_TIME, out SettingsPrompt _);

            Assert.That(countdown.TryUpdatePrompt(START_TIME + 0.5f, out SettingsPrompt _), Is.False);
            Assert.That(countdown.TryUpdatePrompt(START_TIME + 1.5f, out SettingsPrompt prompt), Is.True);
            Assert.That(prompt.Message, Does.Contain("14 s"));
        }

        [Test]
        public void IsExpired_AfterFullDuration()
        {
            DisplayConfirmationCountdown countdown = new DisplayConfirmationCountdown();
            countdown.Start(START_TIME);

            Assert.That(countdown.IsExpired(START_TIME + DisplayConfirmationCountdown.DURATION_SECONDS - 0.1f), Is.False);
            Assert.That(countdown.IsExpired(START_TIME + DisplayConfirmationCountdown.DURATION_SECONDS), Is.True);
        }

        [Test]
        public void Start_AgainResetsTheTimer()
        {
            DisplayConfirmationCountdown countdown = new DisplayConfirmationCountdown();
            countdown.Start(START_TIME);
            countdown.Start(START_TIME + 20f);

            Assert.That(countdown.IsExpired(START_TIME + 20f), Is.False);
            Assert.That(countdown.TryUpdatePrompt(START_TIME + 20f, out SettingsPrompt prompt), Is.True);
            Assert.That(prompt.Message, Does.Contain("15 s"));
        }
    }
}
