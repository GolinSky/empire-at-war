using EmpireAtWar.Controllers.Factions;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemyPurchaseProcessor
    {
        void Purchase(UnitRequest unitRequest);
    }
}
