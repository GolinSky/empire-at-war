using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Components.Radar
{
    [CreateAssetMenu(fileName = "LayerModel", menuName = "Model/LayerModel")]
    public class LayerModel:Model
    {
        [field: SerializeField] public LayerMask PlayerLayerMask { get; private set; }
        [field: SerializeField] public LayerMask EnemyLayerMask { get; private set; }
        
    }
}