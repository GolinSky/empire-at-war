using System.Collections.Generic;
using EmpireAtWar.Services.Enemy;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Round 6 (G3): forward anchors rank by distance to the task and skip unsafe or unescorted units.</summary>
    [Category("EnemyAi")]
    public sealed class EnemyForwardSpawnAnchorsTests
    {
        private static readonly Vector3 TASK = new Vector3(0f, 0f, 2000f);
        private readonly EnemyForwardSpawnAnchors.AnchorPicture _picture = new EnemyForwardSpawnAnchors.AnchorPicture();
        private readonly List<ForwardSpawnAnchor> _anchors = new List<ForwardSpawnAnchor>();

        [SetUp]
        public void SetUp()
        {
            _picture.Clear();
            // Own home behind the units; the hostile front far beyond the task.
            _picture.OwnFronts.Add(new Vector3(0f, 0f, -2000f));
            _picture.HostileFronts.Add(new Vector3(0f, 0f, 9000f));
        }

        [Test]
        public void NearestTheTaskComesFirst_KindOnlyBreaksTies()
        {
            Vector3 farWing = new Vector3(0f, 0f, -1000f);
            Vector3 nearShip = new Vector3(0f, 0f, 1500f);
            _picture.Candidates.Add((farWing, EnemyForwardSpawnAnchors.WING));
            _picture.Candidates.Add((nearShip, EnemyForwardSpawnAnchors.SHIP));

            Assert.That(Select(capital: false), Is.True);
            Assert.That(_anchors[0].Position, Is.EqualTo(nearShip));
        }

        [Test]
        public void BeaconLeadsOnlyNearTheBestOtherAnchor()
        {
            Vector3 nearShip = new Vector3(0f, 0f, 1800f);
            _picture.Candidates.Add((nearShip, EnemyForwardSpawnAnchors.SHIP));
            Vector3 farBeacon = new Vector3(3000f, 0f, -500f);
            _picture.Candidates.Add((farBeacon, EnemyForwardSpawnAnchors.BEACON));

            Select(capital: false);
            Assert.That(_anchors[0].Position, Is.EqualTo(nearShip), "A beacon 3,000 away does not lead.");

            _picture.Candidates.RemoveAt(1);
            Vector3 nearBeacon = new Vector3(600f, 0f, 1200f);
            _picture.Candidates.Add((nearBeacon, EnemyForwardSpawnAnchors.BEACON));
            Select(capital: false);
            Assert.That(_anchors[0].Position, Is.EqualTo(nearBeacon));
        }

        [Test]
        public void ShipsLandedThisTickRankBeforeANewSearch()
        {
            _picture.Candidates.Add((new Vector3(0f, 0f, 1900f), EnemyForwardSpawnAnchors.WING));
            Vector3 landed = new Vector3(0f, 0f, 0f);
            _picture.Candidates.Add((landed, EnemyForwardSpawnAnchors.LANDED));
            _picture.OwnShips.Add(landed);

            Select(capital: false);
            Assert.That(_anchors[0].Position, Is.EqualTo(landed));
        }

        [Test]
        public void KnownHostileStructureNearTheUnitRejectsIt()
        {
            Vector3 wing = new Vector3(0f, 0f, 1500f);
            _picture.Candidates.Add((wing, EnemyForwardSpawnAnchors.WING));
            _picture.Hostiles.Add(wing + new Vector3(500f, 0f, 0f));

            Assert.That(Select(capital: false), Is.False);
        }

        [Test]
        public void DeepUnitNeedsThreeOwnShipsNearIt()
        {
            _picture.HostileFronts.Clear();
            _picture.HostileFronts.Add(new Vector3(0f, 0f, 3000f));
            Vector3 deepWing = new Vector3(0f, 0f, 2000f);
            _picture.Candidates.Add((deepWing, EnemyForwardSpawnAnchors.WING));

            Assert.That(Select(capital: false), Is.False);
            for (int i = 0; i < EnemyForwardSpawnAnchors.DEEP_ESCORT_SHIPS; i++)
                _picture.OwnShips.Add(deepWing + new Vector3(100f * i, 0f, 0f));
            Assert.That(Select(capital: false), Is.True);
        }

        [Test]
        public void CapitalNeedsFourOwnShipsNearItsAnchor()
        {
            Vector3 wing = new Vector3(0f, 0f, 1500f);
            _picture.Candidates.Add((wing, EnemyForwardSpawnAnchors.WING));
            for (int i = 0; i < EnemyForwardSpawnAnchors.CAPITAL_ESCORT_SHIPS - 1; i++)
                _picture.OwnShips.Add(wing + new Vector3(100f * i, 0f, 0f));

            Assert.That(Select(capital: false), Is.True);
            Assert.That(Select(capital: true), Is.False);
            _picture.OwnShips.Add(wing + new Vector3(0f, 0f, 100f));
            Assert.That(Select(capital: true), Is.True);
        }

        private bool Select(bool capital) =>
            EnemyForwardSpawnAnchors.Select(_picture, TASK, capital, _anchors);
    }
}
