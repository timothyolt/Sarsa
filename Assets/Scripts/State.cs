namespace Sarsa
{
    public class State
    {
        private float[] Directions { get; } = new float[4];

        public float Up
        {
            get => Directions[(int) Direction.Up];
            set => Directions[(int) Direction.Up] = value;
        }

        public float Down
        {
            get => Directions[(int) Direction.Down];
            set => Directions[(int) Direction.Down] = value;
        }

        public float Right
        {
            get => Directions[(int) Direction.Right];
            set => Directions[(int) Direction.Right] = value;
        }

        public float Left
        {
            get => Directions[(int) Direction.Left];
            set => Directions[(int) Direction.Left] = value;
        }

        public float this[Direction dir]
        {
            get => Directions[(int) dir];
            set => Directions[(int) dir] = value;
        }

        public (float, Direction) Max
        {
            get
            {
                var value = Directions[0];
                var direction = 0;
                for (var i = 1; i < Directions.Length; i++)
                    if (Directions[i] > value)
                    {
                        value = Directions[i];
                        direction = i;
                    }
                return (value, (Direction) direction);
            }
        }

        public (float, Direction) Min
        {
            get
            {
                var value = Directions[0];
                var direction = 0;
                for (var i = 1; i < Directions.Length; i++)
                    if (Directions[i] < value)
                    {
                        value = Directions[i];
                        direction = i;
                    }
                return (value, (Direction) direction);
            }
        }
    }
}