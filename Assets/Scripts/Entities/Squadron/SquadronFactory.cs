using EmpireAtWar.Mvc;
using EmpireAtWar.Services.VContainer;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Factory;
using EmpireAtWar.Services.IdGeneration;
using UnityEngine;
using VContainer.Unity;

namespace EmpireAtWar.Entities.Squadrons
{
    public class SquadronFactory : BaseFactory
    {
        private readonly ILocalPlayer _localPlayer;
        private readonly IUniqueIdGenerator _ids;

        public SquadronFactory(IAssetService assets, LifetimeScope scope, EntityScopeOwnership ownership, ILocalPlayer localPlayer,
            IUniqueIdGenerator ids) : base(assets, scope, ownership)
        {
            _localPlayer = localPlayer;
            _ids = ids;
        }

        public virtual Squadron Create(PlayerId owner, SquadronType squadronType, Vector3 position, Quaternion rotation)
        {
            SquadronData data = Assets.Load<SquadronData>(squadronType + "SquadronData");
            long entityId = _ids.GenerateUniqueId();
            return Spawn(Assets.LoadComponent<Squadron>(squadronType + "SquadronView"), position, rotation,
                (builder, view) => new SquadronInstaller(view, data, _localPlayer, owner, entityId, position,
                    squadronType, rotation).Install(builder));
        }
    }
}
