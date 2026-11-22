using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;

namespace EmpireAtWar.Entities.Squadrons.EntityFacades
{
    /// <summary>Read-only: where the squadron's simulated fighters are, independent of the root transform.</summary>
    public interface ISquadronFighterSpreadFacade : IEntityFacade
    {
        /// <summary>Centroid of the living fighters in the local flight simulation.</summary>
        Vector3 FighterCentroid { get; }

        float LoiterRadius { get; }
    }
}
