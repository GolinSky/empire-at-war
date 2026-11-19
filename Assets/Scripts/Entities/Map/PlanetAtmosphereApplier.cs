using EmpireAtWar.Components.Environment;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Entities.Map
{
    public sealed class PlanetAtmosphereApplier : IInitializable, ILateDisposable, IObserver<BattleMap>
    {
        private readonly IGameModelObserver _gameModel;
        private readonly MapGenerationSettings _settings;
        private readonly BattleAtmosphereView _view;
        private readonly INotifier<BattleMap> _battleMap;

        public PlanetAtmosphereApplier(IGameModelObserver gameModel, MapGenerationSettings settings,
            BattleAtmosphereView view, INotifier<BattleMap> battleMap)
        {
            _gameModel = gameModel;
            _settings = settings;
            _view = view;
            _battleMap = battleMap;
        }

        public void Initialize() => _battleMap.AddObserver(this);

        public void LateDispose() => _battleMap.RemoveObserver(this);

        public void UpdateState(BattleMap battleMap)
        {
            PlanetMapProfileEntry profile = _settings.Profiles.Get(_gameModel.PlanetType);
            _view.Apply(_settings.Atmospheres.Get(profile.Biome));
        }
    }
}
