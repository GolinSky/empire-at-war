using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IAbilityFacingFacade : IEntityFacade
    {
        bool IsAbilityFacing { get; }
        void BeginAbilityFacing();
        void FaceAbility(Vector3 direction);
        void EndAbilityFacing();
    }
}
