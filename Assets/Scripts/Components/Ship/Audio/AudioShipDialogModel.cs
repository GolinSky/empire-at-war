using System;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using Zenject;

namespace EmpireAtWar.Components.Ship.Audio
{
    public class AudioShipDialogModel : PureModel, IAudioShipDialogModelObserver
    {
        private readonly AudioShipDialogData _data;
        private readonly Random _dialogRandom = new Random();
        private readonly Random _attackRandom = new Random();
        private readonly Random _moveRandom = new Random();
        private readonly Random _alarmSightsRandom = new Random();
        private readonly Random _damageRandom = new Random();
        private readonly FactionType _playerFactionType;
        private readonly FactionType _enemyFactionType;

        [Inject]
        public AudioShipDialogModel(
            AudioShipDialogData data,
            [Inject(Id = PlayerType.Player)] FactionType playerFactionType,
            [Inject(Id = PlayerType.Opponent)] FactionType enemyFactionType)
        {
            _data = data;
            _playerFactionType = playerFactionType;
            _enemyFactionType = enemyFactionType;
        }

        public (FactionType FactionType, AudioShipDialogData.ClipType ClipType, int Index) GetDialogClip(PlayerType playerType)
        {
            return GetClip(playerType, AudioShipDialogData.ClipType.Dialog, _dialogRandom);
        }

        public (FactionType FactionType, AudioShipDialogData.ClipType ClipType, int Index) GetAttackClip(PlayerType playerType)
        {
            return GetClip(playerType, AudioShipDialogData.ClipType.Attack, _attackRandom);
        }

        public (FactionType FactionType, AudioShipDialogData.ClipType ClipType, int Index) GetMoveClip(PlayerType playerType)
        {
            return GetClip(playerType, AudioShipDialogData.ClipType.Move, _moveRandom);
        }

        public (FactionType FactionType, AudioShipDialogData.ClipType ClipType, int Index) GetAlarmSightsClip(PlayerType playerType)
        {
            return GetClip(playerType, AudioShipDialogData.ClipType.AlarmSights, _alarmSightsRandom);
        }

        public (FactionType FactionType, AudioShipDialogData.ClipType ClipType, int Index) GetDamageClip(PlayerType playerType)
        {
            return GetClip(playerType, AudioShipDialogData.ClipType.Damage, _damageRandom);
        }

        private (FactionType FactionType, AudioShipDialogData.ClipType ClipType, int Index) GetClip(
            PlayerType playerType,
            AudioShipDialogData.ClipType clipType,
            Random random)
        {
            FactionType factionType = playerType == PlayerType.Player ? _playerFactionType : _enemyFactionType;
            return (factionType, clipType, random.Next(_data.GetClipCount(factionType, clipType)));
        }
    }
}
