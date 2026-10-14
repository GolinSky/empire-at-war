using EmpireAtWar.Services.SuperWeapons;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Entities.Planet
{
    /// <summary>Adapts the battle planet into the superweapon origin: shots leave the surface point facing the target.</summary>
    public class PlanetSuperWeaponOrigin : MonoBehaviour, ISuperWeaponOrigin
    {
        private System.Collections.Generic.IReadOnlyList<ISuperWeaponOriginRegistry> _registries = System.Array.Empty<ISuperWeaponOriginRegistry>();

        [SerializeField] private Transform planetTransform;

        [SerializeField, Tooltip("Surface radius at a planet scale of 1.")] private float surfaceRadius;

        [Inject]
        private void Construct(System.Collections.Generic.IReadOnlyList<ISuperWeaponOriginRegistry> registries)
        {
            _registries = registries;
            foreach (var registry in _registries) registry.Register(this);
        }

        private void OnDestroy()
        {
            foreach (var registry in _registries) registry.Unregister(this);
        }

        public Vector3 GetFirePosition(Vector3 targetPosition)
        {
            Vector3 center = planetTransform.position;
            return center + (targetPosition - center).normalized * surfaceRadius * transform.lossyScale.x;
        }
    }
}
