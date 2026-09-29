using UnityEngine;

namespace EmpireAtWar.Services.Input
{
    public interface IUiHitTest
    {
        bool IsOverUi(Vector2 screenPosition);
    }
}
