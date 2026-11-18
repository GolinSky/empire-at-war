using EmpireAtWar.Services.Settings;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class LobbySettingsDataTests
    {
        [Test]
        public void OnlineOccupantChoice_IsKeptSeparatelyFromOffline()
        {
            var data = new SettingsData();
            data.Lobby.SetOccupantChoice(1, 3, online: true);
            data.Lobby.SetOccupantChoice(1, 4);
            SettingsData loaded = SettingsData.FromJson(data.ToJson());

            Assert.That(loaded.Lobby.GetOccupantChoice(1, online: true), Is.EqualTo(3));
            Assert.That(loaded.Lobby.GetOccupantChoice(1), Is.EqualTo(4));
            Assert.That(loaded.Lobby.GetOccupantChoice(2, online: true), Is.EqualTo(-1));
        }

        [Test]
        public void OccupantChoice_IsUnsetUntilChosenAndSurvivesJson()
        {
            var data = new SettingsData();
            Assert.That(data.Lobby.GetOccupantChoice(1), Is.EqualTo(-1));

            data.Lobby.SetOccupantChoice(1, 4);
            data.Lobby.SetOccupantChoice(2, 0);
            SettingsData loaded = SettingsData.FromJson(data.ToJson());

            Assert.That(loaded.Lobby.GetOccupantChoice(1), Is.EqualTo(4));
            Assert.That(loaded.Lobby.GetOccupantChoice(2), Is.EqualTo(0));
            Assert.That(loaded.Matches(data), Is.True);
        }
    }
}
