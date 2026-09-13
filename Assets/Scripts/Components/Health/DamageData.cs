namespace EmpireAtWar.Models.Health
{
    public struct DamageData
    {
        public float ShieldDamage { get; }
        public float ArmorDamage { get; }

        public DamageData(float shieldDamage, float armorDamage)
        {
            ShieldDamage = shieldDamage;
            ArmorDamage = armorDamage;
        }
    }
}
