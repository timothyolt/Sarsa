using System;

namespace Sarsa
{
    public enum Direction
    {
        Up, Down, Right, Left
    }

    public static class DirectionUtils
    {
        public static Direction Opposite(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return Direction.Down;
                case Direction.Down:
                    return Direction.Up;
                case Direction.Right:
                    return Direction.Left;
                case Direction.Left:
                    return Direction.Right;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        public static (int, int) Act(this Direction direction, int x, int y)
        {
            switch (direction)
            {
                case Direction.Up:
                    return (x, ++y);
                case Direction.Down:
                    return (x, --y);
                case Direction.Right:
                    return (++x, y);
                case Direction.Left:
                    return (--x, y);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}