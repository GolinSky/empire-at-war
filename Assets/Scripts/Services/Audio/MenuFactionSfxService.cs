using System.Linq;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using UnityEngine;
using UnityEngine.Audio;

namespace EmpireAtWar.Services.Audio
{
    public sealed class MenuFactionSfxService : IMenuFactionSfxService, IInitializable, ILateDisposable
    {
        private readonly InterfaceSfxData _data;
        private readonly IAudioService _audio;
        private readonly AudioMixer _mixer;
        private AudioSource _source;

        public MenuFactionSfxService(InterfaceSfxData data, IAudioService audio, AudioMixer mixer)
        {
            _data = data;
            _audio = audio;
            _mixer = mixer;
        }

        public void Initialize()
        {
            _source = new GameObject("Menu Faction Audio").AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.ignoreListenerPause = true;
            _source.outputAudioMixerGroup = _mixer.FindMatchingGroups("Sfx").Single(group => group.name == "Sfx");
        }

        public void PlaySelection(FactionType faction)
        {
            AudioClip clip = faction == FactionType.Empire ? _data.EmpireSelected : _data.RebellionSelected;
            _audio.Play(_source, clip, 1f, false);
        }

        public void LateDispose()
        {
            if (_source == null) return;
            if (Application.isPlaying) Object.Destroy(_source.gameObject);
            else Object.DestroyImmediate(_source.gameObject);
        }
    }
}
