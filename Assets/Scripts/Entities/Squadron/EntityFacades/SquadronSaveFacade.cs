using System.Linq;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Saves;
using UnityEngine;

namespace EmpireAtWar.Entities.Squadrons.EntityFacades
{
    /// <summary>A squadron is saved at its fighters' centroid, facing their heading; it reforms around that point on load.</summary>
    public sealed class SquadronSaveFacade : IUnitSaveFacade
    {
        private readonly long _entityId;
        private readonly PlayerId _owner;
        private readonly SquadronType _squadronType;
        private readonly ISquadronFlightComponent _flight;
        private readonly SquadronHealthModel _health;
        private readonly UnitOrderModel _orders;

        public SquadronSaveFacade(long entityId, PlayerId owner, SquadronType squadronType,
            ISquadronFlightComponent flight, SquadronHealthModel health, UnitOrderModel orders)
        {
            _entityId = entityId;
            _owner = owner;
            _squadronType = squadronType;
            _flight = flight;
            _health = health;
            _orders = orders;
        }

        public UnitSave Capture() => UnitSave.Create(_entityId, _owner, UnitTypeId.Squadron(_squadronType),
            _flight.Centroid, Quaternion.LookRotation(_flight.Heading, Vector3.up),
            UnitHealthSave.ForSquadron(_health.Members.Select(member => member.HealthPercentage).ToArray(),
                Enumerable.Range(0, _health.Members.Length).Select(_health.GetMemberShieldPercentage).ToArray()),
            UnitOrderSave.From(_orders));

        public void RestoreHealth(UnitHealthSave save) => _health.Restore(save.MemberHull, save.MemberShields);
    }
}
