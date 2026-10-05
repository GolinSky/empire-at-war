using System;

namespace EmpireAtWar.Components.Squadrons.Flight
{
    public sealed class SFoilsModel
    {
        public event Action Changed;
        public bool IsClosed { get; private set; }

        public void SetClosed(bool isClosed)
        {
            IsClosed = isClosed;
            Changed?.Invoke();
        }
    }
}
