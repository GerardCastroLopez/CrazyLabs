using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace gSDK.Patterns.Pooling
{
    public class AddressablePool<T> : BasePool<T> where T : Component
    {
        private AsyncOperationHandle<GameObject> _prefabHandle;
        private UniTask<GameObject> _prefab;
        private Transform _poolParentTr;


        public AddressablePool(string poolId, AssetReference viewRef, int maxInstances, bool canGrow = true)
        {
            _prefabHandle = Addressables.LoadAssetAsync<GameObject>(viewRef);
            _prefab = _prefabHandle.Task.AsUniTask();
            CommonInit(poolId, maxInstances, canGrow);
        }

        public AddressablePool(string viewId, int maxInstances, bool canGrow = true)
        {
            _prefabHandle = Addressables.LoadAssetAsync<GameObject>(viewId);
            _prefab = _prefabHandle.Task.AsUniTask();
            CommonInit(viewId, maxInstances, canGrow);
        }

        private void CommonInit(string poolId, int maxInstances, bool canGrow = true)
        {
            _poolParentTr = new GameObject($"AddressablePool - {poolId}").transform;
            Init(maxInstances, canGrow);
        }

        public override void Return(T instance)
        {
            instance.gameObject.SetActive(false);
            instance.transform.parent = _poolParentTr;
            base.Return(instance);
        }

        protected override void Initialise(T instance)
        {
            instance.gameObject.SetActive(true);
        }

        protected override async UniTask<T> SpawnInstance()
        {
            await _prefab;

            var instance = Object.Instantiate(_prefab.AsTask().Result, _poolParentTr);
            instance.SetActive(false);

            return instance.GetComponent<T>();
        }

        protected override void InternalDispose(List<T> list)
        {
            foreach(var item in list)
            {
                Object.Destroy(item.gameObject);
            }

            if(_poolParentTr != null)
            {
                Object.Destroy(_poolParentTr.gameObject);
                _poolParentTr = null;
            }

            if(_prefabHandle.IsValid())
            {
                Addressables.Release(_prefabHandle);
            }
        }
    }
}