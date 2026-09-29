using System;
using System.Collections.Generic;
using EmpireAtWar.Components.TeamColor;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.ViewComponents.Wreck;
using UnityEngine;
using UnityEngine.Pool;
using Zenject;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Services.UnitWreck
{
    /// <summary>
    /// Owns every wreck in the battle: one pool per <see cref="UnitWreckData"/>, and the lifetime of each
    /// active wreck. The wreck animates itself in the shader; this class only hands it out and takes it back.
    /// Time is <see cref="Time.time"/>, the clock the shader's _Time.y follows in Play Mode.
    /// Not timeSinceLevelLoad: that resets when the battle loads, _Time.y does not.
    /// </summary>
    public sealed class UnitWreckService : Service, IUnitWreckService, ITickable, IDisposable
    {
        private readonly IPlayerRoster _roster;
        private readonly Dictionary<UnitWreckData, ObjectPool<UnitWreckView>> _pools =
            new Dictionary<UnitWreckData, ObjectPool<UnitWreckView>>();
        private readonly List<ActiveWreck> _activeWrecks = new List<ActiveWreck>();
        private readonly System.Random _random = new System.Random();
        private Transform _root;

        public UnitWreckService(IPlayerRoster roster)
        {
            _roster = roster;
        }

        public void Spawn(UnitWreckData data, Transform unit, PlayerId owner, float delay)
        {
            if (CountActive(data) >= data.MaxActive)
            {
                ReleaseOldest(data);
            }

            float startTime = Time.time + delay;
            WreckCutPlan cutPlan = WreckCutPlan.Create(data.ThreePartChance, data.MinCutRatio, data.MaxCutRatio, _random);
            UnitWreckView view = GetPool(data).Get();
            view.Show(unit.position, unit.rotation, TeamColorView.GetUserValue(owner, _roster), startTime,
                cutPlan, (float)_random.NextDouble(), data);
            _activeWrecks.Add(new ActiveWreck(data, view, startTime + data.Lifetime));
        }

        public void Tick()
        {
            float now = Time.time;
            for (int i = _activeWrecks.Count - 1; i >= 0; i--)
            {
                if (now >= _activeWrecks[i].EndTime)
                {
                    Release(i);
                }
            }
        }

        public void Dispose()
        {
            for (int i = _activeWrecks.Count - 1; i >= 0; i--)
            {
                Release(i);
            }

            foreach (ObjectPool<UnitWreckView> pool in _pools.Values)
            {
                pool.Dispose();
            }

            _pools.Clear();
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }
        }

        private ObjectPool<UnitWreckView> GetPool(UnitWreckData data)
        {
            if (_pools.TryGetValue(data, out ObjectPool<UnitWreckView> pool))
            {
                return pool;
            }

            if (_root == null)
            {
                _root = new GameObject("UnitWrecks").transform;
            }

            pool = new ObjectPool<UnitWreckView>(
                () => Object.Instantiate(data.Prefab, _root),
                actionOnRelease: view => view.Hide(),
                actionOnDestroy: view => Object.Destroy(view.gameObject),
                collectionCheck: false,
                defaultCapacity: data.MaxActive,
                maxSize: data.MaxActive);
            _pools.Add(data, pool);
            return pool;
        }

        private int CountActive(UnitWreckData data)
        {
            int count = 0;
            foreach (ActiveWreck wreck in _activeWrecks)
            {
                if (wreck.Data == data) count++;
            }

            return count;
        }

        private void ReleaseOldest(UnitWreckData data)
        {
            // Wrecks are appended in spawn order, so the first match is the oldest.
            for (int i = 0; i < _activeWrecks.Count; i++)
            {
                if (_activeWrecks[i].Data == data)
                {
                    Release(i);
                    return;
                }
            }
        }

        private void Release(int index)
        {
            ActiveWreck wreck = _activeWrecks[index];
            _activeWrecks.RemoveAt(index);
            _pools[wreck.Data].Release(wreck.View);
        }

        private readonly struct ActiveWreck
        {
            public ActiveWreck(UnitWreckData data, UnitWreckView view, float endTime)
            {
                Data = data;
                View = view;
                EndTime = endTime;
            }

            public UnitWreckData Data { get; }
            public UnitWreckView View { get; }
            public float EndTime { get; }
        }
    }
}
