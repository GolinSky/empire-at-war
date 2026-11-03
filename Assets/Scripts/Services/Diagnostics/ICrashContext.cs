using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Services.Diagnostics
{
    public interface ICrashContext
    {
        void BeginBattle(GameMode mode, MapType map, PlanetType planet, FactionType faction);
        void UpdateBattle(BattleState state, float seconds, int unitCount);
        void EndBattle();
    }
}
