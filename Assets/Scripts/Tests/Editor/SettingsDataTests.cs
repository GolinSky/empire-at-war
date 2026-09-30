using EmpireAtWar.Services.Settings;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class SettingsDataTests
    {
        [Test]
        public void FromJson_MissingSectionsKeepDefaults()
        {
            SettingsData data = SettingsData.FromJson("{}");

            Assert.That(data.SchemaVersion, Is.EqualTo(SettingsData.CURRENT_SCHEMA_VERSION));
            Assert.That(data.Display.WindowMode, Is.EqualTo(DisplayWindowMode.Borderless));
            Assert.That(data.Display.UsesDesktopResolution, Is.True);
            Assert.That(data.Audio.MasterVolume, Is.EqualTo(1f));
            Assert.That(data.Audio.MuteWhenUnfocused, Is.True);
            Assert.That(data.Camera.EdgeScrolling, Is.True);
            Assert.That(data.Input.BindingOverridesJson, Is.Empty);
        }

        [Test]
        public void FromJson_IgnoresUnknownFields()
        {
            SettingsData data = SettingsData.FromJson(
                "{\"removedSection\":{\"value\":3},\"camera\":{\"panSpeedMultiplier\":2.0,\"oldField\":true}}");

            Assert.That(data.Camera.PanSpeedMultiplier, Is.EqualTo(2f));
        }

        [Test]
        public void FromJson_SanitizesOutOfRangeValues()
        {
            SettingsData data = SettingsData.FromJson(
                "{\"display\":{\"windowMode\":\"Bogus\",\"width\":-5,\"height\":900}," +
                "\"graphics\":{\"frameRateLimit\":-30}," +
                "\"audio\":{\"masterVolume\":2.5,\"musicVolume\":-1.0}," +
                "\"camera\":{\"panSpeedMultiplier\":10.0,\"zoomSpeedMultiplier\":0.01}}");

            Assert.That(data.Display.WindowMode, Is.EqualTo(DisplayWindowMode.Borderless));
            Assert.That(data.Display.UsesDesktopResolution, Is.True);
            Assert.That(data.Graphics.FrameRateLimit, Is.EqualTo(GraphicsSettingsData.UNLIMITED_FRAME_RATE));
            Assert.That(data.Audio.MasterVolume, Is.EqualTo(1f));
            Assert.That(data.Audio.MusicVolume, Is.EqualTo(0f));
            Assert.That(data.Camera.PanSpeedMultiplier, Is.EqualTo(CameraSettingsData.MAX_SPEED_MULTIPLIER));
            Assert.That(data.Camera.ZoomSpeedMultiplier, Is.EqualTo(CameraSettingsData.MIN_SPEED_MULTIPLIER));
        }

        [Test]
        public void Clone_IsIndependentAndMatchesUntilEdited()
        {
            SettingsData original = new SettingsData();
            SettingsData clone = original.Clone();

            Assert.That(clone.Matches(original), Is.True);

            clone.Audio.SfxVolume = 0.5f;

            Assert.That(clone.Matches(original), Is.False);
            Assert.That(original.Audio.SfxVolume, Is.EqualTo(1f));
        }

        [Test]
        public void Matches_DetectsEditsInEverySection()
        {
            SettingsData saved = new SettingsData();

            SettingsData display = saved.Clone();
            display.Display.WindowMode = DisplayWindowMode.Windowed;
            SettingsData graphics = saved.Clone();
            graphics.Graphics.VSync = true;
            SettingsData audio = saved.Clone();
            audio.Audio.MuteWhenUnfocused = false;
            SettingsData camera = saved.Clone();
            camera.Camera.InvertZoom = true;
            SettingsData input = saved.Clone();
            input.Input.BindingOverridesJson = "{}";

            foreach (SettingsData edited in new[] { display, graphics, audio, camera, input })
            {
                Assert.That(edited.Matches(saved), Is.False);
            }
        }

        [Test]
        public void ToJson_RoundTripsValues()
        {
            SettingsData data = new SettingsData();
            data.Display.WindowMode = DisplayWindowMode.ExclusiveFullscreen;
            data.Display.SetResolution(1920, 1080);
            data.Graphics.QualityPreset = "Balanced";
            data.Audio.VoiceVolume = 0.25f;
            data.Camera.ZoomSpeedMultiplier = 1.5f;

            Assert.That(SettingsData.FromJson(data.ToJson()).Matches(data), Is.True);
        }
    }
}
