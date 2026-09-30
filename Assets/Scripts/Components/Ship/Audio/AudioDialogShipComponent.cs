using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Audio;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Components.Ship.Audio
{
    public sealed class AudioDialogShipComponent : MonoBehaviour, IAudioDialogShipComponent, IMonoComponent
    {
        private const float MIN_ALARM_DELAY = 30f;
        private const float MAX_ALARM_DELAY = 60f;
        private IShipSfxService _audio;
        private ShipVoiceSet _voices;
        private float _alarmReadyAt;
        private bool _isSelected;

        public string Id => nameof(AudioDialogShipComponent);

        [Inject]
        private void Construct(ShipSfxData data, IShipSfxService audio, IPlayerRoster roster, PlayerId owner)
        {
            _voices = data.GetVoiceSet(roster.Get(owner).Faction);
            _audio = audio;
            _alarmReadyAt = Time.time + Random.Range(MIN_ALARM_DELAY, MAX_ALARM_DELAY);
        }

        public void HandleEnemyDetected()
        {
            if (!_isSelected || Time.time < _alarmReadyAt) return;
            if (_audio.TryPlayVoice(_voices.Alarm))
                _alarmReadyAt = Time.time + Random.Range(MIN_ALARM_DELAY, MAX_ALARM_DELAY);
        }

        public void HandleStopped()
        {
            if (_isSelected) _audio.TryPlayVoice(_voices.Damage);
        }

        public void HandleMove(Vector3 position)
        {
            if (_isSelected) _audio.TryPlayVoice(_voices.Move);
        }

        public void HandleAttack(Vector3 target)
        {
            if (_isSelected) _audio.TryPlayVoice(_voices.Attack);
        }

        public void HandleSelection(bool isSelected)
        {
            _isSelected = isSelected;
            if (isSelected) _audio.TryPlayVoice(_voices.Selection);
        }

        public void Release() => _isSelected = false;
    }
}
