using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.DefendPlatform;
using UnityEngine;

namespace EmpireAtWar.Views.Cheats
{
    public sealed class CheatView : MonoBehaviour, ICheatView
    {
        private const int WINDOW_ID = 90421;

        private const float WINDOW_WIDTH = 480f;
        private const float WINDOW_HEIGHT = 560f;
        private const float WINDOW_MARGIN = 16f;
        private const float WINDOW_ANIMATION_DURATION = 0.18f;
        private const float WINDOW_COLLAPSED_SCALE = 0.85f;

        private const string DEFAULT_MONEY_AMOUNT = "10000";

        private static readonly string[] TAB_NAMES =
            { "Economy", "Ships", "Station", "Defend", "Enemy", "Superweapons", "Debug" };
        private IReadOnlyList<DefendPlatformType> _platforms = Array.Empty<DefendPlatformType>();
        private static readonly SuperWeaponType[] SUPER_WEAPONS =
            (SuperWeaponType[])Enum.GetValues(typeof(SuperWeaponType));
        private FactionType[] _factions = Array.Empty<FactionType>();
        private string[] _factionNames = Array.Empty<string>();
        private ShipType[] _ships = Array.Empty<ShipType>();
        private string[] _shipNames = Array.Empty<string>();

        private string _moneyAmount = DEFAULT_MONEY_AMOUNT;
        private string _status = string.Empty;

        private Rect _windowRect = new Rect(
            WINDOW_MARGIN,
            WINDOW_MARGIN,
            WINDOW_WIDTH,
            WINDOW_HEIGHT);

        private float _windowVisibility;

        private int _selectedTab;
        private int _selectedFaction;
        private int _selectedShip;

        private bool _isExpanded;
        private bool _showRanges;
        private Vector2 _scrollPosition;
        private IReadOnlyList<PlayerSlot> _enemies = Array.Empty<PlayerSlot>();

        public event Action LevelUpStationRequested;
        public event Action<FactionType> SpawnAllShipsRequested;
        public event Action<DefendPlatformType> AddPlatformReinforcementRequested;
        public event Action<DefendPlatformType> SpawnPlatformRequested;
        public event Action<PlayerId, string> AddEnemyMoneyRequested;
        public event Action<string> AddAllEnemiesMoneyRequested;

        public event Action<string> AddMoneyRequested;

        public event Action<FactionType> FactionSelected;

        public event Action<ShipType> AddReinforcementRequested;

        public event Action<ShipType> SpawnForceRequested;

        public event Action<SuperWeaponType> GrantSuperWeaponRequested;

        public event Action GrantAllSuperWeaponsRequested;

        public event Action<bool> RangeDebugToggled;

        public event Action DestroyOwnShipsRequested;

        public void SetFactions(IReadOnlyList<FactionType> factions)
        {
            _factions = new FactionType[factions.Count];
            _factionNames = new string[factions.Count];
            for (int index = 0; index < factions.Count; index++)
            {
                _factions[index] = factions[index];
                _factionNames[index] = factions[index].ToString();
            }

            _selectedFaction = 0;
        }

        public void SetShips(IReadOnlyList<ShipType> ships)
        {
            if (ships == null)
            {
                throw new ArgumentNullException(nameof(ships));
            }

            _ships = new ShipType[ships.Count];
            _shipNames = new string[ships.Count];
            for (int index = 0; index < ships.Count; index++)
            {
                _ships[index] = ships[index];
                _shipNames[index] = ships[index].ToString();
            }

            _selectedShip = 0;
        }

        public void SetStatus(string status)
        {
            _status = status ?? string.Empty;
        }

        public void SetEnemies(IReadOnlyList<PlayerSlot> enemies)
        {
            _enemies = enemies;
        }

        public void SetPlatforms(IReadOnlyList<DefendPlatformType> platforms)
        {
            _platforms = platforms;
        }

        private void DrawStationTab()
        {
            GUILayout.Label("Upgrade your station instantly for free");
            if (GUILayout.Button("Level Up Station", GUILayout.Height(36f)))
                LevelUpStationRequested?.Invoke();
        }

        private void DrawDefendTab()
        {
            foreach (DefendPlatformType platform in _platforms)
            {
                GUILayout.Label(platform.ToString());
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Add to Reinforcement", GUILayout.Height(36f)))
                    AddPlatformReinforcementRequested?.Invoke(platform);
                if (GUILayout.Button("Spawn in Home Area", GUILayout.Height(36f)))
                    SpawnPlatformRequested?.Invoke(platform);
                GUILayout.EndHorizontal();
                GUILayout.Space(8f);
            }
        }

        private void DrawEnemyTab()
        {
            if (_enemies.Count == 0)
            {
                GUILayout.Label("No enemies in this match.");
                return;
            }

            GUILayout.Label("Money amount per enemy");
            _moneyAmount = GUILayout.TextField(_moneyAmount, 16);
            foreach (PlayerSlot enemy in _enemies)
            {
                if (GUILayout.Button($"Add Money: {enemy.Id} ({enemy.Faction})", GUILayout.Height(36f)))
                    AddEnemyMoneyRequested?.Invoke(enemy.Id, _moneyAmount);
            }
            GUILayout.Space(8f);
            if (GUILayout.Button("Add Money to All Enemies", GUILayout.Height(36f)))
                AddAllEnemiesMoneyRequested?.Invoke(_moneyAmount);
        }

        private void Update()
        {
            float targetVisibility = _isExpanded ? 1f : 0f;
            _windowVisibility = Mathf.MoveTowards(
                _windowVisibility,
                targetVisibility,
                Time.unscaledDeltaTime / WINDOW_ANIMATION_DURATION);
        }

        private void OnGUI()
        {
            GUI.depth = -1000;

            if (!_isExpanded && _windowVisibility <= 0f)
            {
                if (GUI.Button(new Rect(WINDOW_MARGIN, WINDOW_MARGIN, 100f, 32f), "Cheats"))
                {
                    _isExpanded = true;
                }

                return;
            }

            float easedVisibility = Mathf.SmoothStep(0f, 1f, _windowVisibility);
            float scale = Mathf.Lerp(WINDOW_COLLAPSED_SCALE, 1f, easedVisibility);
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            bool wasEnabled = GUI.enabled;

            GUIUtility.ScaleAroundPivot(
                new Vector2(scale, scale),
                new Vector2(_windowRect.xMin, _windowRect.yMin));
            GUI.color = new Color(
                previousColor.r,
                previousColor.g,
                previousColor.b,
                previousColor.a * easedVisibility);
            GUI.enabled = wasEnabled && _isExpanded && _windowVisibility >= 1f;
            _windowRect = GUILayout.Window(
                WINDOW_ID,
                _windowRect,
                DrawWindow,
                "Cheats",
                GUILayout.Width(WINDOW_WIDTH),
                GUILayout.Height(WINDOW_HEIGHT));
            GUI.enabled = wasEnabled;
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private void DrawWindow(int windowId)
        {
            int selectedTab = GUILayout.SelectionGrid(_selectedTab, TAB_NAMES, 4);
            if (selectedTab != _selectedTab)
            {
                _selectedTab = selectedTab;
                _scrollPosition = Vector2.zero;
            }
            GUILayout.Space(8f);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            switch (_selectedTab)
            {
                case 0: DrawEconomyTab(); break;
                case 1: DrawShipsTab(); break;
                case 2: DrawStationTab(); break;
                case 3: DrawDefendTab(); break;
                case 4: DrawEnemyTab(); break;
                case 5: DrawSuperWeaponsTab(); break;
                case 6: DrawDebugTab(); break;
            }

            GUILayout.EndScrollView();
            if (!string.IsNullOrEmpty(_status)) GUILayout.Label(_status);

            if (GUILayout.Button("Collapse")) _isExpanded = false;
            GUI.DragWindow();
        }

        private void DrawEconomyTab()
        {
            GUILayout.Label("Money amount");
            _moneyAmount = GUILayout.TextField(_moneyAmount, 16);

            if (GUILayout.Button("Add Money", GUILayout.Height(36f)))
            {
                AddMoneyRequested?.Invoke(_moneyAmount);
            }
        }

        private void DrawSuperWeaponsTab()
        {
            GUILayout.Label("Grant a ready-to-fire charge");
            foreach (SuperWeaponType type in SUPER_WEAPONS)
            {
                if (GUILayout.Button(type.ToString(), GUILayout.Height(36f)))
                {
                    GrantSuperWeaponRequested?.Invoke(type);
                }
            }

            GUILayout.Space(8f);
            if (GUILayout.Button("Grant All", GUILayout.Height(36f)))
            {
                GrantAllSuperWeaponsRequested?.Invoke();
            }
        }

        private void DrawDebugTab()
        {
            bool showRanges = GUILayout.Toggle(_showRanges, "Show attack / radar range of selected units");
            if (showRanges != _showRanges)
            {
                _showRanges = showRanges;
                RangeDebugToggled?.Invoke(_showRanges);
            }
        }

        private void DrawShipsTab()
        {
            GUILayout.Label("Spawn one of every ship at your default zone");
            foreach (FactionType faction in _factions)
            {
                if (GUILayout.Button($"Spawn All {faction} Ships", GUILayout.Height(36f)))
                    SpawnAllShipsRequested?.Invoke(faction);
            }
            GUILayout.Space(8f);

            if (GUILayout.Button("Destroy All My Ships", GUILayout.Height(36f)))
            {
                DestroyOwnShipsRequested?.Invoke();
            }

            GUILayout.Label("Select faction");
            GUILayout.BeginHorizontal();
            for (int index = 0; index < _factions.Length; index++)
            {
                bool isSelected = index == _selectedFaction;
                if (GUILayout.Toggle(isSelected, _factionNames[index], GUI.skin.button) && !isSelected)
                {
                    _selectedFaction = index;
                    FactionSelected?.Invoke(_factions[index]);
                }
            }
            GUILayout.EndHorizontal();

            if (_ships.Length == 0)
            {
                GUILayout.Label("No ships are configured.");
                return;
            }

            GUILayout.Label("Select ship");
            _selectedShip = GUILayout.SelectionGrid(
                _selectedShip,
                _shipNames,
                2,
                GUILayout.Height(250f));

            if (GUILayout.Button("Add to Reinforcement", GUILayout.Height(36f)))
            {
                AddReinforcementRequested?.Invoke(_ships[_selectedShip]);
            }

            if (GUILayout.Button("Spawn at Default Zone", GUILayout.Height(36f)))
            {
                SpawnForceRequested?.Invoke(_ships[_selectedShip]);
            }
        }
    }
}
