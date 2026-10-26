using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.Audio
{
    public interface IMusicService : IService
    {
        void SetVoiceDucking(bool active);
    }
}
