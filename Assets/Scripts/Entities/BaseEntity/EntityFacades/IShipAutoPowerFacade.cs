using EmpireAtWar.Components.Ship.Health;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IShipAutoPowerFacade : IEntityFacade
    {
        bool Eligible { get; }
        bool Enabled { get; }
        float MaxShields { get; }
        bool ShieldSuppressed { get; }
        int ShieldRevision { get; }
        bool CanRegenerateShields { get; }
        void SetEnabled(bool enabled);
        bool CanEngage(ShipClass targetClass, Vector3 position);
    }
}
