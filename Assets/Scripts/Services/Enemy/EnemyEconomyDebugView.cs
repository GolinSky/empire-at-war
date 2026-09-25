#if UNITY_EDITOR || DEVELOPMENT_BUILD
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Services.Economy;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyEconomyDebugView : MonoBehaviour
    {
        private const float PANEL_WIDTH = 220f;
        private const float PANEL_HEIGHT = 80f;
        private const float PANEL_MARGIN = 10f;

        private IEconomyModelObserver _economyModel;
        private EconomyService _economyService;
        private EnemyFactionData _factionModel;

        [Inject]
        private void Construct(
            IEconomyModelObserver economyModel,
            EconomyService economyService,
            EnemyFactionData factionModel)
        {
            _economyModel = economyModel;
            _economyService = economyService;
            _factionModel = factionModel;
        }

        private void OnGUI()
        {
            Rect panel = new Rect(
                Screen.width - PANEL_WIDTH - PANEL_MARGIN,
                PANEL_MARGIN,
                PANEL_WIDTH,
                PANEL_HEIGHT);
            GUI.Box(panel, "Enemy Economy");
            GUI.Label(
                new Rect(panel.x + PANEL_MARGIN, panel.y + 22f, PANEL_WIDTH - PANEL_MARGIN * 2f, 60f),
                $"Money: {_economyModel.Money:0}\n" +
                $"Income: +{_economyService.TotalIncome:0.#} / tick\n" +
                $"Level: {_factionModel.CurrentLevel}");
        }
    }
}
#endif
