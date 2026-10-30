using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Squadrons
{
    /// <summary>One position snapshot shared by all squadron searches in a frame.</summary>
    public sealed class SquadronTargetIndex
    {
        private const float CELL_SIZE = 500f;
        private readonly IEntityLocator _entities;
        private readonly IPlayerRelations _relations;
        private readonly Dictionary<Vector2Int, List<Target>> _cells = new Dictionary<Vector2Int, List<Target>>();
        private readonly Stack<List<Target>> _freeCells = new Stack<List<Target>>();
        private readonly List<Target> _all = new List<Target>();
        private static readonly IComparer<Target> OrderComparer = Comparer<Target>.Create((a, b) => a.Order.CompareTo(b.Order));
        private int _frame = -1;

        public SquadronTargetIndex(IEntityLocator entities, IPlayerRelations relations)
        {
            _entities = entities;
            _relations = relations;
        }

        public void Query(PlayerId side, Vector3 center, float radius, List<Target> results)
        {
            Refresh();
            results.Clear();
            // Hunting covers the whole map; avoid an unbounded walk through empty grid cells.
            if (float.IsPositiveInfinity(radius) || radius / CELL_SIZE > 8f)
            {
                foreach (Target target in _all)
                    if (_relations.IsHostile(side, target.Entity.Owner)) results.Add(target);
                return;
            }

            Vector2Int min = Cell(center - new Vector3(radius, 0f, radius));
            Vector2Int max = Cell(center + new Vector3(radius, 0f, radius));
            for (int x = min.x; x <= max.x; x++)
            for (int z = min.y; z <= max.y; z++)
            {
                if (!_cells.TryGetValue(new Vector2Int(x, z), out List<Target> cell)) continue;
                foreach (Target target in cell)
                    if (_relations.IsHostile(side, target.Entity.Owner)) results.Add(target);
            }
            // Preserve the locator's candidate order for ties and cheap rejection before cloak/vision queries.
            results.Sort(OrderComparer);
        }

        private void Refresh()
        {
            if (_frame == Time.frameCount) return;
            _frame = Time.frameCount;
            foreach (List<Target> cell in _cells.Values)
            {
                cell.Clear();
                _freeCells.Push(cell);
            }
            _cells.Clear();
            _all.Clear();
            foreach (IEntity entity in _entities.Entities)
            {
                if (entity.HealthModel.IsDestroyed || !entity.HealthModel.HasUnits) continue;
                Target target = new Target(entity, entity.GetFacade<IEntityTransformFacade>().Transform.position, _all.Count);
                _all.Add(target);
                Vector2Int key = Cell(target.Position);
                if (!_cells.TryGetValue(key, out List<Target> cell))
                {
                    cell = _freeCells.Count > 0 ? _freeCells.Pop() : new List<Target>();
                    _cells.Add(key, cell);
                }
                cell.Add(target);
            }
        }

        private static Vector2Int Cell(Vector3 position) =>
            new Vector2Int(Mathf.FloorToInt(position.x / CELL_SIZE), Mathf.FloorToInt(position.z / CELL_SIZE));

        public readonly struct Target
        {
            public readonly IEntity Entity;
            public readonly Vector3 Position;
            public readonly int Order;

            public Target(IEntity entity, Vector3 position, int order)
            {
                Entity = entity;
                Position = position;
                Order = order;
            }
        }
    }
}
