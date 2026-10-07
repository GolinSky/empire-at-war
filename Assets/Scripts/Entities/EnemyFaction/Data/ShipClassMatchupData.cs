using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using UnityEngine;

namespace EmpireAtWar.Entities.EnemyFaction.Data
{
    /// <summary>
    /// The AI's strong/weak-against table per ship class. Production multiplies each candidate's score by how much
    /// of the hostile force (hull + shields) sits in classes the candidate is strong or weak against.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(ShipClassMatchupData), menuName = "Data/AI/Ship Class Matchups")]
    public sealed class ShipClassMatchupData : Mvc.Data, IShipClassMatchups
    {
        [SerializeField] private List<ShipClassMatchup> matchups = new List<ShipClassMatchup>();

        [Tooltip("Score bonus when the whole hostile force is made of classes the candidate is strong against.")]
        [SerializeField, Min(0f)] private float strongBonus = 0.5f;

        [Tooltip("Score penalty (0..1) when the whole hostile force is made of classes the candidate is weak against.")]
        [SerializeField, Range(0f, 1f)] private float weakPenalty = 0.5f;

        private Dictionary<ShipClass, ShipClassMatchup> _byClass;

        public float GetPreference(ShipClass candidate, ForceComposition hostile)
        {
            if (!GetRows().TryGetValue(candidate, out ShipClassMatchup row))
            {
                return 1f;
            }

            float total = 0f;
            foreach (ShipClass shipClass in ForceComposition.Classes)
            {
                total += Durability(hostile, shipClass);
            }

            if (total <= 0f)
            {
                return 1f;
            }

            float strongShare = Share(hostile, row.StrongAgainst, total);
            float weakShare = Share(hostile, row.WeakAgainst, total);
            return (1f + strongBonus * strongShare) * (1f - weakPenalty * weakShare);
        }

        private Dictionary<ShipClass, ShipClassMatchup> GetRows()
        {
            if (_byClass == null)
            {
                _byClass = new Dictionary<ShipClass, ShipClassMatchup>();
                foreach (ShipClassMatchup row in matchups)
                    _byClass.Add(row.ShipClass, row);
            }

            return _byClass;
        }

        private static float Share(ForceComposition hostile, ShipClass[] classes, float total)
        {
            float sum = 0f;
            foreach (ShipClass shipClass in classes)
            {
                sum += Durability(hostile, shipClass);
            }

            return sum / total;
        }

        private static float Durability(ForceComposition force, ShipClass shipClass) =>
            force.Hull(shipClass) + force.Shields(shipClass);
    }
}
