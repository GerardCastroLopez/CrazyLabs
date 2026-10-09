using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace gSDK.Patterns.Pooling
{
    public class ComponentPool<T> : BasePool<T> where T : Component
    {
        protected T _prefab;
        private Transform _poolParentTr;


        public void Init(T prefab, int maxInstances, bool canGrow = true)
	    {
            _prefab = prefab;
            _prefab.gameObject.SetActive(false);
            _poolParentTr = new GameObject($"ComponentPool - {_prefab.name}").transform;
		    
            Init(maxInstances, canGrow);
	    }

        public override void Return(T instance)
        {
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(_poolParentTr, false);
            base.Return(instance);
        }

        protected override void Initialise(T instance)
        {
			instance.gameObject.SetActive(true);
        }

        protected override UniTask<T> SpawnInstance()
        {
            var instance = Object.Instantiate(_prefab, _poolParentTr);
            instance.gameObject.SetActive(false);

            return UniTask.FromResult(instance);
        }

        protected override void InternalDispose(List<T> list)
        {
            list.Foreach(item => {
                
                if (item && item.gameObject)
                {
                    Object.Destroy(item.gameObject);
                }
            });

            if (_poolParentTr)
            {
                Object.Destroy(_poolParentTr.gameObject);
                _poolParentTr = null;
            }
        }
    }
}