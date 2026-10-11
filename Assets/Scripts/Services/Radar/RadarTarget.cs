namespace EmpireAtWar.Services.Radar
{
    internal readonly struct RadarTarget
    {
        public readonly int OwnerSlot;
        public readonly bool IsDetectable;

        public RadarTarget(int ownerSlot, bool isDetectable)
        {
            OwnerSlot = ownerSlot;
            IsDetectable = isDetectable;
        }
    }
}
