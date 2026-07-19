using Utilities.ScriptUtils.Math;

namespace EmpireAtWar.Models.Health
{
    public interface IHealthData
    {
        float Armor { get; }
        float Dexterity { get; }
        float Shields { get; }
        float ShieldRegenerateValue { get; }
        float ShieldRegenerateDelay { get; }
        FloatRange ShieldDangerStateRange { get; }
    }

    public interface IHealthState
    {
        bool HasShields { get; }
        float Dexterity { get; }
        float Shields { get; }
    }
}
