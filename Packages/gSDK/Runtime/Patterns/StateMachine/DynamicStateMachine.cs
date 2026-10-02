using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace gSDK.Patterns.StateMachine
{
	public class DynamicStateMachine
	{
		public IState Current { get; private set; }

		private readonly bool _enableLog;
		private readonly Stack<IState> _prevStates = new();


		public DynamicStateMachine(bool enableLog)
		{
			_enableLog = enableLog;
		}

		public bool ClearState()
		{
			if (Current != null)
			{
				Current.Exit().Forget();
				_prevStates.Push(Current);
				Current = null;
				
				return true;
			}
			return false;
		}
		
		public async UniTask ChangeState(IState newState)
		{
			if (ClearState())
			{
				if (_enableLog)
				{
					Debug.Log($"FSM Change from {Current.GetType().Name} to {newState.GetType().Name}");
				}
			}
			else if (_enableLog)
			{
				Debug.Log($"FSM Change to {newState.GetType().Name}");
			}

			if (_prevStates.Contains(newState))
			{
				while (_prevStates.Pop() != newState) {}
			}

			Current = newState;
			await Current.Enter();
		}

		public async UniTask GoBack()
		{
			if (_prevStates.Count > 0)
			{
				await ChangeState(_prevStates.Peek());
			}
		}

		public void Update()
		{
			Current?.Update();
		}
	}
}