using System;

namespace EmpireAtWar.Entities.Fps
{
    public interface IFpsUi : IDisposable
    {
        void Initialize();

        void SetModel(FpsModel model);
    }
}
