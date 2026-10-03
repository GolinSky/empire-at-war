using System.Collections.Generic;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Audio;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.SceneService;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Audio
{
    public sealed class MusicService : Service, IMusicService, IInitializable, ITickable, ILateDisposable
    {
        private const float MUSIC_FADE_DURATION = 1f;

        private const string SOURCE_PATH = "MusicSource";

        private readonly ISceneService _scenes;
        private readonly IGameModelObserver _game;
        private readonly IAudioService _audio;

        private readonly MusicAudioData _data;
        private readonly AudioSource _source;
        private readonly System.Random _random = new System.Random();
        private List<AudioClip> _clips;

        private readonly float _volume;
        private float _fadeDuration;

        private bool _playing;
        private bool _fadingOut;

        public MusicService(ISceneService scenes, IAssetService assets, IGameModelObserver game, IAudioService audio)
        {
            _scenes = scenes;
            _game = game;
            _audio = audio;
            _data = assets.Load<MusicAudioData>(nameof(MusicAudioData));
            _source = Object.Instantiate(assets.LoadComponent<AudioSource>(SOURCE_PATH));
            _source.ignoreListenerPause = true;
            _source.priority = 16;
            _volume = _source.volume;
            Object.DontDestroyOnLoad(_source.gameObject);
        }

        public void Initialize()
        {
            OnSceneLoad(_scenes.TargetScene);
            _scenes.OnSceneActivation += OnSceneLoad;
        }

        public void LateDispose()
        {
            // The project context only disposes on quit or Play Mode exit, when Unity also destroys the
            // DontDestroyOnLoad source in no guaranteed order; touching it here can hit a destroyed object.
            _scenes.OnSceneActivation -= OnSceneLoad;
        }

        private void OnSceneLoad(SceneType scene)
        {
            if (scene == SceneType.Loading) return;
            FactionType faction = scene == SceneType.MainMenu
                ? default : MatchRules.FindHuman(_game.Players).Faction;
            _clips = _data.GetMusicList(scene, faction);
            if (_playing) _fadingOut = true;
            else PlayNext();
        }

        private void PlayNext()
        {
            _audio.Stop(_source);
            _playing = false;
            _fadingOut = false;
            if (_clips.Count == 0) return;
            AudioClip clip = _clips[_random.Next(_clips.Count)];
            _fadeDuration = Mathf.Min(MUSIC_FADE_DURATION, clip.length * 0.5f);
            _audio.Play(_source, clip, 0f, false);
            _playing = true;
        }

        public void Tick()
        {
            if (!_playing) return;
            if (!_source.isPlaying)
            {
                PlayNext();
                return;
            }
            AudioClip clip = _source.clip;
            float remaining = (clip.samples - _source.timeSamples) / (float)clip.frequency;
            if (remaining <= _fadeDuration) _fadingOut = true;
            _source.volume = Mathf.MoveTowards(_source.volume, _fadingOut ? 0f : _volume,
                _volume * Time.unscaledDeltaTime / _fadeDuration);
            if (_fadingOut && _source.volume == 0f) PlayNext();
        }
    }
}
