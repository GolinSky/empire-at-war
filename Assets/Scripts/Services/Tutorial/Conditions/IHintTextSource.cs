namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>
    /// A trigger that names what happened, or a step condition that reports progress; the card replaces [token]
    /// placeholders such as [unit] or [count].
    /// </summary>
    public interface IHintTextSource
    {
        bool TryGetText(string token, out string value);
    }
}
