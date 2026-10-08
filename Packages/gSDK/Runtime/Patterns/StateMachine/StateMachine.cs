using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace gSDK.Patterns.StateMachine
{
    public class StateMachine
    {
        public IState Current { get; private set; }

        private Dictionary<Type, IState> _registeredStates = new();
        
        private readonly bool _enableLog;
        private readonly Stack<Type> _prevStates = new();


        public StateMachine(bool enableLog)
        {
            _enableLog = enableLog;
        }

        public void RegisterState(IState state)
        {
            var type = state.GetType();
            if (!_registeredStates.ContainsKey(type))
            {
                _registeredStates.Add(type, state);
            }
            else
            {
                Debug.LogError($"State already registered '{type.Name}'");
            }
        }

        public void RegisterStates(params IState[] states)
        {
            foreach (var state in states)
            {
                RegisterState(state);
            }
        }

        public bool ClearState()
        {
            if (Current != null)
            {
                Current.Exit().Forget();
                _prevStates.Push(Current.GetType());
                Current = null;
				
                return true;
            }
            return false;
        }

        public void ChangeState<T>(Action<T> setup = null) where T : IState
        {
            AsyncChangeState(setup).Forget();
        }

        public async UniTask AsyncChangeState<T>(Action<T> setup = null) where T : IState
        {
            await AsyncChangeState(typeof(T), setup);
        }

        public void ChangeState<T>(Type newStateType, Action<T> setup = null) where T : IState
        {
            AsyncChangeState(newStateType, setup).Forget();
        }

        public async UniTask AsyncChangeState<T>(Type newStateType, Action<T> setup = null) where T : IState
        {   
            if (!TryGetState(newStateType, out T newState))
            {
                return;
            }
            
            setup?.Invoke(newState);

            if (ClearState())
            {
                if (_enableLog)
                {
                    Debug.Log($"FSM Change from {Current.GetType().Name} to {newStateType.Name}");
                }
            }
            else if (_enableLog)
            {
                Debug.Log($"FSM Change to {newStateType.Name}");
            }

            if (_prevStates.Contains(newStateType))
            {
                while (_prevStates.Pop() != newStateType) {}
            }

            Current = newState;
            await Current.Enter();
        }
        
        public async UniTask GoBack()
        {
            if (_prevStates.Count > 0)
            {
                await AsyncChangeState<IState>(_prevStates.Peek());
            }
        }

        public void Update()
        {
            Current?.Update();
        }
        
        private bool TryGetState<T>(Type type, out T state) where T : IState
        {
            if (_registeredStates.TryGetValue(type, out var baseState))
            {
                state = (T)baseState;
                return true;
            }
            
            state = default;
            Debug.LogError($"Tried to get unregistered state '{typeof(T).Name}'");
            return false;
        }
    }
}