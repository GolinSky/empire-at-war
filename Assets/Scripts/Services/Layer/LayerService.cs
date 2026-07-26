using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using UnityEngine;

namespace EmpireAtWar.Services.Layer
{
    public interface ILayerService
    {
        int GetLayer(LayerKey key);
        LayerMask GetMask(params LayerKey[] keys);
        bool IsInLayer(GameObject gameObject, LayerKey key);
        void Apply(GameObject gameObject, LayerKey key, bool includeChildren);
    }

    public sealed class LayerService : ILayerService
    {
        private readonly Dictionary<LayerKey, int> _layers = new Dictionary<LayerKey, int>();

        public LayerService(LayerModel layerModel)
        {
            Register(LayerKey.Player, layerModel.PlayerLayerMask);
            Register(LayerKey.Enemy, layerModel.EnemyLayerMask);
            Register(LayerKey.Obstacle, layerModel.ObstacleLayerMask);
            Register(LayerKey.Dead, layerModel.DeadLayerMask);
        }

        public int GetLayer(LayerKey key)
        {
            if (!_layers.TryGetValue(key, out int layer))
            {
                throw new InvalidOperationException($"Layer key '{key}' is not configured.");
            }

            return layer;
        }

        public LayerMask GetMask(params LayerKey[] keys)
        {
            int mask = 0;
            for (int i = 0; i < keys.Length; i++)
            {
                mask |= 1 << GetLayer(keys[i]);
            }

            return mask;
        }

        public bool IsInLayer(GameObject gameObject, LayerKey key)
        {
            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }

            return gameObject.layer == GetLayer(key);
        }

        public void Apply(GameObject gameObject, LayerKey key, bool includeChildren)
        {
            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }

            int layer = GetLayer(key);
            gameObject.layer = layer;
            if (!includeChildren)
            {
                return;
            }

            ApplyToChildren(gameObject.transform, layer);
        }

        private static void ApplyToChildren(Transform parent, int layer)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                child.gameObject.layer = layer;
                ApplyToChildren(child, layer);
            }
        }

        private void Register(LayerKey key, LayerMask mask)
        {
            int value = mask.value;
            if (value == 0 || (value & (value - 1)) != 0)
            {
                throw new InvalidOperationException($"Layer key '{key}' must map to exactly one Unity layer.");
            }

            int layer = 0;
            while ((value >>= 1) != 0)
            {
                layer++;
            }

            _layers.Add(key, layer);
        }
    }
}
