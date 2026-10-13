using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.BaseEntity
{
    public interface IViewEntity
    {
        long Id { get; }
        PlayerId Owner { get; }
    }
}
