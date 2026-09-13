using EmpireAtWar.Components.Radar;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace EmpireAtWar.Ship
{
    public interface IShipModelObserver : IUnitModelObserver
    {
        ShipType ShipType { get; }
    }

    [CreateAssetMenu(fileName = nameof(ShipComponentsData), menuName = "Data/ShipComponentsData")]
    public class ShipComponentsData : Data, IModel, IShipModelObserver
    {
        [Inject] public ShipType ShipType { get; }

        [Header("Radar Model")]
        [field: FormerlySerializedAs("radarModel")]
        [field: SerializeField] public RadarModel RadarModel { get; private set; }
    }
}
