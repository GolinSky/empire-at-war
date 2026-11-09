namespace EmpireAtWar.Entities.Heroes
{
    public interface IHeroPresenter
    {
        void SelectHero(long entityId);
        void FocusHero(long entityId);
    }
}
