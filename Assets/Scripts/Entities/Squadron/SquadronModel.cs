using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.Squadrons
{
    public class SquadronModel : IModel, ISquadronModelObserver
    {
        public SquadronType SquadronType { get; }

        public SquadronModel(SquadronType squadronType)
        {
            SquadronType = squadronType;
        }
    }
}
