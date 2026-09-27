using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Squadrons;

namespace EmpireAtWar.Components.Hangar
{
    public interface IHangarCommand : IEntityFacade
    {
        /// <summary>Launches a squadron outside the bay reserve, e.g. one bought at the space station.</summary>
        ISquadron Launch(SquadronType squadronType);
    }
}
