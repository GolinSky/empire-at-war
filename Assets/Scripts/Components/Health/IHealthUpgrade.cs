namespace EmpireAtWar.Components.Ship.Health
{
    /// <summary>Health of a unit that grows with its owner's upgrade level, such as a space station.</summary>
    public interface IHealthUpgrade
    {
        /// <param name="level">Hardpoints whose unlock level is at most this are installed.</param>
        /// <param name="hullScale">Multiplier of the base hull.</param>
        /// <param name="shieldsScale">Multiplier of the base shields.</param>
        /// <param name="shieldRegenerateScale">Multiplier of the base shield regeneration.</param>
        void Upgrade(int level, float hullScale, float shieldsScale, float shieldRegenerateScale);
    }
}
