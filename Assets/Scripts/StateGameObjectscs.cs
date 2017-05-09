using UnityEngine;

namespace Sarsa
{
    public class StateGameObjects
    {
        public GameObject Floor { get; set; }
        public Material FloorMaterial { get; set; }

        private GameObject[] Directions { get; } = new GameObject[4];

        public GameObject Up
        {
            get => Directions[(int) Direction.Up];
            set => Directions[(int) Direction.Up] = value;
        }

        public GameObject Down
        {
            get => Directions[(int) Direction.Down];
            set => Directions[(int) Direction.Down] = value;
        }

        public GameObject Right
        {
            get => Directions[(int) Direction.Right];
            set => Directions[(int) Direction.Right] = value;
        }

        public GameObject Left
        {
            get => Directions[(int) Direction.Left];
            set => Directions[(int) Direction.Left] = value;
        }

        public GameObject this[Direction dir]
        {
            get => Directions[(int) dir];
            set => Directions[(int) dir] = value;
        }
    }
}