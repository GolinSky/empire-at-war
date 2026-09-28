
namespace EmpireAtWar.Services.Battle
{
    public interface ISelectionSubject
    {
        ISelectionContext PlayerSelectionContext { get; }
        ISelectionContext OtherSelectionContext { get; }
        SelectionScope UpdatedScope { get; }
    }
}
