using EmpireAtWar.Lifecycle;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    [LifecycleOrder(Initialize = LifecycleOrders.INTERFACE_SFX_INITIALIZE)]
    public sealed class InterfaceSfxService : IInterfaceSfxService, IInitializable, ITickable, ILateDisposable
    {
        private const float REJECT_COOLDOWN = 0.15f;
        private readonly InterfaceSfxData _data;
        private readonly ShipSfxData _shipData;
        private readonly ShipSfxSources _sources;
        private GameObject _root;
        private AudioSource _ui;
        private AudioSource _announcer;
        private float _rejectReadyAt;
        private AnnouncementPriority _playingPriority;
        private AudioClip _pendingConstruction;

        public bool IsAnnouncing => _pendingConstruction != null || IsPlayingAnnouncement;

        private bool IsPlayingAnnouncement => _announcer.isPlaying ||
            _announcer.clip != null && _announcer.clip.loadState == AudioDataLoadState.Loading;

        public InterfaceSfxService(InterfaceSfxData data, ShipSfxData shipData, ShipSfxSources sources)
        {
            _data = data;
            _shipData = shipData;
            _sources = sources;
        }

        public void Initialize()
        {
            _root = new GameObject("Interface Audio");
            _root.transform.SetParent(_sources.transform, false);
            _ui = _root.AddComponent<AudioSource>();
            _announcer = _root.AddComponent<AudioSource>();
            foreach (AudioSource source in new[] { _ui, _announcer })
            {
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.priority = 32;
                source.ignoreListenerPause = true;
                source.outputAudioMixerGroup = _sources.Voice.outputAudioMixerGroup;
                source.volume = _shipData.VoiceVolume;
            }
        }

        public void PlayRejected()
        {
            if (Time.unscaledTime < _rejectReadyAt) return;
            _rejectReadyAt = Time.unscaledTime + REJECT_COOLDOWN;
            _ui.PlayOneShot(_data.Rejected);
        }

        public void PlayAbilityTargetingCanceled() => _ui.PlayOneShot(_data.AbilityCanceled);

        public void PlayAnnouncement(AudioClip clip)
        {
            if (IsAnnouncing && _playingPriority == AnnouncementPriority.Defeat) return;
            StartAnnouncement(clip, AnnouncementPriority.Warning);
        }

        public bool TryPlayAnnouncement(AudioClip clip, AnnouncementPriority priority)
        {
            if (IsAnnouncing && (priority >= _playingPriority || priority == AnnouncementPriority.Progress))
                return false;
            StartAnnouncement(clip, priority);
            return true;
        }

        private void StartAnnouncement(AudioClip clip, AnnouncementPriority priority)
        {
            _sources.Voice.Stop();
            _pendingConstruction = null;
            _playingPriority = priority;
            _announcer.clip = clip;
            _announcer.Play();
        }

        public bool TryPlayStationUpgrade(AudioClip clip, AudioClip newConstruction)
        {
            if (!TryPlayAnnouncement(clip, AnnouncementPriority.Notification)) return false;
            _pendingConstruction = newConstruction;
            return true;
        }

        public void Tick()
        {
            if (_pendingConstruction == null || IsPlayingAnnouncement) return;
            StartAnnouncement(_pendingConstruction, AnnouncementPriority.Notification);
        }

        public void LateDispose()
        {
            _pendingConstruction = null;
            if (Application.isPlaying) UnityEngine.Object.Destroy(_root);
            else UnityEngine.Object.DestroyImmediate(_root);
        }
    }
}
