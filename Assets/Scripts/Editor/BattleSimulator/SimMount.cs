using EmpireAtWar.Components.Weapon;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public sealed class SimMount
    {
        public SimMountDefinition Definition;
        public HitAccumulator Hits;
        public double SalvoStart;
        public int Shot;
        public double NextShot => SalvoStart + Shot * Definition.Profile.ShotInterval;

        public void Advance()
        {
            Shot++;
            if (Shot < Definition.Profile.ShotsPerSalvo) return;
            Shot = 0;
            SalvoStart += Definition.Profile.CycleSeconds;
        }
    }
}
