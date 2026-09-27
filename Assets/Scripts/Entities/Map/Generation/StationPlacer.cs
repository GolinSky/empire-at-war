using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Puts the player's station in a random corner and the opponent's in the diagonally opposite one.</summary>
    public sealed class StationPlacer
    {
        private readonly MapGenerationSettings _settings;

        public StationPlacer(MapGenerationSettings settings)
        {
            _settings = settings;
        }

        public MapStation[] Place(
            Vector2Range bounds, FactionType playerFaction, FactionType opponentFaction, Random random)
        {
            Vector2 center = (bounds.Min + bounds.Max) * 0.5f;
            Vector2 cornerOffset = (bounds.Max - bounds.Min) * 0.5f -
                Vector2.one * _settings.StationEdgeDistance;
            Vector2 corner = new Vector2(
                random.Next(2) == 0 ? -cornerOffset.x : cornerOffset.x,
                random.Next(2) == 0 ? -cornerOffset.y : cornerOffset.y);

            return new[]
            {
                CreateStation(playerFaction, PlayerType.Player, center + corner),
                CreateStation(opponentFaction, PlayerType.Opponent, center - corner)
            };
        }

        private MapStation CreateStation(FactionType faction, PlayerType owner, Vector2 position)
        {
            return new MapStation(
                faction,
                owner,
                new Vector3(position.x, _settings.StationHeight, position.y),
                _settings.GetStationRadius(faction));
        }
    }
}
