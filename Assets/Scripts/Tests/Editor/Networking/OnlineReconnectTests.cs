using System.Collections.Generic;
using EmpireAtWar.Services.Networking.Online;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Networking")]
    public sealed class OnlineReconnectTests
    {
        [Test]
        public void Credential_ReclaimsDisconnectedSlotDespiteChangedName()
        {
            OnlineLobby lobby = CreateLobby();
            string credential = lobby.GetReconnectCredential(1);
            lobby.MarkDisconnected(1);

            Assert.That(lobby.TryRebind(credential, 42UL, "Renamed", 3, out int slot, out int previous), Is.True);
            Assert.That(slot, Is.EqualTo(1));
            Assert.That(previous, Is.EqualTo(1));
            Assert.That(lobby.TryGetMember(1, out _), Is.False);
            Assert.That(lobby.TryGetMember(3, out LobbyMember member), Is.True);
            Assert.That(member.Name, Is.EqualTo("Renamed"));
            Assert.That(member.Connected, Is.True);
            Assert.That(lobby.GetReconnectCredential(3), Is.EqualTo(credential));
        }

        [Test]
        public void Credential_ReplacesConnectedSlotBeforeTransportDetectsLoss()
        {
            OnlineLobby lobby = CreateLobby();

            Assert.That(lobby.TryRebind(lobby.GetReconnectCredential(1), 42UL, "Client", 3,
                out int slot, out int previous), Is.True);
            lobby.MarkDisconnected(previous);

            Assert.That(slot, Is.EqualTo(1));
            Assert.That(lobby.TryGetMember(3, out LobbyMember member), Is.True);
            Assert.That(member.Connected, Is.True, "A stale disconnect callback cannot disconnect the replacement.");
        }

        [TestCase(null, 42UL)]
        [TestCase("", 42UL)]
        [TestCase("wrong", 42UL)]
        public void InvalidCredential_DoesNotRebindDuplicateName(string credential, ulong identity)
        {
            OnlineLobby lobby = CreateLobby();
            Assert.That(lobby.TryRebind(credential, identity, "Client", 3, out _, out _), Is.False);
            Assert.That(lobby.TryGetMember(1, out LobbyMember member), Is.True);
            Assert.That(member.Connected, Is.True);
        }

        [TestCase(0UL)]
        [TestCase(99UL)]
        public void Credential_WithWrongVerifiedIdentityDoesNotReplaceConnection(ulong identity)
        {
            OnlineLobby lobby = CreateLobby();
            Assert.That(lobby.TryRebind(lobby.GetReconnectCredential(1), identity, "Client", 3,
                out _, out _), Is.False);
            Assert.That(lobby.TryGetMember(1, out _), Is.True);
        }

        [Test]
        public void DuplicateNames_HaveSeparateCredentialsAndSlots()
        {
            OnlineLobby lobby = CreateLobby();
            lobby.MarkDisconnected(1);
            lobby.MarkDisconnected(2);

            Assert.That(lobby.TryRebind(lobby.GetReconnectCredential(2), 43UL, "Client", 3,
                out int slot, out _), Is.True);
            Assert.That(slot, Is.EqualTo(2));
            Assert.That(lobby.TryGetMember(1, out LobbyMember first), Is.True);
            Assert.That(first.Connected, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RemovedOrAbandonedMember_CannotReclaim(bool abandoned)
        {
            OnlineLobby lobby = CreateLobby();
            string credential = lobby.GetReconnectCredential(1);
            if (abandoned) lobby.RemoveAbandoned(1);
            else lobby.Remove(1);

            Assert.That(lobby.TryRebind(credential, 42UL, "Client", 3, out _, out _), Is.False);
            Assert.That(lobby.TryGetMember(1, out _), Is.False);
        }

        [Test]
        public void Reset_RevokesPreviousSessionCredential()
        {
            OnlineLobby lobby = CreateLobby();
            string credential = lobby.GetReconnectCredential(1);
            lobby.Reset("Host", 0UL, 8);
            lobby.Add(1, "Client", 42UL);
            lobby.AssignSlots(new Dictionary<int, int> { [0] = 0, [1] = 1 });
            Assert.That(lobby.TryRebind(credential, 42UL, "Client", 3, out _, out _), Is.False);
        }

        [Test]
        public void Reservation_ExpiresAtFiveMinutesAndRefusesRejoin()
        {
            var reservations = new ReconnectReservations();
            reservations.MarkDisconnected(1, 10f);
            Assert.That(reservations.Expired(309f), Is.Empty);
            Assert.That(reservations.Expired(310f), Is.EqualTo(new[] { 1 }));
            Assert.That(reservations.TryAccept(1, 310f), Is.False);
        }

        [Test]
        public void NearExpiryAcceptance_GetsOneLoadingAllowanceDespiteRepeatedCrashes()
        {
            var reservations = new ReconnectReservations();
            reservations.MarkDisconnected(1, 0f);
            Assert.That(reservations.TryAccept(1, 299f), Is.True);
            Assert.That(reservations.Remaining(1, 299f), Is.EqualTo(180f));
            reservations.MarkDisconnected(1, 350f);
            Assert.That(reservations.TryAccept(1, 400f), Is.True);
            Assert.That(reservations.Remaining(1, 400f), Is.EqualTo(79f));
            Assert.That(reservations.Expired(479f), Is.EqualTo(new[] { 1 }));
            Assert.That(reservations.TryAccept(1, 479f), Is.False);
        }

        [Test]
        public void EarlyAcceptance_DoesNotShortenOriginalGrace()
        {
            var reservations = new ReconnectReservations();
            reservations.MarkDisconnected(1, 0f);
            Assert.That(reservations.TryAccept(1, 10f), Is.True);
            Assert.That(reservations.Remaining(1, 10f), Is.EqualTo(290f));
        }

        [Test]
        public void SupersedingLiveConnection_StartsBoundedDeadline()
        {
            var reservations = new ReconnectReservations();
            Assert.That(reservations.TryAccept(1, 20f), Is.True);
            Assert.That(reservations.Expired(320f), Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void CompletedSynchronization_AllowsFreshGraceOnNextRealDisconnect()
        {
            var reservations = new ReconnectReservations();
            reservations.MarkDisconnected(1, 0f);
            reservations.TryAccept(1, 299f);
            reservations.Complete(1);
            Assert.That(reservations.Expired(1000f), Is.Empty);
            reservations.MarkDisconnected(1, 500f);
            Assert.That(reservations.Remaining(1, 500f), Is.EqualTo(300f));
            Assert.That(reservations.TryAccept(1, 799f), Is.True);
            Assert.That(reservations.Remaining(1, 799f), Is.EqualTo(180f));
        }

        private static OnlineLobby CreateLobby()
        {
            var lobby = new OnlineLobby();
            lobby.Reset("Host", 0UL, 8);
            lobby.Add(1, "Client", 42UL);
            lobby.Add(2, "Client", 43UL);
            lobby.AssignSlots(new Dictionary<int, int> { [0] = 0, [1] = 1, [2] = 2 });
            return lobby;
        }
    }
}
