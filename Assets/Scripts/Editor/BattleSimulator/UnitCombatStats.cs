namespace EmpireAtWar.Editor.BattleSimulator
{
    public sealed class UnitCombatStats
    {
        public string Name;
        public string UnitId;
        public int Side;
        public int Index;
        public double DestroyedAt = -1;
        public double DamageDealt;
        public double ShieldDamage;
        public double HullDamage;
        public double HardPointDamage;
        public double Overkill;
        public int Shots;
        public int Hits;
        public float Hull;
        public float Shields;
        public float MaxHull;
        public float MaxShields;
        public double BaseDps;
    }
}
