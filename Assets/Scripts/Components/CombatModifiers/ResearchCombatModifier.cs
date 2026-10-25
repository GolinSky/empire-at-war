using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Components.Combat
{
    /// <summary>
    /// Keeps one entity's combat modifiers in sync with its faction's completed research.
    /// </summary>
    public sealed class ResearchCombatModifier : IInitializable, IOrderedDisposable
    {
        private const float NEUTRAL_MULTIPLIER = 1f;

        private readonly IReadOnlyList<IFactionResearchModelObserver> _research;

        private readonly CombatModifiers _modifiers;

        private readonly ShipClass _shipClass;
        private CombatModifierHandle _applied;

        public ResearchCombatModifier(
            IReadOnlyList<IFactionResearchModelObserver> research,
            IHealthData healthData,
            CombatModifiers modifiers)
        {
            _modifiers = modifiers;
            _research = research;
            _shipClass = healthData.ShipClass;
        }

        public void Initialize()
        {
            _applied = _modifiers.Add(CreateModifier());
            foreach (IFactionResearchModelObserver research in _research)
                research.OnResearchCompleted += HandleResearchCompleted;
        }

        public void Dispose()
        {
            foreach (IFactionResearchModelObserver research in _research)
                research.OnResearchCompleted -= HandleResearchCompleted;
        }

        private void HandleResearchCompleted(ResearchType researchType)
        {
            _modifiers.Remove(_applied);
            _applied = _modifiers.Add(CreateModifier());
        }

        private CombatStatModifier CreateModifier()
        {
            float damage = NEUTRAL_MULTIPLIER;
            float speed = NEUTRAL_MULTIPLIER;
            float damageTaken = NEUTRAL_MULTIPLIER;
            foreach (IFactionResearchModelObserver research in _research)
            {
                damage *= research.GetMultiplier(ResearchStat.WeaponDamage, _shipClass);
                speed *= research.GetMultiplier(ResearchStat.Speed, _shipClass);
                damageTaken *= research.GetMultiplier(ResearchStat.DamageTaken, _shipClass);
            }
            return new CombatStatModifier(damage, NEUTRAL_MULTIPLIER, speed, NEUTRAL_MULTIPLIER, damageTaken);
        }
    }
}
