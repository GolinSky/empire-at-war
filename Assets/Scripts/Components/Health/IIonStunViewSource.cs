using System;
using EmpireAtWar.ViewComponents.Health;

namespace EmpireAtWar.Components.Ship.Health
{
    public interface IIonStunViewSource
    {
        event Action<IonStunView> IonStunViewSpawned;
    }
}
