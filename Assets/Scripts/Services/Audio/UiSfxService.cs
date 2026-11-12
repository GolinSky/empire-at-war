using System.Linq;
using EmpireAtWar.Lifecycle;
using UnityEngine;
using UnityEngine.Audio;

namespace EmpireAtWar.Services.Audio
{
    public sealed class UiSfxService : IUiSfxService, IInitializable, ILateTickable, ILateDisposable
    {
        private readonly UiSfxData _data;
        private readonly IAudioService _audio;
        private readonly AudioMixer _mixer;
        private AudioSource _source;
        private bool _clickPending;
        private UiSelectSfx? _selectionPending;

        public UiSfxService(UiSfxData data, IAudioService audio, AudioMixer mixer)
        {
            _data = data;
            _audio = audio;
            _mixer = mixer;
        }

        public void Initialize()
        {
            AudioMixerGroup group = _mixer.FindMatchingGroups("Sfx").Single(group => group.name == "Sfx");
            var root = new GameObject("UI SFX");
            if (Application.isPlaying) Object.DontDestroyOnLoad(root);
            _source = root.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.ignoreListenerPause = true;
            _source.outputAudioMixerGroup = group;
        }

        public void RequestClick() => _clickPending = true;

        public void RequestSelect(UiSelectSfx kind) => _selectionPending = kind;

        public void LateTick()
        {
            UiSelectSfx? selection = _selectionPending;
            if (!_clickPending && !selection.HasValue) return;

            _clickPending = false;
            _selectionPending = null;
            var (clip, volume) = _data.GetCue(selection);
            _audio.PlayOneShot(_source, clip, volume);
        }

        public void LateDispose()
        {
            if (_source == null) return;
            if (Application.isPlaying) Object.Destroy(_source.gameObject);
            else Object.DestroyImmediate(_source.gameObject);
        }
    }
}
