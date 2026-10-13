using EmpireAtWar.Mvc;
using EmpireAtWar.Services.VContainer;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Factory;
using EmpireAtWar.Services.IdGeneration;
using UnityEngine;
using VContainer.Unity;

namespace EmpireAtWar.Ship
{
    public class ShipFactory : BaseFactory
    {
        private readonly ShipsData _shipsData;
        private readonly ILocalPlayer _localPlayer;
        private readonly IUniqueIdGenerator _ids;

        public ShipFactory(IAssetService assets, LifetimeScope scope, EntityScopeOwnership ownership, ShipsData shipsData,
            ILocalPlayer localPlayer, IUniqueIdGenerator ids) : base(assets, scope, ownership)
        {
            _shipsData = shipsData;
            _localPlayer = localPlayer;
            _ids = ids;
        }

        public virtual Ship Create(PlayerId owner, ShipType shipType, Vector3 position)
        {
            ShipData data = Assets.Load<ShipData>(_shipsData.GetShipDataPath(shipType));
            long entityId = _ids.GenerateUniqueId();
            return Spawn(Assets.LoadComponent<Ship>(shipType + "ShipView"), position, Quaternion.identity,
                (builder, view) => new ShipInstaller(view, data, _localPlayer, owner, entityId, position, shipType).Install(builder));
        }
    }
}
