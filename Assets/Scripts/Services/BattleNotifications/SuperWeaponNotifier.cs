using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.SuperWeapons;

namespace EmpireAtWar.Services.BattleNotifications
{
    /// <summary>Incoming hostile strikes on friendly units (named) and the local superweapons becoming ready.</summary>
    public sealed class SuperWeaponNotifier : IInitializable, ILateDisposable
    {
        private readonly IBattleNotifications _notifications;
        private readonly SuperWeaponFireService _fire;
        private readonly SuperWeaponModel _model;
        private readonly FactionCatalog _factions;
        private readonly FactionRoster _roster;
        private readonly ILocalPlayer _local;
        private readonly SuperWeaponCatalog _superWeapons;

        public SuperWeaponNotifier(IBattleNotifications notifications, SuperWeaponFireService fire,
            SuperWeaponModel model, FactionCatalog factions, FactionRoster roster, ILocalPlayer local,
            SuperWeaponCatalog superWeapons)
        {
            _notifications = notifications;
            _fire = fire;
            _model = model;
            _factions = factions;
            _roster = roster;
            _local = local;
            _superWeapons = superWeapons;
        }

        public void Initialize()
        {
            _fire.Fired += HandleFired;
            _model.OnStateChanged += HandleStateChanged;
        }

        public void LateDispose()
        {
            _fire.Fired -= HandleFired;
            _model.OnStateChanged -= HandleStateChanged;
        }

        private void HandleFired(SuperWeaponType type, IEntity target)
        {
            if (!_local.IsFriendly(target.Owner)) return;
            _notifications.Post(BattleNotificationPriority.Urgent, $"Incoming {WeaponName(type)} strike on {TargetName(target)}",
                iconKey: _superWeapons.Get(type).IconKey);
        }

        private void HandleStateChanged(SuperWeaponType type, SuperWeaponState state)
        {
            if (_model.IsRestoring || state != SuperWeaponState.Ready || !_model.CanFire(type)) return;
            _notifications.Post(BattleNotificationPriority.Progress, $"{_roster.SuperWeapons[type].Name} ready",
                iconKey: _roster.SuperWeapons[type].IconKey);
        }

        private string TargetName(IEntity target)
        {
            if (!target.TryGetFacade(out IUnitTypeFacade unit))
                return target.TryGetFacade(out IStationLevelFacade _) ? "your station" : "a friendly structure";
            return unit.UnitTypeId.IsShip
                ? _factions.GetShipFactionData(unit.UnitTypeId.ShipType).Name
                : _factions.GetSquadronFactionData(unit.UnitTypeId.SquadronType).Name;
        }

        // The attacker may belong to another faction, so the enum name stands in for the roster entry.
        private static string WeaponName(SuperWeaponType type)
        {
            switch (type)
            {
                case SuperWeaponType.IonCannon: return "Ion Cannon";
                case SuperWeaponType.HypervelocityGun: return "Hypervelocity Gun";
                case SuperWeaponType.PlasmaCannon: return "Plasma Cannon";
                default: throw new System.ArgumentOutOfRangeException(nameof(type), type, "Unknown superweapon.");
            }
        }
    }
}
