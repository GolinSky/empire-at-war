using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Enemy;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class TeamAiCooperationTests
    {
        private static readonly PlayerId LOW = new PlayerId(1);
        private static readonly PlayerId HIGH = new PlayerId(3);
        private static readonly PlayerId FOCUS = new PlayerId(0);
        private static readonly PlayerId OTHER_FOCUS = new PlayerId(2);

        [Test]
        public void Reservation_AllyClaimBlocksUnheldObjective()
        {
            var cooperation = new TeamAiCooperation();
            cooperation.PublishReservations(HIGH, new[] { "zone:0" });

            Assert.That(cooperation.IsReservedByAlly(LOW, "zone:0"), Is.True);
            Assert.That(cooperation.IsReservedByAlly(LOW, "zone:1"), Is.False);
            Assert.That(cooperation.IsReservedByAlly(HIGH, "zone:0"), Is.False, "Own reservations never block self.");
        }

        [Test]
        public void Reservation_SimultaneousClaimResolvesToLowerPlayerOnly()
        {
            var cooperation = new TeamAiCooperation();
            cooperation.PublishReservations(LOW, new[] { "zone:0" });
            cooperation.AddReservations(HIGH, new[] { "zone:0" });

            Assert.That(cooperation.IsReservedByAlly(LOW, "zone:0"), Is.False);
            Assert.That(cooperation.IsReservedByAlly(HIGH, "zone:0"), Is.True);
        }

        [Test]
        public void Reservation_PublishReplacesAndRemovePlayerReleases()
        {
            var cooperation = new TeamAiCooperation();
            cooperation.PublishReservations(HIGH, new[] { "zone:0" });
            cooperation.PublishReservations(HIGH, new[] { "site:2" });

            Assert.That(cooperation.IsReservedByAlly(LOW, "zone:0"), Is.False);
            Assert.That(cooperation.IsReservedByAlly(LOW, "site:2"), Is.True);
            cooperation.RemovePlayer(HIGH);
            Assert.That(cooperation.IsReservedByAlly(LOW, "site:2"), Is.False);
        }

        [Test]
        public void Strike_AddsOnlyFreshAvailableAlliesWithSameFocus()
        {
            var cooperation = new TeamAiCooperation();
            cooperation.PublishStrike(HIGH, Force(1), FOCUS, Vector3.zero, true, 0f);
            cooperation.PublishStrike(new PlayerId(4), Force(1), OTHER_FOCUS, Vector3.zero, true, 0f);
            cooperation.PublishStrike(new PlayerId(5), Force(1), FOCUS, Vector3.zero, false, 0f);
            cooperation.PublishStrike(LOW, Force(1), FOCUS, Vector3.zero, true, 0f);

            ForceComposition combined = Force(1);
            cooperation.AddAlliedStrikeStrength(combined, LOW, FOCUS, 1f);
            Assert.That(combined.UnitCount, Is.EqualTo(2), "Only the available ally on the same focus joins; self is not added twice.");

            ForceComposition stale = Force(1);
            cooperation.AddAlliedStrikeStrength(stale, LOW, FOCUS, TeamAiCooperation.STALE_SECONDS + 1f);
            Assert.That(stale.UnitCount, Is.EqualTo(1), "Stale published forces are ignored.");
        }

        private const float MIN_ADVANTAGE = 0.8f;

        [Test]
        public void Defense_EveryAvailableAllyAnswersAndTheRequesterDoesNot()
        {
            var cooperation = new TeamAiCooperation();
            var requester = new PlayerId(0);
            cooperation.PublishStrike(requester, Force(1), FOCUS, Vector3.zero, false, 0f);
            cooperation.PublishStrike(LOW, Force(1), FOCUS, new Vector3(500f, 0f, 0f), true, 0f);
            cooperation.PublishStrike(HIGH, Force(1), FOCUS, new Vector3(100f, 0f, 0f), true, 0f);
            cooperation.RequestDefense(requester, Vector3.zero, 0f);

            Assert.That(cooperation.TryGetDefenseAssignment(HIGH, 1f, MIN_ADVANTAGE, out PlayerId answered,
                out Vector3 station), Is.True);
            Assert.That(answered, Is.EqualTo(requester));
            Assert.That(station, Is.EqualTo(Vector3.zero));
            Assert.That(cooperation.TryGetDefenseAssignment(LOW, 1f, MIN_ADVANTAGE, out _, out _), Is.True,
                "Not only the nearest ally answers.");
            Assert.That(cooperation.TryGetDefenseAssignment(requester, 1f, MIN_ADVANTAGE, out _, out _), Is.False);
            Assert.That(cooperation.HasDefenseRequest(1f), Is.True);

            cooperation.ClearDefense(requester);
            Assert.That(cooperation.TryGetDefenseAssignment(HIGH, 1f, MIN_ADVANTAGE, out _, out _), Is.False);
            Assert.That(cooperation.HasDefenseRequest(1f), Is.False);
        }

        [Test]
        public void Defense_StaleRequestOrUnavailableResponderIsIgnored()
        {
            var cooperation = new TeamAiCooperation();
            var requester = new PlayerId(0);
            cooperation.PublishStrike(HIGH, Force(1), FOCUS, Vector3.zero, true, 0f);
            cooperation.RequestDefense(requester, Vector3.zero, 0f);
            Assert.That(cooperation.TryGetDefenseAssignment(HIGH, TeamAiCooperation.STALE_SECONDS + 1f, MIN_ADVANTAGE,
                out _, out _), Is.False);
            Assert.That(cooperation.HasDefenseRequest(TeamAiCooperation.STALE_SECONDS + 1f), Is.False);

            cooperation.RequestDefense(requester, Vector3.zero, 20f);
            cooperation.PublishStrike(HIGH, Force(1), FOCUS, Vector3.zero, false, 20f);
            Assert.That(cooperation.TryGetDefenseAssignment(HIGH, 20f, MIN_ADVANTAGE, out _, out _), Is.False);
        }

        [Test]
        public void Defense_AnsweredOnlyWhenTheAlliesTogetherWithTheStationBeatTheAttackers()
        {
            var cooperation = new TeamAiCooperation();
            var requester = new PlayerId(0);
            cooperation.PublishStrike(LOW, Force(1), FOCUS, Vector3.zero, true, 0f);
            cooperation.PublishStrike(HIGH, Force(1), FOCUS, Vector3.zero, true, 0f);
            cooperation.RequestDefense(requester, Vector3.zero, 0f, attackers: Force(3), defenders: Force(1));

            Assert.That(cooperation.TryGetDefenseAssignment(LOW, 1f, MIN_ADVANTAGE, out _, out _), Is.True,
                "Two weak fleets and the station together beat the attackers: both go.");
            Assert.That(cooperation.TryGetDefenseAssignment(HIGH, 1f, MIN_ADVANTAGE, out _, out _), Is.True);

            cooperation.PublishStrike(HIGH, Force(1), FOCUS, Vector3.zero, false, 1f);
            Assert.That(cooperation.TryGetDefenseAssignment(LOW, 1f, MIN_ADVANTAGE, out _, out _), Is.False,
                "Alone the remaining fleet would lose: it does not go.");
        }

        [Test]
        public void ArmadaStrength_AddsEveryAvailableAllyWhateverItsFocus()
        {
            var cooperation = new TeamAiCooperation();
            cooperation.PublishStrike(HIGH, Force(1), FOCUS, Vector3.zero, true, 0f);
            cooperation.PublishStrike(new PlayerId(4), Force(1), OTHER_FOCUS, Vector3.zero, true, 0f);
            cooperation.PublishStrike(new PlayerId(5), Force(1), FOCUS, Vector3.zero, false, 0f);
            cooperation.PublishStrike(LOW, Force(1), FOCUS, Vector3.zero, true, 0f);

            ForceComposition combined = Force(1);
            cooperation.AddAvailableAlliedStrength(combined, LOW, 1f);

            Assert.That(combined.UnitCount, Is.EqualTo(3), "Both available allies join; the unavailable one and self do not.");
        }

        private static ForceComposition Force(int units)
        {
            var damage = new float[ForceComposition.ClassSlots];
            damage[(int)ShipClass.Capital] = 10f;
            var profile = new UnitCombatProfile(ShipClass.Capital, 100f, 0f, damage, damage, damage,
                Array.Empty<UnitCombatProfile>());
            var force = new ForceComposition();
            for (int i = 0; i < units; i++) force.Add(profile, 100f, 0f, 1f);
            return force;
        }
    }
}
