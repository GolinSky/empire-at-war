using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Entities.Game
{
    public interface IGameModelObserver : IModelObserver
    {
        PlanetType PlanetType { get; }
        FactionType PlayerFactionType { get; }
        FactionType EnemyFactionType { get; }
    }

    [CreateAssetMenu(fileName = "GameModel", menuName = "Model/GameModel")]
    public class GameModel : Model, IGameModelObserver
    {
        public GameMode GameMode { get; set; }
        public PlanetType PlanetType { get; set; }
        public FactionType PlayerFactionType { get; set; }
        public FactionType EnemyFactionType { get; set; } = FactionType.Separatist;
    }
}