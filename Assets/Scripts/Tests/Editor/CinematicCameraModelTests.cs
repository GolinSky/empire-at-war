using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.CinematicCamera.Model;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Camera;
using NUnit.Framework;
using NumericsVector3 = System.Numerics.Vector3;

namespace EmpireAtWar.Tests.CinematicCamera
{
    public sealed class CinematicCameraModelTests
    {
        [Test]
        public void Sequencer_NeverRepeatsShotTypeAndKeepsDurationInRange()
        {
            CinematicShotSequencer sequencer = new CinematicShotSequencer(new Random(7), 3f, 8f);
            CinematicShot shot = sequencer.First();
            Assert.That(shot.Type, Is.EqualTo(CinematicShotType.Wide));

            for (int i = 0; i < 200; i++)
            {
                CinematicShot next = sequencer.Next(shot.Type);
                Assert.That(next.Type, Is.Not.EqualTo(shot.Type));
                Assert.That(next.Duration, Is.InRange(3f, 8f));
                Assert.That(Math.Abs(next.Side), Is.EqualTo(1f));
                shot = next;
            }
        }

        [Test]
        public void ActivityTracker_RecordsOnlyHealthDecreases()
        {
            CinematicActivityTracker tracker = new CinematicActivityTracker();
            tracker.Sample(1, 100f, 0f);
            tracker.Sample(1, 110f, 1f);
            Assert.That(tracker.GetSecondsSinceDamaged(1, 2f), Is.EqualTo(float.PositiveInfinity));

            tracker.Sample(1, 90f, 3f);
            Assert.That(tracker.GetSecondsSinceDamaged(1, 5f), Is.EqualTo(2f));
        }

        [Test]
        public void Scorer_PrefersRecentlyDamagedUnitInsideEngagement()
        {
            CinematicCameraSettings settings = new CinematicCameraSettings();
            CinematicInterestScorer scorer = new CinematicInterestScorer(settings, new Random(1));
            CinematicCandidate[] candidates =
            {
                new(1, new NumericsVector3(0f, 0f, 0f), ShipClass.Frigate, PlayerType.Player, 0.5f),
                new(2, new NumericsVector3(50f, 0f, 0f), ShipClass.Frigate, PlayerType.Opponent, float.PositiveInfinity),
                new(3, new NumericsVector3(5000f, 0f, 0f), ShipClass.HeavyCapital, PlayerType.Player, float.PositiveInfinity),
            };

            bool found = scorer.TrySelect(candidates, -1, out CinematicSelection selection);

            Assert.That(found, Is.True);
            Assert.That(selection.Target.Id, Is.EqualTo(1));
            Assert.That(selection.FocusOffset, Is.EqualTo(new NumericsVector3(25f, 0f, 0f)));
        }

        [Test]
        public void Scorer_ReturnsFalseWithoutCandidates()
        {
            CinematicInterestScorer scorer = new CinematicInterestScorer(new CinematicCameraSettings(), new Random(1));

            Assert.That(scorer.TrySelect(Array.Empty<CinematicCandidate>(), -1, out _), Is.False);
        }
    }
}
