using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    public sealed class ImpactEffectPresenter
    {
        private readonly IImpactEffectView _view;
        private readonly DamageMatrixData _damageMatrix;

        public ImpactEffectPresenter(IImpactEffectView view, DamageMatrixData damageMatrix)
        {
            _view = view;
            _damageMatrix = damageMatrix;
        }

        public ImpactSurface ResolveSurface(IHealthModelObserver target, DamageType damageType)
        {
            if (target.IsDestroyed) return ImpactSurface.None;
            return target.HasShields && !_damageMatrix.IsShieldPiercing(damageType)
                ? ImpactSurface.Shield : ImpactSurface.Armor;
        }

        public void Play(ImpactSurface surface, Vector3 position, Vector3 direction, float size)
        {
            if (surface != ImpactSurface.None)
                _view.Emit(surface, position, direction, size);
        }
    }
}
