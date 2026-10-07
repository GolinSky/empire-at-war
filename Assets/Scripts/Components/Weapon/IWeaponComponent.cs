using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Components.Weapon
{
    public interface IWeaponComponent : IComponent, IMonoComponent
    {
        float AttackDistance { get; }

        void AddTarget(AttackData attackData, AttackType attackType);

        bool HasEnoughRange(float distance, float contactDistance);

        void ResetTarget();
    }
}
