using System.Collections.Generic;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Pooling;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class HarmonicBombEffects : ITickable, ILateDisposable
    {
        private const int MAX_IDLE_VIEWS = 8;

        private readonly IVisionService _vision;
        private readonly ILocalPlayer _localPlayer;
        private readonly IWorldSfxService _sfx;
        private readonly List<HarmonicBombView> _active = new List<HarmonicBombView>();
        private ViewPool<HarmonicBombView> _pool;
        private SfxProfile _detonation;

        public HarmonicBombEffects(IVisionService vision, ILocalPlayer localPlayer, IWorldSfxService sfx)
        {
            _vision = vision;
            _localPlayer = localPlayer;
            _sfx = sfx;
        }

        public HarmonicBombView Drop(Vector3 position, HarmonicBombView prefab)
        {
            if (_pool == null)
            {
                _pool = new ViewPool<HarmonicBombView>(prefab, view => view.gameObject.SetActive(false),
                    "Harmonic Bombs", MAX_IDLE_VIEWS);
                _detonation = prefab.Detonation;
            }
            HarmonicBombView marker = _pool.Get();
            marker.ShowMarker(position);
            UpdateVisibility(marker);
            _active.Add(marker);
            return marker;
        }

        public void Detonate(HarmonicBombView marker, float radius, PlayerId owner, Vector3 position)
        {
            marker.BeginBlast(radius);
            _sfx.TryPlayOneShot(owner, _detonation, position);
        }

        public void Tick()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                HarmonicBombView view = _active[i];
                if (!view.Advance(Time.deltaTime))
                {
                    UpdateVisibility(view);
                    continue;
                }
                _active.RemoveAt(i);
                _pool.Release(view);
            }
        }

        private void UpdateVisibility(HarmonicBombView view)
        {
            Transform ring = view.transform;
            view.SetVisible(_vision.IsAreaVisible(_localPlayer.Id, ring.position, ring.localScale.x));
        }

        public void Clear()
        {
            if (_pool == null) return;
            _sfx.StopOneShots(_detonation);
            foreach (HarmonicBombView view in _active) _pool.Release(view);
            _active.Clear();
            _pool.Dispose();
            _pool = null;
        }

        public void LateDispose() => Clear();
    }
}
