using EmpireAtWar.Extensions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Vision;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Components.FogOfWar
{
    /// <summary>Gives the owner's team vision around a structure for as long as it exists.</summary>
    public sealed class FogVisionSource : IInitializable, ILateDisposable
    {
        private readonly IVisionService _visionService;
        private readonly IFogVisionData _data;
        private readonly Transform _viewTransform;
        private readonly PlayerId _owner;

        public FogVisionSource(IVisionService visionService, IFogVisionData data,
            [Inject(Id = EntityBindType.ViewTransform)] Transform viewTransform, PlayerId owner)
        {
            _visionService = visionService;
            _data = data;
            _viewTransform = viewTransform;
            _owner = owner;
        }

        public void Initialize() => _visionService.Register(_owner, _viewTransform, _data.VisionRange);

        public void LateDispose() => _visionService.Unregister(_viewTransform);
    }
}
