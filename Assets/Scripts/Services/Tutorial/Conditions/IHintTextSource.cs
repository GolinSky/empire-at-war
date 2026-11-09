namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>A trigger that names what happened; the card replaces [token] placeholders such as [unit].</summary>
    public interface IHintTextSource
    {
        bool TryGetText(string token, out string value);
    }
}
