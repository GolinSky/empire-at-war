using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Planet;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Models.Game
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