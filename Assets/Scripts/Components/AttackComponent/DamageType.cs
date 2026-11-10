namespace EmpireAtWar.Components.AttackComponent
{
    public enum DamageType
    {
        Laser = 0,
        PointDefense = 1,
        Turbolaser = 2,
        Ion = 4,
        ConcussionMissile = 5,
        ProtonTorpedo = 6,
        Beam = 7,
        IonCannon = 8,
        HypervelocityGun = 9,
        PlasmaCannon = 10,
        MassDriver = 11,
        CompositeBeam = 12,
        /// <summary>Strikecraft guns: strong against fighters, bombers and interceptors, weak against every ship.</summary>
        FighterLaser = 13,
        /// <summary>Interceptor guns: best against bombers, weaker than fighter guns against fighters and ships.</summary>
        InterceptorLaser = 14,
        /// <summary>Hero strikecraft guns: beat every squadron, hurt corvettes, and hit other heroes normally.</summary>
        HeroLaser = 15,
    }
}
