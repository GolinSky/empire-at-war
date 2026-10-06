using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Health
{
    public interface IHardPoint
    {
        bool IsDestroyed { get; }
        int Id { get; }
        HardPointType HardPointType { get; }
        Vector3 Position { get; }
        Transform Transform { get; }

        /// <summary>The weapon mounted on this hardpoint; false for hardpoints that carry no weapon.</summary>
        bool TryGetWeaponType(out WeaponType weaponType);

        void UpdateData(float healthPercentage);
    }
}
