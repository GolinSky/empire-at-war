using EmpireAtWar.Components.AttackComponent;

namespace EmpireAtWar.Models.Health
{
    public interface IHardPointStatus : IHardPointModel
    {
        float Health { get; }
        float MaxHealth { get; }
        /// <summary>False while the hardpoint waits for its upgrade level; it is then absent from the unit.</summary>
        bool IsInstalled { get; }

        /// <summary>The weapon mounted on this hardpoint; false for hardpoints that carry no weapon.</summary>
        bool TryGetWeaponType(out WeaponType weaponType);
    }
}
