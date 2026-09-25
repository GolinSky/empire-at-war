using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    public interface IWeaponComponent : IComponent, IMonoComponent
    {
        void AddTarget(AttackData attackData, AttackType attackType);
        bool HasEnoughRange(float distance);
        void ResetTarget();
        float GetFiringTurnAngle(Vector3 targetPosition);
        float AttackDistance { get; }
    }
}
