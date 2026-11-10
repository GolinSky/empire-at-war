using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Battle;
using UnityEngine;

namespace EmpireAtWar.Services.Tutorial.Guided
{
    /// <summary>
    /// Drives the guided tutorial battle for the local player: spawns each scripted enemy group once its lesson is
    /// finished, tops the player's money up so a lesson never waits on income, and speeds up the player's builds.
    /// </summary>
    public sealed class TutorialDirector : IInitializable, ITickable, ILateDisposable
    {
        private const float GOLDEN_ANGLE = 2.39996f;

        private readonly TutorialScript _script;
        private readonly TutorialHintProgress _progress;
        private readonly IEconomyModelObserver _money;
        private readonly IEconomyProvider _economy;
        private readonly IFactionLevelObserver _level;
        private readonly PlayerFactionModel _production;
        private readonly PlayerFactoryLookup _spawner;
        private readonly IMapModelObserver _map;
        private readonly SkirmishStateModel _state;
        private readonly PlayerSlot _player;
        private readonly PlayerSlot _enemy;
        private readonly HashSet<TutorialEnemyGroup> _spawned = new HashSet<TutorialEnemyGroup>();
        private bool _groupsDue = true;

        public TutorialDirector(TutorialScript script, TutorialHintProgress progress, IEconomyModelObserver money,
            IEconomyProvider economy, IFactionLevelObserver level, PlayerFactionModel production, PlayerFactoryLookup spawner,
            IMapModelObserver map,
            SkirmishStateModel state, BattleDefinition battle, PlayerSlot player)
        {
            _script = script;
            _progress = progress;
            _money = money;
            _economy = economy;
            _level = level;
            _production = production;
            _spawner = spawner;
            _map = map;
            _state = state;
            _player = player;
            _enemy = battle.Players.Single(entry => entry.Role == PlayerRole.AiOpponent).Slot;
        }

        public void Initialize()
        {
            _progress.Changed += HandleProgressChanged;
            _money.OnMoneyChanged += HandleMoneyChanged;
            _level.LevelUpgradeCompleted += HandleLevelUpgraded;
            TopUpMoney();
        }

        // Spawning waits for a running battle: units must not appear while the battle is still loading.
        public void Tick()
        {
            if (_state.State != BattleState.Running) return;
            // Production already advanced by deltaTime this frame; the extra share makes builds BuildSpeed times faster.
            if (_script.BuildSpeed > 1f) _production.Advance(Time.deltaTime * (_script.BuildSpeed - 1f));
            if (!_groupsDue) return;
            _groupsDue = false;
            HintProgress progress = _progress.Load();
            foreach (TutorialEnemyGroup group in _script.EnemyGroups)
            {
                if (_spawned.Contains(group)) continue;
                if (!string.IsNullOrEmpty(group.AfterLesson) && !progress.IsDone(group.AfterLesson)) continue;
                _spawned.Add(group);
                Spawn(group);
            }
        }

        private void Spawn(TutorialEnemyGroup group)
        {
            Vector3 anchor = Vector3.Lerp(_map.GetStationPosition(_enemy.Id), _map.GetStationPosition(_player.Id),
                group.AlongFromEnemy);
            List<UnitTypeId> units = Expand(group.GetUnits(_enemy.Faction));
            for (int i = 0; i < units.Count; i++)
            {
                // Sunflower spread: units fill the circle evenly without overlapping.
                float radius = _script.SpawnSpread * Mathf.Sqrt((i + 0.5f) / units.Count);
                float angle = i * GOLDEN_ANGLE;
                Vector3 position = anchor + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                _spawner.Spawn(_enemy.Id, units[i], position);
            }
        }

        private static List<UnitTypeId> Expand(TutorialFactionUnits units)
        {
            List<UnitTypeId> expanded = new List<UnitTypeId>();
            foreach (SurvivalWaveEntry entry in units.Units)
                for (int count = 0; count < entry.Quantity; count++)
                    expanded.Add(entry.Unit);
            return expanded;
        }

        private void TopUpMoney()
        {
            float floor = _script.GetMoneyFloor(_level.CurrentLevel);
            if (_money.Money < floor) _economy.AddMoney(floor - _money.Money);
        }

        private void HandleProgressChanged() => _groupsDue = true;
        private void HandleMoneyChanged(float money) => TopUpMoney();
        private void HandleLevelUpgraded(int level) => TopUpMoney();

        public void LateDispose()
        {
            _progress.Changed -= HandleProgressChanged;
            _money.OnMoneyChanged -= HandleMoneyChanged;
            _level.LevelUpgradeCompleted -= HandleLevelUpgraded;
        }
    }
}
