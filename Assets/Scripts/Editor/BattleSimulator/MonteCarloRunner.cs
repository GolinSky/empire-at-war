using System;
using System.Collections.Generic;
using System.Diagnostics;
using EmpireAtWar.Components.AttackComponent;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public sealed class MonteCarloRunner
    {
        private readonly BattleSetup _setup;
        private readonly IReadOnlyDictionary<string, SimUnitDefinition> _definitions;
        private readonly DamageMatrixData _matrix;
        private BattleSimulation _current;
        public BattleSummary Summary { get; } = new BattleSummary();
        public bool IsComplete => Summary.Cancelled || Summary.Completed == _setup.Runs;
        public float Progress => (float)Summary.Completed / _setup.Runs;

        public MonteCarloRunner(BattleSetup setup, IReadOnlyDictionary<string, SimUnitDefinition> definitions, DamageMatrixData matrix)
        {
            if (setup.Runs != 1 && (setup.Runs < 10 || setup.Runs > 1000))
                throw new InvalidOperationException("Choose one run or 10–1000 seeds.");
            _setup = setup;
            _definitions = definitions;
            _matrix = matrix;
        }

        public void Advance(int milliseconds = 8)
        {
            var watch = Stopwatch.StartNew();
            while (!IsComplete && watch.ElapsedMilliseconds < milliseconds)
            {
                if (_current == null) _current = new BattleSimulation(_setup, _definitions, _matrix,
                    unchecked(_setup.Seed + Summary.Completed), Summary.Completed == 0);
                _current.Step();
                if (!_current.IsComplete) continue;
                Summary.Add(_current.Result);
                _current = null;
            }
        }

        public void Cancel() => Summary.Cancelled = true;
    }
}
