using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Views.SpawnArea
{
    /// <summary>Shows where a team may deploy reinforcements while a placement is running.</summary>
    public interface ISpawnAreaOverlay
    {
        void Show(PlayerId team);

        void Hide();
    }
}
