using System.Linq;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Components.Ship.Audio
{
    public sealed class AudioDialogShipComponent : MonoBehaviour, IAudioDialogShipComponent, IMonoComponent,
        IRespawnable
    {
        [SerializeField] private ShipAbilityId voicedAbility;
        [SerializeField] private AudioClip abilityVoice;

        private ShipAbilitySlot _voicedSlot;
        private IShipSfxService _shipSfxService;

        private ShipVoiceSet _voices;


        private bool _isSelected;

        public string Id => nameof(AudioDialogShipComponent);

        [Inject]
        private void Construct(IShipSfxService shipSfxService, IPlayerRoster playerRoster, ShipSfxData data, PlayerId owner,
            IUnitTypeFacade unitType, IShipAbilityFacade abilities)
        {
            _voices = data.GetVoiceSet(playerRoster.Get(owner).Faction, unitType.UnitTypeId);
            _shipSfxService = shipSfxService;
            if (voicedAbility != ShipAbilityId.None)
                _voicedSlot = abilities.Slots.Single(slot => slot.Id == voicedAbility);
            Respawn();
        }

        public void HandleEnemyDetected()
        {
            if (_isSelected) _shipSfxService.TryPlayAlarmVoice(_voices);
        }

        public void HandleMove(Vector3 position)
        {
            if (_isSelected) _shipSfxService.TryPlayVoice(_voices.Move);
        }

        public void HandleAttack(Vector3 target)
        {
            if (_isSelected) _shipSfxService.TryPlayVoice(_voices.Attack);
        }

        public void HandleSelection(bool isSelected)
        {
            _isSelected = isSelected;
            if (isSelected) _shipSfxService.TryPlayVoice(_voices.Selection);
        }

        private void HandleAbilityStateChanged()
        {
            if (_voicedSlot.State == ShipAbilityState.Active)
                _shipSfxService.TryPlayVoice(abilityVoice, interruptChatter: _voicedSlot.Id == ShipAbilityId.HarmonicBomb);
        }

        public void Release()
        {
            _isSelected = false;
            if (_voicedSlot != null) _voicedSlot.StateChanged -= HandleAbilityStateChanged;
        }

        public void Respawn()
        {
            if (_voicedSlot != null) _voicedSlot.StateChanged += HandleAbilityStateChanged;
        }
    }
}
