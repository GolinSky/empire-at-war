using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public interface IIonPulseView
    {
        void Show(Vector3 position, Vector3 direction, float radius);
        void Release();
    }
}
