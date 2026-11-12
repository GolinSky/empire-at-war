using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Services.Enemy;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class ForceCompositionBuilderTests
    {
        private const float HULL = 500f;
        private const float SHIELDS = 200f;

        [Test]
        public void BuildKnown_CountsStaleSightingsAtFullStrength_UntilForgotten()
        {
            UnitCombatProfileCatalog catalog = new UnitCombatProfileCatalog(null, null, null, null);
            UnitCombatProfile profile = new UnitCombatProfile(ShipClass.Fighter, 1f, 0f,
                new float[ForceComposition.ClassSlots], new float[ForceComposition.ClassSlots],
                new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>());
            var profiles = (Dictionary<UnitTypeId, UnitCombatProfile>)typeof(UnitCombatProfileCatalog)
                .GetField("_profiles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(catalog);
            profiles.Add(UnitTypeId.Squadron(SquadronType.Vulture), profile);
            HostileIntelModel intel = new HostileIntelModel();
            intel.Report(new HostileSighting(1, UnitTypeId.Squadron(SquadronType.Vulture), TestPlayers.Human,
                new FormationPoint(0f, 0f), HULL, SHIELDS, 1f, seenAt: 0f));
            ForceCompositionBuilder builder = new ForceCompositionBuilder(new EntityLocator(), catalog);
            ForceComposition force = new ForceComposition();

            // Stale but remembered: trust in the position has faded, the unit's strength has not.
            builder.BuildKnown(force, intel, IntelConfidence.STALE_AGE, sighting => true);
            Assert.That(force.Hull(ShipClass.Fighter), Is.EqualTo(HULL));
            Assert.That(force.Shields(ShipClass.Fighter), Is.EqualTo(SHIELDS));

            builder.BuildKnown(force, intel, IntelConfidence.FORGET_AGE, sighting => true);
            Assert.That(force.IsEmpty, Is.True);
        }
    }
}
