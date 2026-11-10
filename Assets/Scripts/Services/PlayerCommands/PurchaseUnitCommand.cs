using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.PlayerCommands
{
    /// <summary>Queue a unit, research tier, station upgrade or super weapon charge, by kind and key.</summary>
    public readonly struct PurchaseUnitCommand
    {
        public PlayerId Issuer { get; }
        public UnitRequestKind Kind { get; }
        public int Key { get; }
        public int Tier { get; }

        public PurchaseUnitCommand(PlayerId issuer, UnitRequestKind kind, int key, int tier)
        {
            Issuer = issuer;
            Kind = kind;
            Key = key;
            Tier = tier;
        }

        public static PurchaseUnitCommand From(PlayerId issuer, UnitRequest request)
        {
            UnitRequestKey key = UnitRequestKey.From(request);
            return new PurchaseUnitCommand(issuer, key.Kind, key.Key, key.Tier);
        }
    }
}
