using System;

namespace EmpireAtWar.Components.Ship.Health
{
    public interface IIonStunViewSource
    {
        event Action<IIonStunRenderers> IonStunViewSpawned;
    }
}
