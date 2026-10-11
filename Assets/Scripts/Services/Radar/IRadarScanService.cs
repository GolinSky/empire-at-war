using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.Radar
{
    public interface IRadarScanService : IService
    {
        void Register(IRadarScanner scanner);

        void Unregister(IRadarScanner scanner);
    }
}
