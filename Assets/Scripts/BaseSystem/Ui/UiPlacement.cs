using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.CoreGameUi;
using EmpireAtWar.Entities.Economy.Ui;
using EmpireAtWar.Entities.EndGameUi;
using EmpireAtWar.Entities.Faction.Ui;
using EmpireAtWar.Entities.Fade;
using EmpireAtWar.Entities.Fps;
using EmpireAtWar.Entities.Heroes;
using EmpireAtWar.Entities.MainMenu.Main;
using EmpireAtWar.Entities.MainMenu.Settings;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.Menu;
using EmpireAtWar.Entities.MiniMap;
using EmpireAtWar.Entities.Reinforcement;
using EmpireAtWar.Entities.ShipUi;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Entities.UnitOrderFeedback;
using UnityEngine;

namespace EmpireAtWar.Ui.Base
{
    public sealed class UiPlacement : MonoBehaviour, IUiPlacement
    {
        [SerializeField] private Canvas defaultCanvas;
        [SerializeField] private Canvas dynamicCanvas;
        [SerializeField] private Canvas popupCanvas;
        [SerializeField] private Canvas feedbackCanvas;
        [SerializeField] private Transform miniMapSlot;
        [SerializeField] private Transform buildPipelineSlot;
        [SerializeField] private Transform economySlot;
        [SerializeField] private UiContentLayout factionLayout;
        [SerializeField] private UiContentLayout shipLayout;
        [SerializeField] private Transform contextualSlot;
        [SerializeField] private Transform endGameSlot;
        [SerializeField] private Transform modalSlot;
        [SerializeField] private Transform tooltipSlot;
        [SerializeField] private Transform fadeSlot;
        private Dictionary<Type, Transform> _parents;

        public Canvas DefaultCanvas => defaultCanvas;
        public Canvas DynamicCanvas => dynamicCanvas;
        public Canvas FeedbackCanvas => feedbackCanvas;
        public Transform ContextualSlot => contextualSlot;
        public UiContentLayout FactionLayout => factionLayout;
        public UiContentLayout ShipLayout => shipLayout;

        public Transform GetParent<TUi>() where TUi : BaseUi
        {
            if (_parents == null)
            {
                _parents = new Dictionary<Type, Transform>
                {
                    { typeof(CoreGameUi), defaultCanvas.transform },
                    { typeof(EmpireAtWar.Entities.Game.SurvivalHudUi), dynamicCanvas.transform },
                    { typeof(EmpireAtWar.Entities.Game.SimulationHudUi), dynamicCanvas.transform },
                    { typeof(EmpireAtWar.Entities.MainMenu.Simulation.SimulationSetupUi), modalSlot },
                    { typeof(MainMenuUi), defaultCanvas.transform },
                    { typeof(EmpireAtWar.Entities.MainMenu.Play.PlayUi), defaultCanvas.transform },
                    { typeof(SkirmishUi), defaultCanvas.transform },
                    { typeof(EmpireAtWar.Entities.MainMenu.Survival.SurvivalUi), defaultCanvas.transform },
                    { typeof(ReinforcementUi), defaultCanvas.transform },
                    { typeof(HeroUi), defaultCanvas.transform },
                    { typeof(EconomyUi), economySlot },
                    { typeof(FactionUi), factionLayout.Viewport },
                    { typeof(ShipUi), shipLayout.Content },
                    { typeof(ShipGroupUi), shipLayout.Content },
                    { typeof(MiniMapUi), miniMapSlot },
                    { typeof(ShipBuildUi), buildPipelineSlot },
                    { typeof(FpsUi), dynamicCanvas.transform },
                    { typeof(UnitOrderFeedbackUi), feedbackCanvas.transform },
                    { typeof(CaptureSiteBuildUi), contextualSlot },
                    { typeof(EndGameUi), endGameSlot },
                    { typeof(PauseMenuUi), modalSlot },
                    { typeof(EmpireAtWar.Entities.Saves.SaveBrowserUi), modalSlot },
                    { typeof(SettingsUi), modalSlot },
                    { typeof(TooltipUi), tooltipSlot },
                    { typeof(FadeUi), fadeSlot }
                };
            }

            return _parents[typeof(TUi)];
        }
    }
}
