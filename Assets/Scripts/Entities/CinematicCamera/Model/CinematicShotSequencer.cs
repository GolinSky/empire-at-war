using System;

namespace EmpireAtWar.Entities.CinematicCamera.Model
{
    public class CinematicShotSequencer
    {
        // Markov transition weights: row = previous shot, column = next shot,
        // both in CinematicShotType order. Zero diagonal prevents repeating a shot.
        private static readonly float[,] _transitionWeights =
        {
            //          Side FrontRear LowHigh Chase Wide
            /* Side */      { 0f, 3f, 3f, 2f, 2f },
            /* FrontRear */ { 3f, 0f, 2f, 2f, 3f },
            /* LowHigh */   { 3f, 2f, 0f, 3f, 2f },
            /* Chase */     { 3f, 2f, 3f, 0f, 2f },
            /* Wide */      { 4f, 3f, 3f, 3f, 0f },
        };

        private readonly Random _random;
        private readonly float _minDuration;
        private readonly float _maxDuration;

        public CinematicShotSequencer(Random random, float minDuration, float maxDuration)
        {
            _random = random;
            _minDuration = minDuration;
            _maxDuration = maxDuration;
        }

        public CinematicShot First()
        {
            return CreateShot(CinematicShotType.Wide);
        }

        public CinematicShot Next(CinematicShotType previous)
        {
            int row = (int)previous;
            int columns = _transitionWeights.GetLength(1);
            float total = 0f;
            for (int column = 0; column < columns; column++)
            {
                total += _transitionWeights[row, column];
            }

            float roll = (float)_random.NextDouble() * total;
            for (int column = 0; column < columns; column++)
            {
                roll -= _transitionWeights[row, column];
                if (roll < 0f)
                {
                    return CreateShot((CinematicShotType)column);
                }
            }

            return CreateShot((CinematicShotType)(columns - 1));
        }

        private CinematicShot CreateShot(CinematicShotType type)
        {
            float side = _random.Next(2) == 0 ? -1f : 1f;
            float duration = _minDuration + (float)_random.NextDouble() * (_maxDuration - _minDuration);
            return new CinematicShot(type, side, duration);
        }
    }
}
