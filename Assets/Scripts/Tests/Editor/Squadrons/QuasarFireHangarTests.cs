using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Squadrons")]
    public sealed class QuasarFireHangarTests
    {
        [Test]
        public void QuasarFire_IsAnEmpireCarrierWithThreeTieBays()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<FactionCatalog>(AssetDatabase.GUIDToAssetPath(
                AssetDatabase.FindAssets("t:FactionCatalog").Single()));
            Assert.That(catalog.Factions.Where(faction => faction.Ships.ContainsKey(ShipType.QuasarFire))
                .Select(faction => faction.FactionType), Is.EqualTo(new[] { FactionType.Empire }));

            var ship = AssetDatabase.LoadAssetAtPath<ShipData>(AssetDatabase.GUIDToAssetPath(
                AssetDatabase.FindAssets("QuasarFireShipData t:ShipData").Single()));
            Assert.That(ship.HangarBays.Select(bay => (bay.SquadronType, bay.Reserve, bay.MaxActive)),
                Is.EqualTo(new[]
                {
                    (SquadronType.TIEFighter, 6, 2),
                    (SquadronType.TIEBomber, 3, 1),
                    (SquadronType.TIEInterceptor, 3, 1)
                }));
        }

        [Test]
        public void Replacement_RequiresALostSquadronAndConsumesOneReserve()
        {
            var model = new HangarModel(new CarrierData());
            Assert.That(model.CanLaunchReplacement, Is.False);
            model.TryLaunch(4f, out _);
            model.TryLaunch(8f, out _);
            Assert.That(model.CanLaunchReplacement, Is.False);

            model.SquadronLost(1);
            Assert.That(model.TryLaunchReplacement(out int bay), Is.True);
            Assert.That(bay, Is.EqualTo(1));
            Assert.That(model.GetActive(1), Is.EqualTo(1));
            Assert.That(model.GetReserve(1), Is.Zero);
            Assert.That(model.GetReserve(0), Is.EqualTo(1));
            Assert.That(model.TryLaunchReplacement(out _), Is.False);
        }

        [Test]
        public void Replacement_SkipsDestroyedBayAndStopsAfterShutdown()
        {
            var model = new HangarModel(new CarrierData());
            model.TryLaunch(4f, out _);
            model.TryLaunch(8f, out _);
            model.SquadronLost(0);
            model.SquadronLost(1);
            model.DisableBay(0);

            Assert.That(model.TryLaunchReplacement(out int bay), Is.True);
            Assert.That(bay, Is.EqualTo(1));
            Assert.That(model.GetReserve(0), Is.EqualTo(1));
            model.Shutdown();
            Assert.That(model.CanLaunchReplacement, Is.False);
            Assert.That(model.TryLaunchReplacement(out _), Is.False);
        }

        [Test]
        public void Replacement_DoesNotCreateSquadronsAfterReserveIsExhausted()
        {
            var model = new HangarModel(new CarrierData());
            model.TryLaunch(4f, out _);
            model.SquadronLost(0);
            model.TryLaunchReplacement(out _);
            model.SquadronLost(0);

            Assert.That(model.TryLaunchReplacement(out _), Is.False);
            Assert.That(model.GetReserve(0), Is.Zero);
            Assert.That(model.GetReserve(1), Is.EqualTo(2));
        }

        private sealed class CarrierData : IHangarData
        {
            public IReadOnlyList<HangarBay> HangarBays { get; } = new[]
            {
                new HangarBay(SquadronType.XWing, 2, 1),
                new HangarBay(SquadronType.YWing, 2, 1)
            };
            public int HangarMaxActive => -1;
            public float HangarInitialDelay => 4f;
            public float HangarLaunchInterval => 8f;
        }
    }
}
