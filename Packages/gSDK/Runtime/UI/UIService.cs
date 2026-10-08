using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using gSDK.Services;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

namespace gSDK.UI
{
    public class UIService : BaseService
    {
        private readonly Transform _uiRoot, _blockingInput;
        private readonly ILoadingView _loading;
        private readonly Dictionary<int, RectTransform> _layers = new();
        private readonly List<object> _blockRequesters = new();
        private readonly Dictionary<Type, GameObject> _loadedSingleInstances = new();
        

        public UIService(Transform uiRoot, ILoadingView loading) : base(false)
        {
            _uiRoot = uiRoot;
            _loading = loading;

            GameObject.DontDestroyOnLoad(_uiRoot);

            ToggleLoading(true, false);

            // blocking layer
            _blockingInput = new GameObject($"Blocking Input", typeof(RectTransform), typeof(GraphicRaycaster)).transform;
            _blockingInput.SetParent(_uiRoot, false);
            _blockingInput.SetFullScreen();
            _blockingInput.gameObject.AddComponent<Image>().SetAlpha(0f);
            _blockingInput.gameObject.SetActive(false);
        }

        public void BlockTouch(bool value, object requester)
        {
            if (value)
            {
                _blockRequesters.AddIfMissing(requester);
            }
            else
            {
                _blockRequesters.Remove(requester);
            }

            _blockingInput.SetAsLastSibling();
            _blockingInput.gameObject.SetActive(!_blockRequesters.IsEmpty());
        }

        public void ToggleLoading(bool show, bool animate, Action callback = null)
        {
            _loading?.Toggle(show, animate, callback);
        }
        
        public UniTask AsyncToggleLoading(bool show, bool animate)
        {
            return _loading?.AsyncToggle(show, animate) ?? UniTask.CompletedTask;
        }

        public async UniTask<GameObject> Show(object viewRef, int layerIndex, [CanBeNull] Type singleInstanceType)
        {
            var layerTr = GetLayer(layerIndex);
            
            if (singleInstanceType != null && _loadedSingleInstances.TryGetValue(singleInstanceType, out var instance))
            {
                if (instance)
                {
                    instance.transform.SetParent(layerTr, false);
                    instance.transform.SetAsLastSibling();
                    return instance;
                }

                _loadedSingleInstances.Remove(singleInstanceType);
            }
            
            instance = await AddressableInstance.Instantiate(viewRef, layerTr);
            
            if (singleInstanceType != null)
            {
                _loadedSingleInstances.Add(singleInstanceType, instance);
            }
            
            return instance;
        }

        public void ReleaseSingleInstances()
        {
            foreach (var instance in _loadedSingleInstances.Values)
            {
                AddressableInstance.ReleaseOrDestroy(instance);
            }
            _loadedSingleInstances.Clear();
        }

        protected override void OnDispose()
        {
            ReleaseSingleInstances();
        }

        public void SetLayer(RectTransform rect, int layerIndex)
        {
            rect.SetParent(GetLayer(layerIndex));
        }

        private RectTransform GetLayer(int layerIndex)
        {
            if (_layers.ContainsKey(layerIndex))
            {
                return _layers[layerIndex];
            }

            if (layerIndex < 0)
            {
                Debug.LogWarning("UI Layers can't have negative values, defaulting to 0");
                layerIndex = 0;
            }

            var layer = new GameObject($"Layer_{layerIndex}", typeof(GraphicRaycaster)).AddOrGetComponent<RectTransform>();

            layer.SetParent(_uiRoot, false);
            layer.SetFullScreen();
            _layers.Add(layerIndex, layer);

            foreach (var kvp in _layers)
            {
                kvp.Value.SetSiblingIndex(kvp.Key);
            }

            _loading.MoveToFront();

            return layer;
        }
    }
}