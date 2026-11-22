using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Utils;
using UnityEngine;

namespace EmpireAtWar.Entities.Squadrons.EntityFacades
{
    public sealed class SquadronFighterSpreadFacade : ISquadronFighterSpreadFacade
    {
        private readonly SquadronFlightModel _model;

        public SquadronFighterSpreadFacade(SquadronFlightModel model) => _model = model;

        public Vector3 FighterCentroid => _model.GetCentroid().ToUnity();

        public float LoiterRadius => _model.Data.LoiterRadius;
    }
}
