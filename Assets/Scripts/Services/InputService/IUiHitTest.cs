using UnityEngine;

namespace EmpireAtWar.Services.InputService
{
    public interface IUiHitTest
    {
        bool IsOverUi(Vector2 screenPosition);
    }
}
