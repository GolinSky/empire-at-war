using System.IO;
using System.Linq;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.MainMenu.Survival;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class MenuFactionAudioTests
    {
        private const string AUDIO_DATA_PATH = "Assets/Settings/Data/Models/Audio/InterfaceSfxData.asset";

        [Test]
        public void InitializeSelectsSfxGroupWhenMixerAlsoReturnsItsChildren()
        {
            var data = AssetDatabase.LoadAssetAtPath<InterfaceSfxData>(AUDIO_DATA_PATH);
            var mixer = AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>("Assets/Audio/AudioMixer.mixer");
            Assert.That(mixer.FindMatchingGroups("Sfx").Length, Is.GreaterThan(1));
            var playback = new RecordingAudio();
            var menu = new MenuFactionSfxService(data, playback, mixer);
            try
            {
                menu.Initialize();
                menu.PlaySelection(FactionType.Empire);
                Assert.That(playback.Source.outputAudioMixerGroup.name, Is.EqualTo("Sfx"));
                Assert.That(playback.Source.outputAudioMixerGroup.audioMixer, Is.SameAs(mixer));
                Assert.That(playback.Source.spatialBlend, Is.Zero);
            }
            finally { menu.LateDispose(); }
            Assert.That(playback.Source == null, Is.True, "Disposal destroys the initialized audio source.");
        }

        [Test]
        public void MainMenuScopeHasItsSerializedAudioDataReference()
        {
            string scopeGuid = AssetDatabase.AssetPathToGUID("Assets/Scripts/Services/VContainer/Scopes/MainMenuScope.cs");
            string dataGuid = AssetDatabase.AssetPathToGUID(AUDIO_DATA_PATH);
            Assert.That(scopeGuid, Is.Not.Empty);
            Assert.That(dataGuid, Is.Not.Empty);
            string scene = File.ReadAllText("Assets/Scenes/MainMenuScene.unity");
            string[] scopes = scene.Split(new[] { "--- !u!" }, System.StringSplitOptions.None)
                .Where(block => block.Contains("guid: " + scopeGuid)).ToArray();
            Assert.That(scopes, Has.Length.EqualTo(1));
            Assert.That(scopes[0], Does.Contain("interfaceSfxData: {fileID: 11400000, guid: " + dataGuid + ", type: 2}"));
        }

        [TestCase(FactionType.Empire, "GUI_Select_Empire")]
        [TestCase(FactionType.Rebellion, "GUI_Select_Rebel")]
        [TestCase(FactionType.Republic, "GUI_Select_Rebel")]
        [TestCase(FactionType.Separatist, "GUI_Select_Rebel")]
        public void MenuRoutesAllFactionsToTheirChosenClip(FactionType faction, string clipName)
        {
            var data = AssetDatabase.LoadAssetAtPath<InterfaceSfxData>(AUDIO_DATA_PATH);
            var playback = new RecordingAudio();
            var menu = new MenuFactionSfxService(data, playback, null);
            menu.PlaySelection(faction);
            Assert.That(playback.Clip, Is.Not.Null);
            Assert.That(playback.Clip.name, Is.EqualTo(clipName));
        }

        [Test]
        public void SkirmishOnlyAnnouncesChangedHumanFaction()
        {
            var model = new SkirmishModel();
            var audio = new SelectionAudio();
            var controller = new SkirmishRouteUiController(null, null, null, null, model, null, audio);
            controller.SelectSlotFaction(0, (int)FactionType.Republic);
            controller.SelectSlotFaction(1, (int)FactionType.Empire);
            Assert.That(audio.Count, Is.Zero);
            controller.SelectSlotFaction(0, (int)FactionType.Empire);
            Assert.That(audio.Count, Is.EqualTo(1));
            Assert.That(audio.Faction, Is.EqualTo(FactionType.Empire));
            controller.SelectSlotFaction(0, (int)FactionType.Empire);
            Assert.That(audio.Count, Is.EqualTo(1));
        }

        [Test]
        public void SurvivalOnlyAnnouncesChangedPlayerFaction()
        {
            var model = new SurvivalModel();
            var audio = new SelectionAudio();
            var controller = new SurvivalRouteUiController(null, null, model, null, audio);
            controller.SelectPlayer((int)FactionType.Republic);
            controller.SelectEnemy((int)FactionType.Empire);
            controller.SelectAlly((int)FactionType.Empire + 1);
            Assert.That(audio.Count, Is.Zero);
            controller.SelectPlayer((int)FactionType.Rebellion);
            Assert.That(audio.Count, Is.EqualTo(1));
            Assert.That(audio.Faction, Is.EqualTo(FactionType.Rebellion));
        }

        [Test]
        public void DirectSetupAndRestorationRemainSilent()
        {
            var model = new SkirmishModel();
            var audio = new SelectionAudio();
            _ = new SkirmishRouteUiController(null, null, null, null, model, null, audio);
            model.SelectSlotFaction(0, FactionType.Empire);
            Assert.That(audio.Count, Is.Zero);
        }

        private sealed class SelectionAudio : IMenuFactionSfxService
        {
            public int Count { get; private set; }
            public FactionType Faction { get; private set; }
            public void PlaySelection(FactionType faction) { Count++; Faction = faction; }
        }

        private sealed class RecordingAudio : Service, IAudioService
        {
            public AudioClip Clip { get; private set; }
            public AudioSource Source { get; private set; }
            public void Play(AudioSource source, AudioClip clip, float volume, bool loop)
            {
                Source = source;
                Clip = clip;
            }
            public void PlayOneShot(AudioSource source, AudioClip clip, float volume) => Clip = clip;
            public void Stop(AudioSource source) { }
            public void Pause(AudioSource source) { }
            public void UnPause(AudioSource source) { }
            public void SetGamePaused(bool paused) { }
        }
    }
}
