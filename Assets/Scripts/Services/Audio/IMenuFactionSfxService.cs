using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Audio
{
    public interface IMenuFactionSfxService
    {
        void PlaySelection(FactionType faction);
    }
}
