using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using UnityEngine;

namespace EmpireAtWar.Services.Layer
{
    public interface ILayerService
    {
        LayerMask CoreGameCameraMask { get; }

        LayerMask CinemaCameraMask { get; }

        int GetLayer(LayerKey key);

        LayerMask GetMask(params LayerKey[] keys);

        bool IsInLayer(GameObject gameObject, LayerKey key);

        /// <summary>Children on render-only layers (WorldUi, Fog) keep their layer.</summary>
        void Apply(GameObject gameObject, LayerKey key, bool includeChildren);
    }

    public sealed class LayerService : ILayerService
    {
        private readonly Dictionary<LayerKey, int> _layers = new Dictionary<LayerKey, int>();
        private readonly int _renderOnlyMask;

        public LayerMask CoreGameCameraMask { get; }
        public LayerMask CinemaCameraMask { get; }

        public LayerService(LayerData layerModel)
        {
            Register(LayerKey.Unit, layerModel.UnitLayerMask);
            Register(LayerKey.Obstacle, layerModel.ObstacleLayerMask);
            Register(LayerKey.Dead, layerModel.DeadLayerMask);
            Register(LayerKey.WorldUi, layerModel.WorldUiLayerMask);
            Register(LayerKey.Fog, layerModel.FogLayerMask);
            CoreGameCameraMask = layerModel.CoreGameCameraMask;
            CinemaCameraMask = layerModel.CinemaCameraMask;
            _renderOnlyMask = GetMask(LayerKey.WorldUi, LayerKey.Fog);
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

        private void ApplyToChildren(Transform parent, int layer)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                // Physics layers must not pull world UI or fog back into the cinema camera.
                if ((_renderOnlyMask & (1 << child.gameObject.layer)) != 0)
                {
                    continue;
                }

                child.gameObject.layer = layer;
                ApplyToChildren(child, layer);
            }
        }

        private void Register(LayerKey key, LayerMask mask)
        {
            int value = mask.value;
            int layer = 0;
            while ((value >>= 1) != 0)
            {
                layer++;
            }

            _layers.Add(key, layer);
        }
    }
}
