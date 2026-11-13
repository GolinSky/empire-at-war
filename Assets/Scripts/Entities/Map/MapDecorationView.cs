using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    public static class MapDecorationView
    {
        public static void Spawn(MapDecorationSpot spot, Transform parent)
        {
            GameObject instance = Object.Instantiate(spot.Prefab, spot.Position,
                Quaternion.Euler(0f, spot.Yaw, 0f), parent);
            instance.transform.localScale = spot.Prefab.transform.localScale * spot.Scale;
        }
    }
}
