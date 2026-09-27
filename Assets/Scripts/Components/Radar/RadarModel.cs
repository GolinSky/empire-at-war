using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using UnityEngine.Rendering;

namespace EmpireAtWar.Components.Radar
{
    public class RadarModel : PureModel, IRadarModelObserver
    {
        private readonly IRadarData _data;

        public RadarModel(IRadarData data, PlayerType playerType)
        {
            _data = data;
            PlayerType = playerType;
        }

        public float Range => _data.Range;
        public float Delay => _data.Delay;
        public float Distance => _data.Distance;
        public PlayerType PlayerType { get; }
        public ObservableList<IEntity> Enemies { get; } = new ObservableList<IEntity>();
    }
}
