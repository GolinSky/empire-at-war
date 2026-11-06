using EmpireAtWar.Extensions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.SpawnBlocking;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Components.FogOfWar
{
    /// <summary>Keeps hostile reinforcements away from a structure for as long as it exists.</summary>
    public sealed class SpawnBlockerSource : IInitializable, ILateDisposable
    {
        private readonly IBattleAreaWriter _areas;
        private readonly ISpawnBlockerData _data;
        private readonly Transform _viewTransform;
        private readonly PlayerId _owner;

        public SpawnBlockerSource(IBattleAreaWriter areas, ISpawnBlockerData data,
            [Key(EntityBindType.ViewTransform)] Transform viewTransform, PlayerId owner)
        {
            _areas = areas;
            _data = data;
            _viewTransform = viewTransform;
            _owner = owner;
        }

        public void Initialize() => _areas.Register(BattleAreaLayer.SpawnBlock, _owner, _viewTransform, _data.SpawnBlockRadius);

        public void LateDispose() => _areas.Unregister(BattleAreaLayer.SpawnBlock, _viewTransform);
    }
}
