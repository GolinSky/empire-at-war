using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Components.Radar
{
    [CreateAssetMenu(fileName = nameof(LayerData), menuName = "Data/LayerData")]
    public class LayerData:Data
    {
        [field: SerializeField] public LayerMask PlayerLayerMask { get; private set; }
        [field: SerializeField] public LayerMask EnemyLayerMask { get; private set; }
        [field: SerializeField] public LayerMask ObstacleLayerMask { get; private set; }
        [field: SerializeField] public LayerMask DeadLayerMask { get; private set; }
        
    }
}
