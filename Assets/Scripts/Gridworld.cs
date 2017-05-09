using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Sarsa
{
	public class Gridworld : MonoBehaviour
	{
		private const int Size = 20;
		private int[,] _reward;
		private State[,] _q;
		private State[,] _eligibility;
		private int _actorX;
		private int _actorY;

		private const float LearnRate = 0.1f;
		private const float DiscountFactor = 0.9f;
		private const float EligibilityFactor = 0.9f;
		private const float EpsilonDecay = 0.0001f;
		private const float EpsilonFloor = 0.05f;
		private float _epsilon = 0.9f;

		[SerializeField] private GameObject _floorPrefab, _actorPrefab;
		[SerializeField] private Color _ineligibleColor, _eligibleColor, _rewardColor;
		[SerializeField] private GameObject _upPrefab, _downPrefab, _rightPrefab, _leftPrefab;

		private StateGameObjects[,] _stateGameObjectses;
		private GameObject _actor;

		private int _epochYield = 1;
		private bool _stepTime;
		private bool _breakTraining;
		private bool _renderMaxArrows;
		private bool _normalizeArrows;

		private void Start() {
			_reward = new int[Size,Size];
			_q = new State[Size,Size];
			_eligibility = new State[Size,Size];
			_stateGameObjectses = new StateGameObjects[Size,Size];
			// initialization
			var rewardX = Random.Range(0, Size);
			var rewardY = Random.Range(0, Size);
			_reward[rewardX, rewardY] = 1;
			for (var x = 0; x < Size; x++)
			for (var y = 0; y < Size; y++)
			{
				// Data
				const float rangeLow = 0.001f;
				const float rangeHigh = 0.01f;
				_q[x,y] = new State
				{
					Up = Random.Range(rangeLow, rangeHigh),
					Down = Random.Range(rangeLow, rangeHigh),
					Right = Random.Range(rangeLow, rangeHigh),
					Left = Random.Range(rangeLow, rangeHigh)
				};
				_eligibility[x,y] = new State();
				// GameObjects
				var statePos = new Vector3(x, y, 0);
				var statePosOffset = statePos + new Vector3(0.5f, 0.5f, 0);
				var floor = Instantiate(_floorPrefab, statePos, Quaternion.identity, transform);
				var flooro = floor.GetComponent<Floor>();
				var render = flooro.Mesh.GetComponent<Renderer>();
				var mat = render.material;
				_stateGameObjectses[x, y] = new StateGameObjects
				{
					Floor = floor,
					FloorMaterial = mat,
					Up = Instantiate(_upPrefab, statePosOffset, Quaternion.identity, transform),
					Down = Instantiate(_downPrefab, statePosOffset, Quaternion.identity, transform),
					Right = Instantiate(_rightPrefab, statePosOffset, Quaternion.identity, transform),
					Left = Instantiate(_leftPrefab, statePosOffset, Quaternion.identity, transform)
				};
			}
			_actorX = Random.Range(0, Size);
			_actorY = Random.Range(0, Size);
			_actor = Instantiate(_actorPrefab);
			StartCoroutine(Train());
		}

		private void Update()
		{
			// Number of epochs between updates
			if (Input.GetKeyDown(KeyCode.Alpha0))
				_epochYield = 10;
			else if (Input.GetKeyDown(KeyCode.Alpha9))
				_epochYield = 9;
			else if (Input.GetKeyDown(KeyCode.Alpha8))
				_epochYield = 8;
			else if (Input.GetKeyDown(KeyCode.Alpha7))
				_epochYield = 7;
			else if (Input.GetKeyDown(KeyCode.Alpha6))
				_epochYield = 6;
			else if (Input.GetKeyDown(KeyCode.Alpha5))
				_epochYield = 5;
			else if (Input.GetKeyDown(KeyCode.Alpha4))
				_epochYield = 4;
			else if (Input.GetKeyDown(KeyCode.Alpha3))
				_epochYield = 3;
			else if (Input.GetKeyDown(KeyCode.Alpha2))
				_epochYield = 2;
			else if (Input.GetKeyDown(KeyCode.Alpha1))
				_epochYield = 1;
			// Whether to animate every step
			if (Input.GetKeyDown(KeyCode.Space))
				_stepTime = !_stepTime;
			// Kill the training loop
			if (Input.GetKeyDown(KeyCode.Escape))
				_breakTraining = true;
			// Render only the max arrows
			if (Input.GetKeyDown(KeyCode.Z))
				_renderMaxArrows = !_renderMaxArrows;
			// Normalize arrows to max = 1, min = 0
			if (Input.GetKeyDown(KeyCode.X))
				_normalizeArrows = !_normalizeArrows;
			// Render scene
			for (var x = 0; x < Size; x++)
			for (var y = 0; y < Size; y++)
			{
				var stateGameObjects = _stateGameObjectses[x, y];
				// Floor color
				if (_reward[x, y] == 1)
					stateGameObjects.FloorMaterial.color = _rewardColor;
				else
				{
					// Find highest eligibility
					var eligibility = _eligibility[x, y].Max.Item1;
					var ineligibility = 1-eligibility;
					// Blend colors
					stateGameObjects.FloorMaterial.color = new Color
					{
						r = _eligibleColor.r * eligibility + _ineligibleColor.r * ineligibility,
						g = _eligibleColor.g * eligibility + _ineligibleColor.g * ineligibility,
						b = _eligibleColor.b * eligibility + _ineligibleColor.b * ineligibility,
						a = _eligibleColor.a * eligibility + _ineligibleColor.a * ineligibility
					};
				}
				// Arrow scale
				var q = _q[x, y];
				if (_renderMaxArrows)
				{
					(var value, var direction) = q.Max;
					if (_normalizeArrows)
						value = 1;
					stateGameObjects[direction].transform.localScale = new Vector3(value, value, 1);
					for (var d = (Direction) 0; d < (Direction) 4; d++)
						if (d != direction)
							stateGameObjects[d].transform.localScale = Vector3.zero;
				}
				else if (_normalizeArrows)
				{
					(var minValue, var minDirection) = q.Min;
					(var maxValue, var maxDirection) = q.Max;
					var multiplier = 1 / (maxValue - minValue);
					for (var d = (Direction) 0; d < (Direction) 4; d++)
						stateGameObjects[d].transform.localScale = new Vector3((q[d] - minValue) * multiplier, (q[d] - minValue) * multiplier, 1);
				}
				else
					for (var d = (Direction) 0; d < (Direction) 4; d++)
						stateGameObjects[d].transform.localScale = new Vector3(q[d], q[d], 1);
			}
			_actor.transform.position = new Vector3(_actorX, _actorY, 0);
		}

		private IEnumerator Train()
		{
			var random = new System.Random();
			for (var epoch = 0; epoch < int.MaxValue; epoch++)
			{
				// Initialize eligibility table
				for (var x = 0; x < Size; x++)
				for (var y = 0; y < Size; y++)
					_eligibility[x,y] = new State();
				// Initialize state and action
				_actorX = random.Next(0, Size);
				_actorY = random.Next(0, Size);
				var action = (Direction) random.Next(0, 4);
				// Steps
				for (var step = 0; step < int.MaxValue; step++)
				{
					// Take action
					var actorXPrime = _actorX;
					var actorYPrime = _actorY;
					switch (action)
					{
						case Direction.Up:
							actorYPrime++;
							break;
						case Direction.Down:
							actorYPrime--;
							break;
						case Direction.Right:
							actorXPrime++;
							break;
						case Direction.Left:
							actorXPrime--;
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
					// Observe reward
					var reward = actorXPrime < 0 || actorXPrime >= Size || actorYPrime < 0 || actorYPrime >= Size
						? -1
						: _reward[actorXPrime, actorYPrime];
					// Determine next action
					var actionPrime = random.NextDouble() < _epsilon || reward < 0
						? (Direction) random.Next(0, 4)
						: _q[actorXPrime, actorYPrime].Max.Item2;
					// Determine delta
					var delta = reward + DiscountFactor * (reward < 0 ? 0 : _q[actorXPrime, actorYPrime][actionPrime]) - _q[_actorX, _actorY][action];
					// Modify eligibility
					_eligibility[_actorX, _actorY][action]++;
					// Update tables
					for (var x = 0; x < Size; x++)
					for (var y = 0; y < Size; y++)
					for (var d = (Direction) 0; d < (Direction) 4; d++)
					{
						_q[x, y][d] += LearnRate * delta * _eligibility[x, y][d];
						_eligibility[x, y][d] *= EligibilityFactor * DiscountFactor;
					}
					// Check for terminal state
					if (reward != 0)
						break;
					// Move state and actions forward
					_actorX = actorXPrime;
					_actorY = actorYPrime;
					action = actionPrime;
					// Draw each step
					if (_stepTime)
						yield return null;
				}
				if (_breakTraining)
					break;
				if (epoch % _epochYield == 0)
					yield return null;
				// Move towards exploitation
				_epsilon = Math.Max(EpsilonFloor, _epsilon - EpsilonDecay);
			}
		}
	}
}
