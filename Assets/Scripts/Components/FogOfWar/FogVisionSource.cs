using EmpireAtWar.Extentions;
using UnityEngine;
using ViewComponents;
using Zenject;

namespace EmpireAtWar.Components.FogOfWar
{
    /// <summary>Reveals the local fog of war around a friendly structure for as long as it exists.</summary>
    public sealed class FogVisionSource : IInitializable, ILateDisposable
    {
        private readonly IFogOfWarSystem _fogOfWarSystem;
        private readonly IFogVisionData _data;
        private readonly Transform _viewTransform;

        public FogVisionSource(IFogOfWarSystem fogOfWarSystem, IFogVisionData data,
            [Inject(Id = EntityBindType.ViewTransform)] Transform viewTransform)
        {
            _fogOfWarSystem = fogOfWarSystem;
            _data = data;
            _viewTransform = viewTransform;
        }

        public void Initialize() => _fogOfWarSystem.RegisterVisionSource(_viewTransform, _data.VisionRange);

        public void LateDispose() => _fogOfWarSystem.UnregisterVisionSource(_viewTransform);
    }
}
