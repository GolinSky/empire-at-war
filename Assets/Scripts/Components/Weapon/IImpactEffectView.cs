using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    public interface IImpactEffectView
    {
        void Emit(ImpactSurface surface, Vector3 position, Vector3 direction, float size);
    }
}
