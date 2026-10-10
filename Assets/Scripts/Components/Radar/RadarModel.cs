using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using UnityEngine.Rendering;

namespace EmpireAtWar.Components.Radar
{
    public class RadarModel : Model, IRadarModelObserver
    {
        private readonly IRadarData _data;
        private readonly EmpireAtWar.Components.Combat.CombatModifiers _modifiers;

        public float Range => _data.Range * _modifiers.VisionMultiplier;
        public float Delay => _data.Delay;
        public PlayerId Owner { get; }
        public ObservableList<IEntity> Enemies { get; } = new ObservableList<IEntity>();

        public RadarModel(IRadarData data, PlayerId owner,
            EmpireAtWar.Components.Combat.CombatModifiers modifiers)
        {
            _data = data;
            Owner = owner;
            _modifiers = modifiers;
        }
    }
}
