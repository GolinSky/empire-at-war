using System;
using UnityEngine;

namespace EmpireAtWar.Views.MiniMap
{
    public interface IMiniMapView
    {
        event Action<Vector3> OnCameraMoveRequested;

        event Action<Vector3> OnMoveOrderRequested;

        void SetParent(Transform parent);

        void Show();

        void Hide();

        void PlayMoveTarget(Vector3 worldPoint);
    }
}
