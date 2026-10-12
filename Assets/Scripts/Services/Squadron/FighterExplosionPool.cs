using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Squadrons
{
    /// <summary>
    /// One set of fighter death explosions shared by every squadron, so squadron views no longer carry an
    /// explosion per fighter. An explosion in the local player's fog of war is not drawn.
    /// </summary>
    public sealed class FighterExplosionPool : IFighterExplosionPool, IInitializable
    {
        private const int PREWARM_COUNT = 16;
        // A fighter's footprint: the explosion is drawn when vision reaches any part of it.
        private const float REVEAL_RADIUS = 5f;

        private readonly ExplosionVfx _prefab;
        private readonly IVisionService _visionService;
        private readonly ILocalPlayer _localPlayer;
        private readonly List<ExplosionVfx> _explosions = new List<ExplosionVfx>();

        private Transform _root;

        public FighterExplosionPool(ExplosionVfx prefab, IVisionService visionService, ILocalPlayer localPlayer)
        {
            _prefab = prefab;
            _visionService = visionService;
            _localPlayer = localPlayer;
        }

        public void Initialize()
        {
            _root = new GameObject(nameof(FighterExplosionPool)).transform;
            for (int i = 0; i < PREWARM_COUNT; i++)
            {
                Create();
            }
        }

        public void Play(Vector3 position)
        {
            ExplosionVfx explosion = TakeIdle();
            explosion.transform.position = position;
            bool isHidden = !_visionService.IsAreaVisible(_localPlayer.Id, position, REVEAL_RADIUS);
            foreach (Renderer explosionRenderer in explosion.Renderers)
            {
                explosionRenderer.forceRenderingOff = isHidden;
            }

            explosion.Play();
        }

        private ExplosionVfx TakeIdle()
        {
            foreach (ExplosionVfx explosion in _explosions)
            {
                if (!explosion.IsPlaying) return explosion;
            }

            return Create();
        }

        private ExplosionVfx Create()
        {
            ExplosionVfx explosion = Object.Instantiate(_prefab, _root);
            _explosions.Add(explosion);
            return explosion;
        }
    }
}
