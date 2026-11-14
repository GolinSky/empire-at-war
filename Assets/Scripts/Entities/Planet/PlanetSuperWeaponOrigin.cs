using System;
using EmpireAtWar.Services.SuperWeapons;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Entities.Planet
{
    /// <summary>Adapts the battle planet into the superweapon origin: shots leave the surface point facing the target.</summary>
    public class PlanetSuperWeaponOrigin : MonoBehaviour, ISuperWeaponOrigin, IDisposable
    {
        private ISuperWeaponOriginRegistry _registry;

        [SerializeField] private Transform planetTransform;

        [SerializeField, Tooltip("Surface radius at a planet scale of 1.")] private float surfaceRadius;

        [Inject]
        private void Construct(ISuperWeaponOriginRegistry registry)
        {
            registry.Register(this);
            _registry = registry;
        }

        public void Dispose()
        {
            _registry.Unregister(this);
        }

        public Vector3 GetFirePosition(Vector3 targetPosition)
        {
            Vector3 center = planetTransform.position;
            return center + (targetPosition - center).normalized * surfaceRadius * transform.lossyScale.x;
        }
    }
}
