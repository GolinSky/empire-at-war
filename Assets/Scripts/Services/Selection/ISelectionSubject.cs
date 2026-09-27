using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Battle
{
    public interface ISelectionSubject
    {
        ISelectionContext PlayerSelectionContext { get; }
        ISelectionContext EnemySelectionContext { get; }
        PlayerType UpdatedType { get; }
    }
}
