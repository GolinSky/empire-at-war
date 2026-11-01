using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Entities.SpaceStation
{
    public sealed class StationDurabilityFacade : IStationDurabilityFacade
    {
        private readonly HealthModel _health;

        public StationDurabilityFacade(HealthModel health) => _health = health;

        public float MaximumDurability => _health.MaxHull + _health.MaxShields;
    }
}
