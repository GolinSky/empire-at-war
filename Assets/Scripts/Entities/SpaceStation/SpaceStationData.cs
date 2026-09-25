using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Entities.SpaceStation
{
    public interface ISpaceStationModelObserver:IModelObserver, IUnitModelObserver
    {
    }
    [CreateAssetMenu(fileName = nameof(SpaceStationData), menuName = "Data/SpaceStationData")]
    public class SpaceStationData:Data, IModel, ISpaceStationModelObserver, IHangarData
    {
        [field: SerializeField] public EntityComponentData ComponentData { get; private set; }
        [field:SerializeField] public RadarModel RadarModel { get; private set; }

        // Stations never launch from a reserve; their hangar only launches squadrons bought at the station.
        public IReadOnlyList<HangarBay> HangarBays => Array.Empty<HangarBay>();
        public float HangarInitialDelay => 0f;
        public float HangarLaunchInterval => 0f;
    }
}
