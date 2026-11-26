using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Entities.Units.Matchups
{
    /// <summary>
    /// The one table of which units beat which. Tooltips read it through <see cref="UnitMatchupResolver"/>; enemy
    /// production reads it as <see cref="IShipClassMatchups"/>, using the class part of each rule (the AI counts forces
    /// by class, so role sides only reach tooltips).
    /// </summary>
    [CreateAssetMenu(fileName = nameof(MatchupRules), menuName = "Data/Units/Matchup Rules")]
    public sealed class MatchupRules : Data, IShipClassMatchups
    {
        [SerializeField] private MatchupRule[] rules = Array.Empty<MatchupRule>();
        [SerializeField] private MatchupGroup[] groups = Array.Empty<MatchupGroup>();

        [Header("AI production")]
        [Tooltip("Score bonus when the whole hostile force is made of classes the candidate is strong against.")]
        [SerializeField, Min(0f)] private float strongBonus = 0.5f;

        [Tooltip("Score penalty (0..1) when the whole hostile force is made of classes the candidate is weak against.")]
        [SerializeField, Range(0f, 1f)] private float weakPenalty = 0.5f;

        private Dictionary<ShipClass, HashSet<ShipClass>> _beats;
        private Dictionary<ShipClass, HashSet<ShipClass>> _beatenBy;

        public IReadOnlyList<MatchupRule> Rules => rules;
        public IReadOnlyList<MatchupGroup> Groups => groups;

        public float GetPreference(ShipClass candidate, ForceComposition hostile)
        {
            BuildClassTables();
            float total = 0f;
            foreach (ShipClass shipClass in ForceComposition.Classes)
                total += Durability(hostile, shipClass);
            if (total <= 0f) return 1f;

            float strongShare = Share(hostile, _beats, candidate, total);
            float weakShare = Share(hostile, _beatenBy, candidate, total);
            return (1f + strongBonus * strongShare) * (1f - weakPenalty * weakShare);
        }

        private void BuildClassTables()
        {
            if (_beats != null) return;
            _beats = new Dictionary<ShipClass, HashSet<ShipClass>>();
            _beatenBy = new Dictionary<ShipClass, HashSet<ShipClass>>();
            foreach (MatchupRule rule in rules)
            foreach (ShipClass attacker in rule.Attacker.Classes)
            foreach (ShipClass target in rule.Target.Classes)
            {
                Add(_beats, attacker, target);
                Add(_beatenBy, target, attacker);
            }
        }

        private static void Add(Dictionary<ShipClass, HashSet<ShipClass>> table, ShipClass key, ShipClass value)
        {
            if (!table.TryGetValue(key, out HashSet<ShipClass> set)) table.Add(key, set = new HashSet<ShipClass>());
            set.Add(value);
        }

        private static float Share(ForceComposition hostile, Dictionary<ShipClass, HashSet<ShipClass>> table,
            ShipClass candidate, float total)
        {
            if (!table.TryGetValue(candidate, out HashSet<ShipClass> classes)) return 0f;
            float sum = 0f;
            foreach (ShipClass shipClass in classes)
                sum += Durability(hostile, shipClass);
            return sum / total;
        }

        private static float Durability(ForceComposition force, ShipClass shipClass) =>
            force.Hull(shipClass) + force.Shields(shipClass);
    }
}
