using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;

namespace EmpireAtWar.Services.Audio
{
    [LifecycleOrder(Initialize = LifecycleOrders.INTERFACE_SFX_INITIALIZE)]
    public sealed class InterfaceSfxService : IInterfaceSfxService, IInitializable, ILateDisposable
    {
        private const float REJECT_COOLDOWN = 0.15f;
        private readonly InterfaceSfxData _data;
        private readonly ShipSfxData _shipData;
        private readonly ShipSfxSources _sources;
        private readonly ShipAbilityService _abilities;
        private readonly ILocalPlayer _localPlayer;
        private GameObject _root;
        private AudioSource _ui;
        private AudioSource _announcer;
        private float _rejectReadyAt;

        public bool IsAnnouncing => _announcer.isPlaying;

        public InterfaceSfxService(InterfaceSfxData data, ShipSfxData shipData, ShipSfxSources sources,
            ShipAbilityService abilities, ILocalPlayer localPlayer)
        {
            _data = data;
            _shipData = shipData;
            _sources = sources;
            _abilities = abilities;
            _localPlayer = localPlayer;
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
                source.ignoreListenerPause = true;
                source.outputAudioMixerGroup = _sources.Voice.outputAudioMixerGroup;
                source.volume = _shipData.VoiceVolume;
            }
            _abilities.TargetRejected += HandleRejected;
            _abilities.TargetingCanceled += HandleTargetingCanceled;
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
            _sources.Voice.Stop();
            _announcer.clip = clip;
            _announcer.Play();
        }

        private void HandleRejected(IReadOnlyList<IEntity> casters)
        {
            if (HasLocalCaster(casters)) PlayRejected();
        }

        private void HandleTargetingCanceled(IReadOnlyList<IEntity> casters)
        {
            if (HasLocalCaster(casters)) PlayAbilityTargetingCanceled();
        }

        private bool HasLocalCaster(IReadOnlyList<IEntity> casters)
        {
            foreach (IEntity caster in casters)
                if (caster.Owner == _localPlayer.Id) return true;
            return false;
        }

        public void LateDispose()
        {
            _abilities.TargetRejected -= HandleRejected;
            _abilities.TargetingCanceled -= HandleTargetingCanceled;
            if (Application.isPlaying) Object.Destroy(_root);
            else Object.DestroyImmediate(_root);
        }
    }
}
