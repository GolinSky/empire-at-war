using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.Components.Squadrons.Health
{
    public interface ISquadronHealthData
    {
        ShipClass ShipClass { get; }
        float MemberHull { get; }
        float MemberShields { get; }
        float ShieldRegenerateValue { get; }
        float ShieldRegenerateDelay { get; }
    }
}
