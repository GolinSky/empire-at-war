using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BattleNotifications;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.CoreGameUi;
using EmpireAtWar.Entities.Economy.Ui;
using EmpireAtWar.Entities.EndGameUi;
using EmpireAtWar.Entities.Faction.Ui;
using EmpireAtWar.Entities.Fade;
using EmpireAtWar.Entities.Fps;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Heroes;
using EmpireAtWar.Entities.MainMenu.Main;
using EmpireAtWar.Entities.MainMenu.Online;
using EmpireAtWar.Entities.MainMenu.Play;
using EmpireAtWar.Entities.MainMenu.ServerBrowser;
using EmpireAtWar.Entities.MainMenu.Settings;
using EmpireAtWar.Entities.MainMenu.Simulation;
using EmpireAtWar.Entities.MainMenu.SinglePlayer;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.MainMenu.Survival;
using EmpireAtWar.Entities.Menu;
using EmpireAtWar.Entities.MiniMap;
using EmpireAtWar.Entities.Popups;
using EmpireAtWar.Entities.Reinforcement;
using EmpireAtWar.Entities.Saves;
using EmpireAtWar.Entities.ShipUi;
using EmpireAtWar.Entities.TacticalMarkers;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Entities.TutorialUi;
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
                    { typeof(SurvivalHudUi), dynamicCanvas.transform },
                    { typeof(SimulationHudUi), dynamicCanvas.transform },
                    { typeof(SimulationSetupUi), modalSlot },
                    { typeof(MainMenuUi), defaultCanvas.transform },
                    { typeof(PlayUi), defaultCanvas.transform },
                    { typeof(SinglePlayerUi), defaultCanvas.transform },
                    { typeof(OnlineUi), defaultCanvas.transform },
                    { typeof(ServerBrowserUi), defaultCanvas.transform },
                    { typeof(SkirmishUi), defaultCanvas.transform },
                    { typeof(SurvivalUi), defaultCanvas.transform },
                    { typeof(ReinforcementUi), defaultCanvas.transform },
                    { typeof(HeroUi), defaultCanvas.transform },
                    { typeof(EconomyUi), economySlot },
                    { typeof(FactionUi), factionLayout.Viewport },
                    { typeof(ShipUi), shipLayout.Content },
                    { typeof(ShipGroupUi), shipLayout.Content },
                    { typeof(MiniMapUi), miniMapSlot },
                    { typeof(ShipBuildUi), buildPipelineSlot },
                    { typeof(FpsUi), dynamicCanvas.transform },
                    { typeof(HintUi), tooltipSlot },
                    { typeof(MarkerRadialUi), tooltipSlot },
                    { typeof(BattleNotificationUi), dynamicCanvas.transform },
                    { typeof(UnitOrderFeedbackUi), feedbackCanvas.transform },
                    { typeof(CaptureSiteBuildUi), contextualSlot },
                    { typeof(EndGameUi), endGameSlot },
                    { typeof(PauseMenuUi), modalSlot },
                    { typeof(SaveBrowserUi), modalSlot },
                    { typeof(SettingsUi), modalSlot },
                    { typeof(AcceptPopupUi), modalSlot },
                    { typeof(PopupBlockerUi), modalSlot },
                    { typeof(SteamInvitePopupUi), modalSlot },
                    { typeof(PlanetSelectionPopupUi), modalSlot },
                    { typeof(TooltipUi), tooltipSlot },
                    { typeof(FadeUi), fadeSlot }
                };
            }

            return _parents[typeof(TUi)];
        }
    }
}
