namespace Sarsa
{
    public class State
    {
        private float[] Data { get; } = new float[4];

        public float Up
        {
            get => Data[(int) Direction.Up];
            set => Data[(int) Direction.Up] = value;
        }

        public float Down
        {
            get => Data[(int) Direction.Down];
            set => Data[(int) Direction.Down] = value;
        }

        public float Right
        {
            get => Data[(int) Direction.Right];
            set => Data[(int) Direction.Right] = value;
        }

        public float Left
        {
            get => Data[(int) Direction.Left];
            set => Data[(int) Direction.Left] = value;
        }

        public float this[Direction dir]
        {
            get => Data[(int) dir];
            set => Data[(int) dir] = value;
        }

        public (float, Direction) Max
        {
            get
            {
                var value = Data[0];
                var direction = 0;
                for (var i = 1; i < Data.Length; i++)
                    if (Data[i] > value)
                    {
                        value = Data[i];
                        direction = i;
                    }
                return (value, (Direction) direction);
            }
        }

        public (float, Direction) Min
        {
            get
            {
                var value = Data[0];
                var direction = 0;
                for (var i = 1; i < Data.Length; i++)
                    if (Data[i] < value)
                    {
                        value = Data[i];
                        direction = i;
                    }
                return (value, (Direction) direction);
            }
        }
    }
}