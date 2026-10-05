using System;
using UnityEngine;

namespace EmpireAtWar.Entities.Heroes
{
    public interface IHeroUi : IDisposable
    {
        void SetPresenter(IHeroPresenter presenter);
        void Initialize();
        void AddHero(long entityId, Sprite icon, bool isFriendly, bool canFocus);
        void RemoveHero(long entityId);
        void SetFocusable(long entityId, bool canFocus);
        void Show();
        void Hide();
    }
}
