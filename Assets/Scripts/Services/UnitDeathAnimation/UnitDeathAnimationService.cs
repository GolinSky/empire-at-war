using System;
using System.Collections.Generic;
using DG.Tweening;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Services.UnitDeathAnimation
{
    public interface IUnitDeathAnimationData
    {
        Vector3 FallDownDirection { get; }
        Vector3 FallDownRotation { get; }
        float FallDownDuration { get; }
    }

    public interface IUnitDeathAnimationService : IService
    {
        void Play(Transform unitTransform, IUnitDeathAnimationData data);
    }

    public sealed class UnitDeathAnimationService : Service, IUnitDeathAnimationService, IDisposable
    {
        private readonly Dictionary<Transform, Sequence> _animations =
            new Dictionary<Transform, Sequence>();

        public void Play(Transform unitTransform, IUnitDeathAnimationData data)
        {
            if (unitTransform == null)
            {
                throw new ArgumentNullException(nameof(unitTransform));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.FallDownDuration <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(data),
                    data.FallDownDuration,
                    "Fall-down duration must be greater than zero.");
            }

            if (_animations.TryGetValue(unitTransform, out Sequence currentAnimation))
            {
                currentAnimation.Kill();
            }

            Vector3 targetPosition =
                unitTransform.position - data.FallDownDirection;
            Sequence animation = DOTween.Sequence();
            animation.Append(
                unitTransform.DOMove(targetPosition, data.FallDownDuration));
            animation.Join(
                unitTransform.DOLocalRotate(
                    data.FallDownRotation,
                    data.FallDownDuration));
            _animations[unitTransform] = animation;
            animation.OnKill(() => RemoveAnimation(unitTransform, animation));
        }

        public void Dispose()
        {
            List<Sequence> animations =
                new List<Sequence>(_animations.Values);
            _animations.Clear();

            foreach (Sequence animation in animations)
            {
                animation.Kill();
            }
        }

        private void RemoveAnimation(Transform unitTransform, Sequence animation)
        {
            if (_animations.TryGetValue(unitTransform, out Sequence currentAnimation) &&
                currentAnimation == animation)
            {
                _animations.Remove(unitTransform);
            }
        }
    }
}
