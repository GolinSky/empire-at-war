namespace EmpireAtWar.Services.ShipAbilities
{
    public static class AutoPowerRules
    {
        public static AutoPowerDecision Decide(float hull, float shields, float shieldFraction,
            bool canRegenerate, bool heavyLoss, bool meaningfulForce, bool canAttack,
            float hostileAdvantage, float lowShields, float lastStandHull, float lastStandRatio)
        {
            if (meaningfulForce && canAttack && hull < lastStandHull && shields <= 0f && hostileAdvantage >= lastStandRatio)
                return AutoPowerDecision.Weapons;
            if (canRegenerate && heavyLoss && shieldFraction < lowShields)
                return AutoPowerDecision.Shields;
            return meaningfulForce && canAttack && !heavyLoss ? AutoPowerDecision.Weapons : AutoPowerDecision.None;
        }
    }
}
