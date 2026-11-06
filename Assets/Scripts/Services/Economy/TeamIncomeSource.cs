using System.Collections.Generic;
using EmpireAtWar.Components.Construction;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Player;

namespace EmpireAtWar.Controllers.Economy
{
    public sealed class TeamIncomeSource : IIncomeProvider
    {
        private readonly float _baseIncome;
        private readonly ConstructionComponent _construction;
        private readonly IFactionResearchModelObserver _research;
        private readonly IPlayerRoster _roster;
        private readonly IPlayerRegistry _players;
        private readonly PlayerId _owner;
        private readonly List<IEconomyProvider> _economies = new();

        public float Income => _economies.Count == 0 || _construction.IsConstructing
            ? 0f : _baseIncome * _research.IncomeMultiplier / _economies.Count;

        public TeamIncomeSource(float baseIncome, ConstructionComponent construction,
            IFactionResearchModelObserver research, IPlayerRoster roster, IPlayerRegistry players, PlayerId owner)
        {
            _baseIncome = baseIncome;
            _construction = construction;
            _research = research;
            _roster = roster;
            _players = players;
            _owner = owner;
        }

        public void Register()
        {
            if (_baseIncome <= 0f) return;
            foreach (PlayerSlot player in _roster.Players)
                if (_roster.IsAllied(_owner, player.Id))
                    _economies.Add(_players.GetEconomy(player.Id));

            // AddProvider reads Income immediately, so establish the final divisor first.
            foreach (IEconomyProvider economy in _economies) economy.AddProvider(this);
            _construction.Completed += Recalculate;
            _research.OnResearchCompleted += OnResearchCompleted;
        }

        public void Release()
        {
            if (_baseIncome <= 0f) return;
            _construction.Completed -= Recalculate;
            _research.OnResearchCompleted -= OnResearchCompleted;
            foreach (IEconomyProvider economy in _economies) economy.RemoveProvider(this);
            _economies.Clear();
        }

        private void OnResearchCompleted(ResearchType researchType) => Recalculate();

        private void Recalculate()
        {
            foreach (IEconomyProvider economy in _economies) economy.RecalculateIncome(this);
        }
    }
}
