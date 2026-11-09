namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>One shared progress instance for the whole session; every scope sees the same object.</summary>
    public interface IHintProgressRepository
    {
        /// <summary>Loads once, then returns the same instance. Empty when nothing is saved or the file is unreadable.</summary>
        HintProgress Load();

        /// <summary>Throws <see cref="System.IO.IOException"/> or <see cref="System.UnauthorizedAccessException"/> when the file cannot be written.</summary>
        void Save(HintProgress progress);

        /// <summary>Forgets every seen hint and saves. A running match keeps its own queue until it ends.</summary>
        void Reset();
    }
}
