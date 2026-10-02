using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace gSDK.Patterns.Pooling
{
	public abstract class BasePool<T> where T : class
	{
        private readonly List<T> _list = new();
		private readonly List<T> _availableList = new();
		private int _maxInstances;
		private bool _canGrow;


		protected void Init(int maxInstances, bool canGrow = true)
		{
			_maxInstances = maxInstances;
			_canGrow = canGrow;

			SpawnInitialInstances().Forget();
		}

        public async UniTask<T> GetAsync()
		{
			if(_availableList.Count > 0)
			{
				var instance = _availableList[0];
				Initialise(instance);
				_availableList.RemoveAt(0);

				return instance;
			}

			if(_canGrow)
			{
				return await SpawnInstance(true);
			}

			Debug.LogWarning($"An instance of {typeof(T).Name} was requested but no free instances are available");
			return null;
		}

		public bool IsAvailable(T instance)
		{
			return _availableList.Contains(instance);
		}

		public virtual void Return(T instance)
		{
			if(!IsAvailable(instance))
			{
				_availableList.Add(instance);
			}
		}

		public void ReturnAllInstances()
		{
			foreach(var instance in _list)
			{
				Return(instance);
			}
		}

		public void Dispose()
		{
			InternalDispose(_list);

			_list.Clear();
			_availableList.Clear();
		}

		private async UniTask SpawnInitialInstances()
		{
			var tasksList = new List<UniTask<T>>();

			for(int i = 0; i < _maxInstances; ++i)
			{
				tasksList.Add(SpawnInstance(false));
			}

			await UniTask.WhenAll(tasksList);

			foreach(var task in tasksList)
			{
				_availableList.Add(task.AsTask().Result);
			}
		}

		private async UniTask<T> SpawnInstance(bool initialise)
		{
			var instance = await SpawnInstance();
			_list.Add(instance);

			if(initialise)
			{
				Initialise(instance);
            }

			return instance;
        }

		protected abstract UniTask<T> SpawnInstance();
		protected abstract void Initialise(T instance);
		protected abstract void InternalDispose(List<T> list);
	}
}