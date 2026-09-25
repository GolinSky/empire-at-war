using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using Zenject;

namespace EmpireAtWar.Components.Combat
{
    /// <summary>
    /// Keeps one entity's combat modifiers in sync with its faction's completed research.
    /// </summary>
    public sealed class ResearchCombatModifier : IInitializable, IDisposable
    {
        private const float NEUTRAL_MULTIPLIER = 1f;

        private readonly CombatModifiers _modifiers;
        private readonly IFactionResearchModelObserver _research;
        private readonly ShipClass _shipClass;
        private CombatStatModifier _applied;

        public ResearchCombatModifier(
            CombatModifiers modifiers,
            IFactionResearchModelObserver research,
            IHealthData healthData)
        {
            _modifiers = modifiers;
            _research = research;
            _shipClass = healthData.ShipClass;
        }

        public void Initialize()
        {
            _applied = CreateModifier();
            _modifiers.Add(_applied);
            _research.OnResearchCompleted += HandleResearchCompleted;
        }

        public void Dispose()
        {
            _research.OnResearchCompleted -= HandleResearchCompleted;
        }

        private void HandleResearchCompleted(ResearchType researchType)
        {
            _modifiers.Remove(_applied);
            _applied = CreateModifier();
            _modifiers.Add(_applied);
        }

        private CombatStatModifier CreateModifier()
        {
            return new CombatStatModifier(
                _research.GetMultiplier(ResearchStat.WeaponDamage, _shipClass),
                NEUTRAL_MULTIPLIER,
                _research.GetMultiplier(ResearchStat.Speed, _shipClass),
                NEUTRAL_MULTIPLIER,
                _research.GetMultiplier(ResearchStat.DamageTaken, _shipClass));
        }
    }
}
