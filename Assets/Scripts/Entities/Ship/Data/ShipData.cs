using EmpireAtWar.Components.Weapon;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.Data
{
    [CreateAssetMenu(fileName = "ShipData", menuName = "Data/ShipData")]
    public class ShipData: Mvc.Data, IWeaponContext
    {
        [Header("Weapon Settings")]
        [field: SerializeField] public float DelayBetweenAttack { get; set; }
        
    }
}