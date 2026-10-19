using System.Linq;
using EmpireAtWar.Services.ReinforcementZones;

namespace EmpireAtWar.Services.Saves
{
    public sealed class ReinforcementZonesSaveSource : IBattleSaveSource
    {
        private readonly ReinforcementZonesSystem _zones;

        public ReinforcementZonesSaveSource(ReinforcementZonesSystem zones)
        {
            _zones = zones;
        }

        public void Capture(BattleSave save) => save.ReinforcementZones = _zones.Zones.Select(zone =>
            ReinforcementZoneSave.Create(zone.Owner, zone.CapturingPlayer, zone.CaptureProgress,
                zone.IsContested)).ToArray();

        public void RestoreBeforeUnits(BattleSave save) => _zones.RestoreZones(save.ReinforcementZones);

        public void RestoreAfterUnits(BattleRestoreContext context)
        {
        }
    }
}
