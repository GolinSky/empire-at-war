using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Services.Hazards;
using UnityEngine;

namespace EmpireAtWar.Components.Environment
{
    public sealed class HazardEffectsView : MonoBehaviour, IHazardEffectsView
    {
        [SerializeField] private HazardEntryCue hazardEntryPrefab;

        public void ShowHazardEntry(IEntity entity, SpaceHazardData data)
        {
            Transform target = entity.GetFacade<IEntityTransformFacade>().Transform;
            HazardEntryCue cue = Instantiate(hazardEntryPrefab, target.position, Quaternion.identity, target);
            cue.Configure(data, entity.GetFacade<IMoveFacade>().NavigationRadius);
        }
    }
}
