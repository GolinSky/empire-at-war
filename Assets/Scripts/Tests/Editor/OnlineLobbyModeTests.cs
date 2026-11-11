using EmpireAtWar.Services.Networking.Online;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Networking")]
    public sealed class OnlineLobbyModeTests
    {
        [Test]
        public void ChangeCapacity_AppliesLimitAndClearsClientReadyOnly()
        {
            var lobby = new OnlineLobby();
            lobby.Reset("Host", 0UL, OnlineLobby.MAX_MEMBERS);
            lobby.Add(1, "Client", 0UL);
            lobby.Add(2, "Other", 0UL);
            lobby.SetReady(1, true);
            lobby.SetReady(2, true);

            lobby.ChangeCapacity(OnlineLineup.MAX_SURVIVAL_HUMANS);

            Assert.That(lobby.Capacity, Is.EqualTo(OnlineLineup.MAX_SURVIVAL_HUMANS));
            Assert.That(lobby.IsFull, Is.True);
            Assert.That(lobby.Members[0].Ready, Is.True, "The host is always ready.");
            Assert.That(lobby.Members[1].Ready, Is.False);
            Assert.That(lobby.Members[2].Ready, Is.False);
            Assert.That(lobby.AllReady, Is.False);
        }

        [TestCase(LobbyMode.Skirmish)]
        [TestCase(LobbyMode.Survival)]
        public void SwitchLobbyMode_IsRefusedWhenNotHosting(LobbyMode mode)
        {
            var online = new OnlineMatchService(null);

            Assert.That(online.CanSwitchLobbyMode(mode), Is.False);
            Assert.Throws<System.InvalidOperationException>(() => online.SwitchLobbyMode(mode));
        }
    }
}
