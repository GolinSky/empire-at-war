using EmpireAtWar.Models.Weapon;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public abstract class BaseTurretView : MonoBehaviour
    {
        public abstract bool IsBusy { get; }
        public abstract float Speed { get; }
        public abstract void SetData(ProjectileData projectileData, float attackDistance);
        public abstract void Attack(IHardPointView hardPointView, float duration);
        public abstract void SetParent(Transform parent);
        public abstract void ResetParent();
    }
}