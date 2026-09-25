using EmpireAtWar.Services.SuperWeapons;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.Planet
{
    /// <summary>Adapts the battle planet into the superweapon origin: shots leave the surface point facing the target.</summary>
    public class PlanetSuperWeaponOrigin : MonoBehaviour, ISuperWeaponOrigin
    {
        [SerializeField] private Transform planetTransform;
        [SerializeField] private float surfaceRadius;

        private ISuperWeaponOriginRegistry _registry;

        [Inject]
        private void Construct(ISuperWeaponOriginRegistry registry)
        {
            _registry = registry;
            _registry.Register(this);
        }

        private void OnDestroy()
        {
            _registry.Unregister(this);
        }

        public Vector3 GetFirePosition(Vector3 targetPosition)
        {
            Vector3 center = planetTransform.position;
            return center + (targetPosition - center).normalized * surfaceRadius;
        }
    }
}
