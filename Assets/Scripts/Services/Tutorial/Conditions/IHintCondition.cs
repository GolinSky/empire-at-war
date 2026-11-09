using System;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Latches once its event happens; disposing stops listening.</summary>
    public interface IHintCondition : IDisposable
    {
        bool IsMet { get; }
    }
}
