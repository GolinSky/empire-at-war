using System;
using EmpireAtWar.Models.Players;
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
        private readonly IPlayerRoster _roster;

        [Inject]
        public AudioShipDialogModel(
            AudioShipDialogData data,
            IPlayerRoster roster)
        {
            _data = data;
            _roster = roster;
        }

        public (FactionType FactionType, AudioShipDialogData.ClipType ClipType, int Index) GetDialogClip(PlayerId owner)
        {
            return GetClip(owner, AudioShipDialogData.ClipType.Dialog, _dialogRandom);
        }

        public (FactionType FactionType, AudioShipDialogData.ClipType ClipType, int Index) GetAttackClip(PlayerId owner)
        {
            return GetClip(owner, AudioShipDialogData.ClipType.Attack, _attackRandom);
        }

        public (FactionType FactionType, AudioShipDialogData.ClipType ClipType, int Index) GetMoveClip(PlayerId owner)
        {
            return GetClip(owner, AudioShipDialogData.ClipType.Move, _moveRandom);
        }

        public (FactionType FactionType, AudioShipDialogData.ClipType ClipType, int Index) GetAlarmSightsClip(PlayerId owner)
        {
            return GetClip(owner, AudioShipDialogData.ClipType.AlarmSights, _alarmSightsRandom);
        }

        public (FactionType FactionType, AudioShipDialogData.ClipType ClipType, int Index) GetDamageClip(PlayerId owner)
        {
            return GetClip(owner, AudioShipDialogData.ClipType.Damage, _damageRandom);
        }

        private (FactionType FactionType, AudioShipDialogData.ClipType ClipType, int Index) GetClip(
            PlayerId owner,
            AudioShipDialogData.ClipType clipType,
            Random random)
        {
            FactionType factionType = _roster.Get(owner).Faction;
            return (factionType, clipType, random.Next(_data.GetClipCount(factionType, clipType)));
        }
    }
}
