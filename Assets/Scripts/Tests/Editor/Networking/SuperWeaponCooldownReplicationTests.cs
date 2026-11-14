using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.SuperWeapons.Controller;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Networking;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Networking.States;
using EmpireAtWar.Services.SuperWeapons;
using EmpireAtWar.Services.VContainer;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Networking")]
    public sealed class SuperWeaponCooldownReplicationTests
    {
        [TestCase(SuperWeaponType.IonCannon)]
        [TestCase(SuperWeaponType.HypervelocityGun)]
        [TestCase(SuperWeaponType.PlasmaCannon)]
        public void AcceptedShot_RoundTripsIssuerWeaponAndTarget(SuperWeaponType type)
        {
            var message = new SuperWeaponFiredMessage
            {
                Issuer = TestPlayers.Enemy,
                Type = type,
                TargetId = 9876543210L
            };
            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            writer.Write(message);
            using NetworkReaderPooled reader = NetworkReaderPool.Get(writer.ToArraySegment());
            SuperWeaponFiredMessage decoded = reader.Read<SuperWeaponFiredMessage>();
            Assert.That(decoded.Issuer, Is.EqualTo(message.Issuer));
            Assert.That(decoded.Type, Is.EqualTo(type));
            Assert.That(decoded.TargetId, Is.EqualTo(message.TargetId));
        }

        [TestCase(SuperWeaponType.IonCannon)]
        [TestCase(SuperWeaponType.HypervelocityGun)]
        [TestCase(SuperWeaponType.PlasmaCannon)]
        public void RemoteHuman_CooldownTicksWithoutUiAndSnapshotsAllowASecondShot(SuperWeaponType type)
        {
            var owner = new PlayerSlot(TestPlayers.Enemy, new TeamId(1), FactionType.Separatist,
                PlayerController.Human, EnemyAiDifficulty.Medium, 1);
            var host = new SuperWeaponModel();
            var client = new SuperWeaponModel();
            var builder = new ContainerBuilder();
            builder.RegisterInstance(host);
            // A remote human receives player services, but never ConfigureLocalPlayer.
            new SuperWeaponsModule(null).ConfigurePlayer(builder,
                new BattlePlayer(owner, PlayerRole.Human, BattleFeatures.All), null, runsAi: false);
            using IObjectResolver container = builder.Build();
            Assert.That(container.TryGetRegistration(typeof(SuperWeaponPresenter), out _), Is.False);
            var tickers = container.Resolve<IReadOnlyList<ITickable>>();
            Assert.That(tickers.OfType<SuperWeaponCooldownService>().Count(), Is.EqualTo(1));

            var hostReserve = new ReinforcementModel(null);
            var clientReserve = new ReinforcementModel(null);
            var hostState = CreateState(owner, host, hostReserve);
            var clientState = CreateState(owner, client, clientReserve);
            host.StartCharging(type);
            host.CompleteCharging(type);
            host.Consume(type);
            clientState.Apply(hostState.Capture());
            Assert.That(client.GetCooldownRemaining(type), Is.EqualTo(SuperWeaponModel.COOLDOWN_DURATION));

            float frameTime = Time.deltaTime;
            foreach (ITickable ticker in tickers) ticker.Tick();
            Assert.That(host.GetCooldownRemaining(type),
                Is.EqualTo(Mathf.Max(0f, SuperWeaponModel.COOLDOWN_DURATION - frameTime)).Within(0.001f));

            // Advance the long construction interval deterministically; the registration check above
            // covers the missing runtime ticker that model-only cooldown tests could not catch.
            host.Tick(Mathf.Max(0f, 80f - frameTime));
            host.StartCharging(type);
            host.CompleteCharging(type);
            clientState.Apply(hostState.Capture());
            Assert.That(client.GetCooldownRemaining(type), Is.EqualTo(40f).Within(0.001f));
            Assert.That(client.CanFire(type), Is.False);

            host.Tick(10f);
            hostReserve.RestoreUnitCapacity(3);
            clientState.Apply(hostState.Capture());
            Assert.That(clientReserve.CurrentUnitCapacity, Is.EqualTo(3));
            Assert.That(client.GetCooldownRemaining(type), Is.EqualTo(30f).Within(0.001f),
                "A capacity update must not restore the original 120-second cooldown.");

            host.Tick(host.GetCooldownRemaining(type));
            clientState.Apply(hostState.Capture());
            Assert.That(host.CanFire(type), Is.True);
            Assert.That(client.CanFire(type), Is.True);
            host.Consume(type);
            clientState.Apply(hostState.Capture());
            Assert.That(client.GetState(type), Is.EqualTo(SuperWeaponState.Unavailable));
            Assert.That(client.GetCooldownRemaining(type), Is.EqualTo(SuperWeaponModel.COOLDOWN_DURATION));
        }

        private static PlayerReserveReplicatedState CreateState(PlayerSlot owner, SuperWeaponModel model,
            ReinforcementModel reserve) => new PlayerReserveReplicatedState(new ReplicatedStateRegistry(),
                new OfflineSession(TestPlayers.Human), owner, reserve, null, model, null, null, null);
    }
}
