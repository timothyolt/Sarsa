﻿using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Sarsa
{
	public class Gridworld : MonoBehaviour
	{
		private const int Size = 20;
		private static int[,] _reward;
		private static State[,] _q;
		private static State[,] _eligibility;
		private static int _actorX;
		private static int _actorY;

		//private static Thread _trainingThread;
		//private static CancellationTokenSource _trainingCancel;
		//private static int _seed = Environment.TickCount;

		private const float LearnRate = 0.1f;
		private const float DiscountFactor = 0.9f;
		private const float EligibilityFactor = 0.9f;
		private const float EpsilonDecay = 0.001f;
		private const float EpsilonFloor = 0.05f;
		private static float _epsilon = 0.9f;

		[SerializeField] private GameObject _floorPrefab, _actorPrefab;
		[SerializeField] private Color _ineligibleColor, _eligibleColor, _rewardColor;
		[SerializeField] private GameObject _upPrefab, _downPrefab, _rightPrefab, _leftPrefab;

		private StateGameObjects[,] _stateGameObjectses;
		private GameObject _actor;

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
			//_trainingCancel = new CancellationTokenSource();
			//_trainingThread = new Thread(Train);
			//_trainingThread.Start();
		}

		private void Update()
		{
			Debug.Log($"Epsilon: {_epsilon}");
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
				stateGameObjects.Up.transform.localScale = new Vector3(q.Up, q.Up, 1);
				stateGameObjects.Down.transform.localScale = new Vector3(q.Down, q.Down, 1);
				stateGameObjects.Right.transform.localScale = new Vector3(q.Right, q.Right, 1);
				stateGameObjects.Left.transform.localScale = new Vector3(q.Left, q.Left, 1);
			}
			_actor.transform.position = new Vector3(_actorX, _actorY, 0);
		}

		private static IEnumerator Train()
		{
			var random = new System.Random(); // new System.Random(Interlocked.Increment(ref _seed));
			//var token = _trainingCancel.Token;
			// Episodes
			while (true) // (!token.IsCancellationRequested)
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
				while (true) // (!token.IsCancellationRequested)
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
							actorYPrime--;
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
					// Observe reward
					var reward = actorXPrime < 0 || actorXPrime >= Size || actorYPrime < 0 || actorYPrime >= Size
						? -1
						: _reward[actorXPrime, actorYPrime];
					// Determine next action
					var actionPrime = random.NextDouble() < _epsilon
						? (Direction) random.Next(0, 4)
						: _q[actorXPrime, actorYPrime].Max.Item2;
					// Determine delta
					var delta = reward + DiscountFactor * _q[actorXPrime, actorYPrime][actionPrime] - _q[_actorX, _actorY][action];
					// Modify eligibility
					_eligibility[_actorX, _actorY][action]++;
					// Update tables
					for (var x = 0; x < Size; x++)
					for (var y = 0; y < Size; y++)
					for (var d = 0; d < 4; d++)
					{
						_q[x, y][(Direction) d] += LearnRate * delta * _eligibility[x, y][(Direction) d];
						_eligibility[x, y][(Direction) d] *= EligibilityFactor * DiscountFactor;
					}
					// Check for terminal state
					if (reward != 0)
						break;
					// Move state and actions forward
					_actorX = actorXPrime;
					_actorY = actorYPrime;
					action = actionPrime;
					yield return null;
				}
				// Move towards exploitation
				_epsilon = Math.Max(EpsilonFloor, _epsilon - EpsilonDecay);
			}
		}

		private void OnDestroy()
		{
			//_trainingCancel?.Cancel();
		}
	}
}