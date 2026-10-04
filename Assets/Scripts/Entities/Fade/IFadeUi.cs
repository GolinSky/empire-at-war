using System.Threading;
using UnityEngine;

namespace EmpireAtWar.Entities.Fade
{
    public interface IFadeUi
    {
        /// <summary>Covers the screen at once, without a fade.</summary>
        void Cover();

        /// <summary>Fades from clear to covered.</summary>
        Awaitable FadeInAsync(float duration, CancellationToken cancellationToken);

        /// <summary>Fades from covered to clear, then hides.</summary>
        Awaitable FadeOutAsync(float duration, CancellationToken cancellationToken);
    }
}
