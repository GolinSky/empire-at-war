using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Battle;
using LightWeightFramework.Components.Service;
using Zenject;

namespace EmpireAtWar.Services.BattleService
{
    public interface IBattleService : IService
    {
    }

    public class BattleService : Service, IBattleService, IObserver<ISelectionSubject>, IInitializable, ILateDisposable
    {
        private readonly ISelectionService _selectionService;

        public BattleService(ISelectionService selectionService)
        {
            _selectionService = selectionService;
        }
        
        public void Initialize()
        {
            _selectionService.AddObserver(this);
        }

        public void LateDispose()
        {
            _selectionService.RemoveObserver(this);
        }

        public void UpdateState(ISelectionSubject selectionSubject)
        {
            switch (selectionSubject.UpdatedType)
            {
                case PlayerType.Player:
                    break;
                case PlayerType.Opponent:
                    break;
                case PlayerType.None:
                    break;
            }
        }
    }
}