using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Services.UnitOrders
{
    /// <summary>Cap by target <see cref="ShipClass"/> from <see cref="UnitOrderSettings"/>.</summary>
    public sealed class FocusFireCaps : IFocusFireCaps
    {
        public const int UNLIMITED = 0;

        private readonly UnitOrderSettings _settings;

        public FocusFireCaps(UnitOrderSettings settings) => _settings = settings;

        public int GetCap(IEntity target, bool isSquadron) =>
            _settings.GetFocusFireCap(target.HealthModel.ShipClass, isSquadron);
    }
}
