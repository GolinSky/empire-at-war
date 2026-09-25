namespace EmpireAtWar.Services.SuperWeapons
{
    /// <summary>Lets a scene object offer itself as the superweapon origin without the weapons knowing its type.</summary>
    public interface ISuperWeaponOriginRegistry
    {
        void Register(ISuperWeaponOrigin origin);
        void Unregister(ISuperWeaponOrigin origin);
    }
}
