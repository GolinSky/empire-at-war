using EmpireAtWar.Services.Settings;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ui")]
    public sealed class LobbySettingsDataTests
    {
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
