using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    /// <summary>Hull-mounted gravity well generator effects of a ship that carries the Interdict ability.</summary>
    public interface IGravityWellEmitterFacade : IEntityFacade
    {
        /// <summary>World box the effects can cover; tested against the battle camera.</summary>
        Bounds CullBounds { get; }

        void SetEmitting(bool emitting);

        /// <summary>Pauses new particles off-screen; live ones keep simulating and drain.</summary>
        void SetOnScreen(bool onScreen);
    }
}
