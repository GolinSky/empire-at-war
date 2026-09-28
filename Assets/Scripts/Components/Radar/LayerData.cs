using EmpireAtWar.Mvc;
using UnityEngine;
using UnityEngine.Serialization;

namespace EmpireAtWar.Components.Radar
{
    [CreateAssetMenu(fileName = nameof(LayerData), menuName = "Data/LayerData")]
    public class LayerData:Data
    {
        // Every unit of every player shares one layer; hostility is decided in code, not by layers.
        [field: SerializeField, FormerlySerializedAs("<PlayerLayerMask>k__BackingField")]
        public LayerMask UnitLayerMask { get; private set; }
        [field: SerializeField] public LayerMask ObstacleLayerMask { get; private set; }
        [field: SerializeField] public LayerMask DeadLayerMask { get; private set; }
        
    }
}
