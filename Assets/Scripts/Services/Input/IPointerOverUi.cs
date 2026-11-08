namespace EmpireAtWar.Services.Input
{
    /// <summary>Whether the cursor currently rests on UI that receives pointer events, so world input must yield.</summary>
    public interface IPointerOverUi
    {
        bool IsPointerOverUi { get; }
    }
}
