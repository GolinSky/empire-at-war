using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Saves
{
    public sealed class EconomySaveSource : PlayerSaveSource
    {
        private readonly EconomyModel _economy;

        public EconomySaveSource(PlayerSaveRegistry registry, PlayerSlot owner, EconomyModel economy)
            : base(registry, owner)
        {
            _economy = economy;
        }

        public override void Capture(PlayerSave save) => save.SetMoney(_economy.Money);

        public override void RestoreBeforeUnits(PlayerSave save) => _economy.Restore(save.Money);
    }
}
