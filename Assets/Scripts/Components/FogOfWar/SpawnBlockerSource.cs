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
        private readonly ISpawnBlockerService _spawnBlockerService;
        private readonly ISpawnBlockerData _data;
        private readonly Transform _viewTransform;
        private readonly PlayerId _owner;

        public SpawnBlockerSource(ISpawnBlockerService spawnBlockerService, ISpawnBlockerData data,
            [Key(EntityBindType.ViewTransform)] Transform viewTransform, PlayerId owner)
        {
            _spawnBlockerService = spawnBlockerService;
            _data = data;
            _viewTransform = viewTransform;
            _owner = owner;
        }

        public void Initialize() => _spawnBlockerService.Register(_owner, _viewTransform, _data.SpawnBlockRadius);

        public void LateDispose() => _spawnBlockerService.Unregister(_viewTransform);
    }
}
