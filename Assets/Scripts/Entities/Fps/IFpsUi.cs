using System;

namespace EmpireAtWar.Entities.Fps
{
    public interface IFpsUi : IDisposable
    {
        void SetModel(FpsModel model);
        void Initialize();
    }
}
