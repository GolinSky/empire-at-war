using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Views.Cheats
{
    public interface ICheatView
    {
        event Action<string> AddMoneyRequested;
        event Action<ShipType> AddReinforcementRequested;
        event Action<ShipType> SpawnForceRequested;

        void SetShips(IReadOnlyList<ShipType> ships);
        void SetStatus(string status);
    }

    public sealed class CheatView : MonoBehaviour, ICheatView
    {
        private const int WINDOW_ID = 90421;
        private const float WINDOW_WIDTH = 360f;
        private const float WINDOW_HEIGHT = 430f;
        private const float WINDOW_MARGIN = 16f;
        private const string DEFAULT_MONEY_AMOUNT = "10000";

        private static readonly string[] TAB_NAMES = { "Economy", "Ships" };

        public event Action<string> AddMoneyRequested;
        public event Action<ShipType> AddReinforcementRequested;
        public event Action<ShipType> SpawnForceRequested;

        private Rect _windowRect = new Rect(
            WINDOW_MARGIN,
            WINDOW_MARGIN,
            WINDOW_WIDTH,
            WINDOW_HEIGHT);
        private ShipType[] _ships = Array.Empty<ShipType>();
        private string[] _shipNames = Array.Empty<string>();
        private string _moneyAmount = DEFAULT_MONEY_AMOUNT;
        private string _status = string.Empty;
        private int _selectedTab;
        private int _selectedShip;
        private bool _isExpanded = true;

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

        private void OnGUI()
        {
            GUI.depth = -1000;

            if (!_isExpanded)
            {
                if (GUI.Button(new Rect(WINDOW_MARGIN, WINDOW_MARGIN, 100f, 32f), "Cheats"))
                {
                    _isExpanded = true;
                }

                return;
            }

            _windowRect = GUILayout.Window(
                WINDOW_ID,
                _windowRect,
                DrawWindow,
                "Cheats",
                GUILayout.Width(WINDOW_WIDTH),
                GUILayout.Height(WINDOW_HEIGHT));
        }

        private void DrawWindow(int windowId)
        {
            _selectedTab = GUILayout.Toolbar(_selectedTab, TAB_NAMES);
            GUILayout.Space(8f);

            if (_selectedTab == 0)
            {
                DrawEconomyTab();
            }
            else
            {
                DrawShipsTab();
            }

            GUILayout.FlexibleSpace();
            if (!string.IsNullOrEmpty(_status))
            {
                GUILayout.Label(_status);
            }

            if (GUILayout.Button("Collapse"))
            {
                _isExpanded = false;
            }

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

        private void DrawShipsTab()
        {
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
