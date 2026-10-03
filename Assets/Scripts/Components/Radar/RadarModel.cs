using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using UnityEngine.Rendering;

namespace EmpireAtWar.Components.Radar
{
    public class RadarModel : PureModel, IRadarModelObserver
    {
        private readonly IRadarData _data;

        public float Range => _data.Range;
        public float Delay => _data.Delay;
        public PlayerId Owner { get; }
        public ObservableList<IEntity> Enemies { get; } = new ObservableList<IEntity>();

        public RadarModel(IRadarData data, PlayerId owner)
        {
            _data = data;
            Owner = owner;
        }
    }
}
