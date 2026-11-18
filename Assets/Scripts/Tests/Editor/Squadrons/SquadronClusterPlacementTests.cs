using EmpireAtWar.Components.Squadrons.Icon;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Squadrons")]
    public sealed class SquadronClusterPlacementTests
    {
        private const float PITCH = 84f;
        private const float RISE = 60f;
        private static readonly Rect SCREEN = new Rect(0f, 0f, 1920f, 1080f);
        private static readonly Vector2 CENTROID = new Vector2(900f, 500f);
        private static readonly Vector2 SIZE = new Vector2(PITCH, PITCH);

        [Test]
        public void ClearSpaceKeepsTheGridAboveItsSquadrons()
        {
            var placement = new SquadronClusterPlacement();
            placement.Begin(SCREEN);
            Vector2 center = placement.Place(CENTROID, SIZE, PITCH, RISE, 1f, SquadronClusterPlacement.NO_CANDIDATE,
                out int chosen);
            Assert.That(chosen, Is.Zero);
            Assert.That(center, Is.EqualTo(CENTROID + new Vector2(0f, RISE)));
        }

        [Test]
        public void ObstacleOverTheDefaultMovesTheGridToAFreeSide()
        {
            var placement = new SquadronClusterPlacement();
            placement.Begin(SCREEN);
            var hull = new Rect(CENTROID.x - 40f, CENTROID.y + 20f, 80f, 80f);
            placement.AddObstacle(hull);
            Vector2 center = placement.Place(CENTROID, SIZE, PITCH, RISE, 1f, SquadronClusterPlacement.NO_CANDIDATE,
                out int chosen);
            Assert.That(chosen, Is.Not.Zero);
            Assert.That(new Rect(center - SIZE * 0.5f, SIZE).Overlaps(hull), Is.False);
        }

        [Test]
        public void SurroundedGridFallsBackToALeastOverlapCandidateWithinTheDisplacementLimit()
        {
            var placement = new SquadronClusterPlacement();
            placement.Begin(SCREEN);
            placement.AddObstacle(new Rect(CENTROID.x - 600f, CENTROID.y - 600f, 1200f, 1200f));
            Vector2 center = placement.Place(CENTROID, SIZE, PITCH, RISE, 1f, SquadronClusterPlacement.NO_CANDIDATE,
                out int chosen);
            Assert.That(chosen, Is.InRange(0, SquadronClusterPlacement.CandidateCount - 1));
            Assert.That(Vector2.Distance(center, CENTROID), Is.LessThan(220f + SIZE.x + RISE));
        }

        [Test]
        public void PreviousSideWinsWhenNothingBeatsItByTheSwitchGain()
        {
            var placement = new SquadronClusterPlacement();
            var obstacle = new Rect(CENTROID.x - 40f, CENTROID.y + 40f, 80f, 20f);
            placement.Begin(SCREEN);
            placement.AddObstacle(obstacle);
            Vector2 first = placement.Place(CENTROID, SIZE, PITCH, RISE, 1f, SquadronClusterPlacement.NO_CANDIDATE,
                out int firstChoice);
            placement.Begin(SCREEN);
            placement.AddObstacle(obstacle);
            Vector2 repeat = placement.Place(CENTROID, SIZE, PITCH, RISE, 1f, firstChoice, out int repeatChoice);
            Assert.That(repeatChoice, Is.EqualTo(firstChoice));
            Assert.That(repeat, Is.EqualTo(first));
        }

        [Test]
        public void PlacedGridsBecomeObstaclesForLaterOnes()
        {
            var placement = new SquadronClusterPlacement();
            placement.Begin(SCREEN);
            Vector2 first = placement.Place(CENTROID, SIZE, PITCH, RISE, 1f, SquadronClusterPlacement.NO_CANDIDATE,
                out _);
            var firstRect = new Rect(first - SIZE * 0.5f, SIZE);
            placement.AddObstacle(firstRect);
            Vector2 second = placement.Place(CENTROID, SIZE, PITCH, RISE, 1f, SquadronClusterPlacement.NO_CANDIDATE,
                out _);
            Assert.That(new Rect(second - SIZE * 0.5f, SIZE).Overlaps(firstRect), Is.False);
        }
    }
}
