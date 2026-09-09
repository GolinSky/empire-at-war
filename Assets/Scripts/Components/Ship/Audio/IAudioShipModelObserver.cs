using System;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Components.Ship.Audio
{
    public interface IAudioShipModelObserver : IModelObserver
    {
        event Action<AudioShipModel.OneShot> OnOneShotRequested;
    }
}
