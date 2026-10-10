using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health
{
    public interface IIonStunView
    {
        void Configure(Bounds bounds);

        void SetActive(bool active);

        void Release();
    }
}
