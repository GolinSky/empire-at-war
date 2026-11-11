using System;

namespace EmpireAtWar.Services.Networking.States
{
    [Serializable]
    public sealed class StationLevelSnapshot
    {
        public int Level;
        public int SecondStationLevel = 1;
    }
}
