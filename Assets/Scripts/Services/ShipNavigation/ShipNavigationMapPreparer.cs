using EmpireAtWar.Entities.Map;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.ShipNavigation
{
    // Builds the navigation hazard raster during map loading, so the battle's first ship plan does not pay for it.
    public sealed class ShipNavigationMapPreparer : Service, IInitializable, ILateDisposable, IObserver<BattleMap>
    {
        private readonly INotifier<BattleMap> _battleMap;
        private readonly ShipNavigationService _navigation;

        public ShipNavigationMapPreparer(INotifier<BattleMap> battleMap, ShipNavigationService navigation)
        {
            _battleMap = battleMap;
            _navigation = navigation;
        }

        public void Initialize()
        {
            _battleMap.AddObserver(this);
        }

        public void LateDispose()
        {
            _battleMap.RemoveObserver(this);
        }

        // Layout.Hazards is the same ordered list HazardService exposes as Sources, so planning reuses this raster.
        public void UpdateState(BattleMap battleMap)
        {
            _navigation.PrepareMap(battleMap.Layout.SizeRange, battleMap.Layout.Hazards);
        }
    }
}
