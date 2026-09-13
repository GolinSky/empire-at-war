using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Components.Weapon
{
    public interface IWeaponComponent : IComponent, IMonoComponent
    {
        void AddTarget(AttackData attackData, AttackType attackType);
        bool HasEnoughRange(float distance);
        void ResetTarget();
        float AttackDistance { get; }
    }
}
