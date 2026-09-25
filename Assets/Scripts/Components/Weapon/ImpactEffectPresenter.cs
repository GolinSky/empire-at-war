using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
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

        public Vector3 GetImpactPosition(IHealthModelObserver target, DamageType damageType,
            Vector3 origin, Vector3 position)
        {
            return target is IShieldTarget shield
                ? shield.GetImpactPosition(origin, position, damageType)
                : position;
        }

        public void Play(IHealthModelObserver target, ImpactSurface surface, Vector3 position,
            Vector3 direction, float size)
        {
            if (surface == ImpactSurface.Shield && target is IShieldTarget shield &&
                shield.ShowShieldImpact(position)) return;
            Play(surface, position, direction, size);
        }
    }
}
