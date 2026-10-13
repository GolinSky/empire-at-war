using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public sealed class BattleSimulatorTimelineChart : VisualElement
    {
        private readonly BattleResult _result;
        public BattleSimulatorTimelineChart(BattleResult result)
        {
            _result = result;
            AddToClassList("sim-chart");
            tooltip = "Blue: Side A hull + shields. Orange: Side B hull + shields. Vertical ticks: kills.";
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext context)
        {
            if (_result.Timeline.Count == 0) return;
            var painter = context.painter2D;
            float width = contentRect.width, height = contentRect.height;
            float max = Math.Max(1, _result.Timeline.Max(s => Math.Max(s.HullA + s.ShieldsA, s.HullB + s.ShieldsB)));
            double duration = Math.Max(BattleSimulation.STEP_SECONDS, _result.Duration);
            painter.lineWidth = 2;
            for (int side = 0; side < 2; side++)
            {
                painter.strokeColor = side == 0 ? new Color(0.49f, 0.67f, 1) : new Color(0.91f, 0.68f, 0.49f);
                painter.BeginPath();
                bool first = true;
                foreach (BattleSample sample in _result.Timeline)
                {
                    float health = side == 0 ? sample.HullA + sample.ShieldsA : sample.HullB + sample.ShieldsB;
                    var point = new Vector2((float)(sample.Time / duration) * width, height - health / max * height);
                    if (first) painter.MoveTo(point); else painter.LineTo(point);
                    first = false;
                }
                painter.Stroke();
            }
            painter.lineWidth = 1; painter.strokeColor = new Color(0.8f, 0.3f, 0.3f, 0.6f);
            foreach (BattleEvent entry in _result.Events.Where(e => e.Kind == "Kill"))
            {
                float x = (float)(entry.Time / duration) * width;
                painter.BeginPath(); painter.MoveTo(new Vector2(x, height)); painter.LineTo(new Vector2(x, height - 18)); painter.Stroke();
            }
        }
    }
}
