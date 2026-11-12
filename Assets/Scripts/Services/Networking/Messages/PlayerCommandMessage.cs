using EmpireAtWar.Services.PlayerCommands;
using Mirror;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>Client → host, reliable. Exactly one command field is set, chosen by <see cref="Kind"/>.</summary>
    public struct PlayerCommandMessage : NetworkMessage
    {
        public PlayerCommandKind Kind;
        public uint LastReceivedTick;
        public UnitOrderCommand UnitOrder;
        public PurchaseUnitCommand Purchase;
        public CancelPurchaseCommand CancelPurchase;
        public BuildSiteFacilityCommand BuildSiteFacility;
        public FireSuperWeaponCommand FireSuperWeapon;
        public ShipAbilityCommand ShipAbility;
        public DeployReinforcementCommand DeployReinforcement;
        public PlaceMarkerCommand PlaceMarker;
    }
}
