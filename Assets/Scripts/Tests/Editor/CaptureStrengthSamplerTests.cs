using System;
using EmpireAtWar.Entities.Capture;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Ships;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Utils;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Capture")]
    public sealed class CaptureStrengthSamplerTests
    {
        [Test]
        public void GeometrySnapshot_PreparesOncePerDueScanAndTracksMovingArea()
        {
            var sampler = new CaptureStrengthSampler(new ShipService(), new Squadrons(), TestPlayers.CreateDuel(), 0.5f);
            Vector3 currentCenter = Vector3.zero;
            Vector3 sampledCenter = default;
            float currentRadius = 1f;
            float sampledRadius = 0f;
            int preparations = 0;
            int predicates = 0;
            sampler.Add(position =>
            {
                predicates++;
                return PlanarGeometry.ContainsCircle(position, sampledCenter, sampledRadius);
            }, () =>
            {
                preparations++;
                sampledCenter = currentCenter;
                sampledRadius = currentRadius;
            });

            sampler.Tick(0f);
            Assert.That(preparations, Is.EqualTo(1));
            Assert.That(predicates, Is.EqualTo(2), "One preparation is shared by every unit in the scan.");
            Assert.That(sampler[0].Advantage, Is.EqualTo(1f));

            currentCenter = Vector3.right * 10f;
            sampler.Tick(0.1f);
            Assert.That(preparations, Is.EqualTo(1), "A cached strength read must not refresh geometry.");
            Assert.That(sampler[0].HasUnits, Is.True);
            sampler.Tick(0.15f);
            Assert.That(preparations, Is.EqualTo(2));
            Assert.That(sampler[0].HasUnits, Is.False, "The next due scan uses the area's current geometry.");

            currentCenter = Vector3.zero;
            sampler.Invalidate();
            sampler.Tick(0f);
            Assert.That(preparations, Is.EqualTo(3));
            Assert.That(sampler[0].Advantage, Is.EqualTo(1f));

            currentRadius = 0.1f;
            sampler.Tick(0.25f);
            Assert.That(preparations, Is.EqualTo(4));
            Assert.That(sampler[0].Advantage, Is.EqualTo(0.5f), "Scaled geometry is refreshed too.");
        }

        [Test]
        public void CallbackOnlyArea_RemainsSupported()
        {
            var sampler = new CaptureStrengthSampler(new ShipService(), new Squadrons(), TestPlayers.CreateDuel(), 0.5f);
            sampler.Add(position => true);
            sampler.Tick(0f);
            Assert.That(sampler[0].Advantage, Is.EqualTo(1f));
        }

        private sealed class Squadrons : ISquadronRegistry
        {
            public string Id => nameof(Squadrons);

            public void AddSquadronStrength(Func<Vector3, bool> contains, float weight, CaptureStrengthBuilder tally)
            {
                if (contains(Vector3.zero)) tally.Add(TestPlayers.Human, weight);
                if (contains(Vector3.forward * 0.5f)) tally.Add(TestPlayers.Human, weight);
            }

            public bool HasSquadronInside(Func<Vector3, bool> contains, Predicate<PlayerId> isOwnerIncluded) =>
                throw new NotSupportedException();
        }
    }
}
