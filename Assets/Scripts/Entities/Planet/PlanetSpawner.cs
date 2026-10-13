using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Factory;

namespace EmpireAtWar.Entities.Planet
{
    public sealed class PlanetSpawner : IInitializable, ILateDisposable, IObserver<BattleMap>
    {
        private readonly IGameModelObserver _gameModel;
        private readonly PlanetFactory _factory;
        private readonly INotifier<BattleMap> _battleMap;

        public PlanetSpawner(IGameModelObserver gameModel, PlanetFactory factory, INotifier<BattleMap> battleMap)
        {
            _factory = factory;
            _battleMap = battleMap;
            _gameModel = gameModel;
        }

        public void Initialize() => _battleMap.AddObserver(this);

        public void LateDispose() => _battleMap.RemoveObserver(this);

        public void UpdateState(BattleMap battleMap)
        {
            _factory.Create(_gameModel.PlanetType, _gameModel.MapSize, battleMap.Layout.PlanetPosition);
        }
    }
}
