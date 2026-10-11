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
        // World-space game UI: selection rings, squadron icons, zone labels.
        [field: SerializeField] public LayerMask WorldUiLayerMask { get; private set; }
        [field: SerializeField] public LayerMask FogLayerMask { get; private set; }

        [field: Header("Camera Culling"), SerializeField] public LayerMask CoreGameCameraMask { get; private set; }
        // Must exclude every game UI layer (UI, WorldUi) and Fog.
        [field: SerializeField] public LayerMask CinemaCameraMask { get; private set; }
    }
}
