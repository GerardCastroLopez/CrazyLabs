using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace gSDK
{
    public static class AddressableInstance
    {
        private static readonly Dictionary<int, AsyncOperationHandle<GameObject>> _handles = new();


        public static async UniTask<GameObject> Instantiate(object key, Transform parent)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(key);
            var prefab = await handle.Task.AsUniTask();
            var instance = Object.Instantiate(prefab, parent);

            _handles.Add(instance.GetInstanceID(), handle);
            return instance;
        }

        public static void ReleaseOrDestroy(GameObject instance)
        {
            if (!instance)
            {
                return;
            }

            int id = instance.GetInstanceID();
            Object.Destroy(instance);

            if (_handles.Remove(id, out var handle))
            {
                Addressables.Release(handle);
            }
        }
    }
}
