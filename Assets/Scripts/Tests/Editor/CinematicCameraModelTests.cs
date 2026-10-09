using System;
using EmpireAtWar.Tests.Editor;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.CinematicCamera.Model;
using EmpireAtWar.Services.Camera;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Random = System.Random;
using NumericsVector3 = System.Numerics.Vector3;

namespace EmpireAtWar.Tests.CinematicCamera
{
    public sealed class CinematicCameraModelTests
    {
        private CinematicCameraData _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<CinematicCameraData>();
            EditorUtility.CopySerialized(
                AssetDatabase.LoadAssetAtPath<CinematicCameraData>(
                    "Assets/Settings/Data/Models/Camera/CinematicCameraData.asset"), _settings);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_settings);
        }

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
            CinematicInterestScorer scorer = new CinematicInterestScorer(settings: _settings, random: new Random(1), relations: TestPlayers.CreateDuel());
            CinematicCandidate[] candidates =
            {
                new(id: 1, position: new NumericsVector3(0f, 0f, 0f), shipClass: ShipClass.Frigate, owner: TestPlayers.Human, secondsSinceDamaged: 0.5f),
                new(id: 2, position: new NumericsVector3(50f, 0f, 0f), shipClass: ShipClass.Frigate, owner: TestPlayers.Enemy, secondsSinceDamaged: float.PositiveInfinity),
                new(id: 3, position: new NumericsVector3(5000f, 0f, 0f), shipClass: ShipClass.HeavyCapital, owner: TestPlayers.Human, secondsSinceDamaged: float.PositiveInfinity),
            };

            bool found = scorer.TrySelect(candidates, -1, out CinematicSelection selection);

            Assert.That(found, Is.True);
            Assert.That(selection.Target.Id, Is.EqualTo(1));
            Assert.That(selection.FocusOffset, Is.EqualTo(new NumericsVector3(25f, 0f, 0f)));
        }

        [Test]
        public void Scorer_ReturnsFalseWithoutCandidates()
        {
            CinematicInterestScorer scorer = new CinematicInterestScorer(settings: _settings, random: new Random(1), relations: TestPlayers.CreateDuel());

            Assert.That(scorer.TrySelect(Array.Empty<CinematicCandidate>(), -1, out _), Is.False);
        }

        [Test]
        public void FramingDistance_KeepsClassMinimumForSmallUnits()
        {
            Assert.That(CinematicShotSolver.CalculateFramingDistance(170f, 8f, 30f), Is.EqualTo(170f));
        }

        [TestCase(90f)]
        [TestCase(1060f)]
        public void ShotTransitions_StayOutsideHullAndKeepItFramed(float radius)
        {
            const float FIELD_OF_VIEW = 30f;
            Vector3 anchor = new Vector3(150f, 80f, -200f);
            float distance = CinematicShotSolver.CalculateFramingDistance(20f, radius, FIELD_OF_VIEW);
            foreach (CinematicShotType fromType in Enum.GetValues(typeof(CinematicShotType)))
            foreach (CinematicShotType toType in Enum.GetValues(typeof(CinematicShotType)))
            foreach (float fromSide in new[] { -1f, 1f })
            foreach (float toSide in new[] { -1f, 1f })
            {
                Pose from = CinematicShotSolver.Solve(
                    new CinematicShot(fromType, fromSide, 3f), anchor, Quaternion.identity,
                    Vector3.zero, distance, 2.5f, FIELD_OF_VIEW, 1f);
                Pose to = CinematicShotSolver.Solve(
                    new CinematicShot(toType, toSide, 3f), anchor, Quaternion.identity,
                    Vector3.zero, distance, 2.5f, FIELD_OF_VIEW, 0f);
                for (int i = 1; i <= 20; i++)
                {
                    Pose pose = CinematicShotSolver.InterpolatePose(from.position, to, anchor, i / 20f);
                    Vector3 toHull = anchor - pose.position;
                    Assert.That(toHull.magnitude, Is.GreaterThan(radius),
                        $"{fromType}/{fromSide} -> {toType}/{toSide}, step {i}");
                    float hullAngle = Mathf.Asin(radius / toHull.magnitude) * Mathf.Rad2Deg;
                    float centerAngle = Vector3.Angle(pose.rotation * Vector3.forward, toHull);
                    Assert.That(centerAngle + hullAngle, Is.LessThan(FIELD_OF_VIEW * 0.5f),
                        $"{fromType}/{fromSide} -> {toType}/{toSide}, step {i}");
                }
            }
        }

        [TestCase(125f, 0f, 0f)]
        [TestCase(-125f, 0f, 0f)]
        [TestCase(0f, 125f, 0f)]
        [TestCase(0f, 0f, 300f)]
        public void WideShot_WithOffsetFocusKeepsSubjectFramed(float x, float y, float z)
        {
            const float RADIUS = 10f;
            const float FIELD_OF_VIEW = 30f;
            Vector3 focus = new Vector3(x, y, z);
            float distance = CinematicShotSolver.CalculateFramingDistance(20f, RADIUS, FIELD_OF_VIEW);
            foreach (float side in new[] { -1f, 1f })
            {
                Pose desired = CinematicShotSolver.Solve(
                    new CinematicShot(CinematicShotType.Wide, side, 3f), Vector3.zero, Quaternion.identity,
                    focus, distance, 2.5f, FIELD_OF_VIEW, 0f);
                Vector3 current = new Vector3(-distance, 0f, 0f);
                for (int i = 1; i <= 20; i++)
                {
                    Pose pose = CinematicShotSolver.InterpolatePose(current, desired, focus, i / 20f);
                    Vector3 toHull = -pose.position;
                    Assert.That(toHull.magnitude, Is.GreaterThan(RADIUS));
                    float hullAngle = Mathf.Asin(RADIUS / toHull.magnitude) * Mathf.Rad2Deg;
                    float centerAngle = Vector3.Angle(pose.rotation * Vector3.forward, toHull);
                    Assert.That(centerAngle + hullAngle, Is.LessThan(FIELD_OF_VIEW * 0.5f));
                }
            }
        }

        [Test]
        public void Interpolation_RecoversWhenMovingSubjectReachesCameraPosition()
        {
            Vector3 focus = new Vector3(100f, 80f, 50f);
            Pose desired = new Pose(focus + Vector3.back * 100f, Quaternion.identity);
            Pose pose = CinematicShotSolver.InterpolatePose(focus, desired, focus, 0.1f);
            Assert.That(Vector3.Distance(pose.position, focus), Is.EqualTo(100f).Within(0.001f));
            Assert.That(Vector3.Angle(pose.rotation * Vector3.forward, focus - pose.position),
                Is.LessThan(0.01f));
        }

        [TestCase(0f, 80f, 0f)]
        [TestCase(499f, 80f, 299f)]
        [TestCase(-499f, 80f, -299f)]
        [TestCase(0f, -520f, 0f)]
        public void ConstrainedShotsAndTransitions_StayInsideMapAndZoomLimits(float x, float y, float z)
        {
            Vector3 anchor = new Vector3(x, y, z);
            Vector3 min = new Vector3(-500f, 60f, -300f);
            Vector3 max = new Vector3(500f, 940f, 300f);
            float distance = CinematicShotSolver.CalculateFramingDistance(170f, 1060f, 30f);
            Vector3 current = new Vector3(-4000f, 5000f, 3000f);
            foreach (CinematicShotType type in Enum.GetValues(typeof(CinematicShotType)))
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 focusOffset = new Vector3(100f, 0f, -100f);
                Vector3 lookPoint = type == CinematicShotType.Wide ? anchor + focusOffset : anchor;
                Pose desired = CinematicShotSolver.Solve(
                    new CinematicShot(type, side, 3f), anchor, Quaternion.identity,
                    focusOffset, distance, 2.5f, 30f, 0f);
                desired = CinematicShotSolver.ConstrainPose(desired, lookPoint, min, max, max.y);
                for (int i = 0; i <= 20; i++)
                {
                    Pose pose = CinematicShotSolver.InterpolatePose(current, desired, lookPoint, i / 20f);
                    pose = CinematicShotSolver.ConstrainPose(pose, lookPoint, min, max, max.y);
                    Assert.That(pose.position.x, Is.InRange(min.x, max.x));
                    Assert.That(pose.position.y, Is.InRange(min.y, max.y));
                    Assert.That(pose.position.z, Is.InRange(min.z, max.z));
                    Assert.That(Vector3.Distance(pose.position, lookPoint), Is.LessThanOrEqualTo(max.y + 0.001f));
                    Assert.That(Vector3.Angle(pose.rotation * Vector3.forward, lookPoint - pose.position),
                        Is.LessThan(0.05f));
                }
            }
        }

        [TestCase(500f, 300f)]
        [TestCase(3000f, 3000f)]
        public void ConstrainedOutwardCornerShot_DoesNotCollapseOntoSubject(float halfWidth, float halfDepth)
        {
            Vector3 min = new Vector3(-halfWidth, 60f, -halfDepth);
            Vector3 max = new Vector3(halfWidth, 940f, halfDepth);
            Pose pose = CinematicShotSolver.ConstrainPose(
                new Pose(max + Vector3.one * 1000f, Quaternion.identity), max, min, max, 940f);
            Assert.That(Vector3.Distance(pose.position, max), Is.GreaterThan(0f));
            Assert.That(Vector3.Distance(pose.position, max), Is.LessThanOrEqualTo(940.001f));
            Assert.That(Vector3.Angle(pose.rotation * Vector3.forward, max - pose.position),
                Is.LessThan(0.05f));
        }

        [TestCase(10f, 30f)]
        [TestCase(90f, 30f)]
        [TestCase(290f, 30f)]
        [TestCase(1060f, 30f)]
        [TestCase(90f, 60f)]
        public void Shots_KeepResizedHullInsideFieldOfView(float radius, float fieldOfView)
        {
            float distance = CinematicShotSolver.CalculateFramingDistance(20f, radius, fieldOfView);
            Vector3 anchor = new Vector3(150f, 80f, -200f);
            Quaternion rotation = Quaternion.Euler(0f, 73f, 0f);
            foreach (CinematicShotType type in Enum.GetValues(typeof(CinematicShotType)))
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    foreach (float progress in new[] { 0f, 0.5f, 1f })
                    {
                        Pose pose = CinematicShotSolver.Solve(
                            new CinematicShot(type, side, 3f), anchor, rotation,
                            Vector3.zero, distance, 2.5f, fieldOfView, progress);
                        Vector3 toHull = anchor - pose.position;
                        float centerAngle = Vector3.Angle(pose.rotation * Vector3.forward, toHull);
                        float hullAngle = Mathf.Asin(radius / toHull.magnitude) * Mathf.Rad2Deg;
                        Assert.That(centerAngle + hullAngle, Is.LessThan(fieldOfView * 0.5f),
                            $"{type}, side {side}, progress {progress}");
                    }
                }
            }
        }
    }
}
