using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Sarsa
{
	public class Gridworld : MonoBehaviour
	{
		[SerializeField] private GameObject _floorPrefab, _actorPrefab;
		[SerializeField] private Color _ineligibleColor, _eligibleColor, _rewardColor, _punishColor, _obstacleColor;
		[SerializeField] private GameObject _upPrefab, _downPrefab, _rightPrefab, _leftPrefab;
		[SerializeField] private Text _text;

		private const int Size = 20;
		private int[,] _rewards;
		private bool[,] _obstacles;
		private State[,] _q;
		private State[,] _eligibility;
		private int _actorX;
		private int _actorY;

		[SerializeField] private float _learnRate = 0.05f, _discountFactor = 0.9f, _eligibilityFactor = 0.95f;
		[SerializeField] private double _epsilon = 0.75, _epsilonFloor = 0.0001, _epsilonDecay = 0.00001;

		private StateGameObjects[,] _stateGameObjectses;
		private GameObject _actor;

		[SerializeField] private int _epochYield = 1;
		[SerializeField] private bool _stepTime, _breakTraining, _renderMaxArrows, _normalizeArrows;
		private int _epoch, _step;

		private void Start() {
			_rewards = new int[Size,Size];
			_q = new State[Size,Size];
			_eligibility = new State[Size,Size];
			_obstacles = new bool[Size,Size];
			_stateGameObjectses = new StateGameObjects[Size,Size];
			// initialization
			_rewards[Random.Range(0, Size), Random.Range(0, Size)] = -1;
			_rewards[Random.Range(0, Size), Random.Range(0, Size)] = 1;
			for (var i = 0; i < 10; i++)
				_obstacles[Random.Range(0, Size), Random.Range(0, Size)] = true;
			for (var x = 0; x < Size; x++)
			for (var y = 0; y < Size; y++)
			{
				// Data
				const float rangeLow = 0.0001f;
				const float rangeHigh = 0.001f;
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
			// Strip Q-table of edges
//			for (var i = 0; i < Size; i++)
//			{
//				_q[i, 0].Left = 0;
//				_q[i, Size - 1].Right = 0;
//				_q[0, i].Down = 0;
//				_q[0, Size - 1].Up = 0;
//			}
			// Strip Q-table of obstacles
			for (var x = 0; x < Size; x++)
			for (var y = 0; y < Size; y++)
				if (_obstacles[x, y])
				{
					if (x > 0)
						_q[x - 1, y].Right = 0;
					if (x < Size - 1)
						_q[x + 1, y].Left = 0;
					if (y > 0)
						_q[x, y - 1].Up = 0;
					if (y < Size - 1)
						_q[x, y + 1].Down = 0;
				}
			// Place actor
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
			// Seek policy
			// Epsilon controls
			if (Input.GetKeyDown(KeyCode.DownArrow))
				_epsilon = Math.Max(0, _epsilon - 0.01);
			if (Input.GetKeyDown(KeyCode.DownArrow))
				_epsilon = Math.Min(1, _epsilon + 0.01);
			// Update text info
			_text.text = $"Epsilon: {_epsilon}\n" +
			             $"Epoch: {_epoch}\n" +
			             $"Step: {_step}\n" +
			             $"Epochs/Frame: {_epochYield} [1-10 Keys]\n" +
			             $"Render steps: {_stepTime} [Space Key]\n" +
			             $"Render only max arrows: {_renderMaxArrows} [Z Key]\n" +
			             $"Normalize arrows: {_normalizeArrows} [X Key]";
			// Update grid info
			for (var x = 0; x < Size; x++)
			for (var y = 0; y < Size; y++)
			{
				var stateGameObjects = _stateGameObjectses[x, y];
				// Floor color
				if (_rewards[x, y] > 0)
					stateGameObjects.FloorMaterial.color = _rewardColor;
				else if (_rewards[x, y] < 0)
					stateGameObjects.FloorMaterial.color = _punishColor;
				else if (_obstacles[x, y])
					stateGameObjects.FloorMaterial.color = _obstacleColor;
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
					(var minValue, var _) = q.Min;
					(var maxValue, var _) = q.Max;
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
			for (_epoch = 0; _epoch < int.MaxValue; _epoch++)
			{
				// Initialize eligibility table
				for (var x = 0; x < Size; x++)
				for (var y = 0; y < Size; y++)
					_eligibility[x,y] = new State();
				// Initialize state and action
				do
				{
					_actorX = random.Next(0, Size);
					_actorY = random.Next(0, Size);
				} while (_rewards[_actorX, _actorY] != 0 && !_obstacles[_actorX, _actorY]);
				var action = (Direction) random.Next(0, 4);
				// Steps
				for (_step = 0; _step < int.MaxValue; _step++)
				{
					// Take action
					(var actorXPrime, var actorYPrime) = action.Act(_actorX, _actorY);
					// Observe reward
					var outOfBounds = actorXPrime < 0 || actorXPrime >= Size || actorYPrime < 0 || actorYPrime >= Size || _obstacles[actorXPrime, actorYPrime];
					var reward = outOfBounds ? 0 : _rewards[actorXPrime, actorYPrime];
					// Determine next action
					var actionPrime = outOfBounds || random.NextDouble() < _epsilon
						? (Direction) random.Next(0, 4)
						: _epoch % 2 == 0
							? _q[actorXPrime, actorYPrime].Max.Item2
							: _q[actorXPrime, actorYPrime].Min.Item2;
					// Determine delta
					var delta = reward + _discountFactor * (outOfBounds ? 0 : _q[actorXPrime, actorYPrime][actionPrime]) - _q[_actorX, _actorY][action];
					// Modify eligibility
					_eligibility[_actorX, _actorY][action]++;
					if (!outOfBounds && reward == 0)
						_eligibility[actorXPrime, actorYPrime][action.Opposite()]--;
					// Update tables
					for (var x = 0; x < Size; x++)
					for (var y = 0; y < Size; y++)
					for (var d = (Direction) 0; d < (Direction) 4; d++)
					{
						_q[x, y][d] += _learnRate * delta * _eligibility[x, y][d];
						_eligibility[x, y][d] *= _eligibilityFactor * _discountFactor;
					}
					// Check for terminal state
					if (outOfBounds || reward != 0)
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
				if (_epoch % _epochYield == 0)
					yield return null;
				// Move towards exploitation
				_epsilon = Math.Max(_epsilonFloor, _epsilon - _epsilonDecay);
			}
		}
	}
}
