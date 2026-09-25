using System;
using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Entities.SuperWeapons
{
    /// <summary>The player's superweapon that waits for a target click.</summary>
    public sealed class SuperWeaponTargetingModel
    {
        public event Action Changed;
        public event Action<SuperWeaponType, IEntity> TargetSubmitted;

        public SuperWeaponType? Pending { get; private set; }

        public void Start(SuperWeaponType type)
        {
            Pending = type;
            Changed?.Invoke();
        }

        public void Submit(IEntity target)
        {
            if (Pending == null) return;
            TargetSubmitted?.Invoke(Pending.Value, target);
        }

        public void Cancel()
        {
            if (Pending == null) return;
            Pending = null;
            Changed?.Invoke();
        }
    }
}
