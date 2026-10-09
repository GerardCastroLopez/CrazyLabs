using CrazyLabs.Levels.Data;
using Cysharp.Threading.Tasks;
using gSDK;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CrazyLabs.Gameplay.Modules
{
    public class AtmosphereModule
    {
        private readonly Transform _followTarget;
        private readonly Vector3 _effectOffset;
        private GameObject _ambientEffect;
        private bool _disposed;

        
        public AtmosphereModule(Camera cam, Light sun, Transform followTarget, Vector3 effectOffset, LevelData level, Transform parent)
        {
            _followTarget = followTarget;
            _effectOffset = effectOffset;
            
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = level.SkyColor;
            RenderSettings.fogStartDistance = level.FogStart;
            RenderSettings.fogEndDistance = level.FogEnd;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = level.AmbientColor;

            cam.backgroundColor = level.SkyColor;
            sun.color = level.SunColor;
            
            LoadAmbientEffect(level.AmbientEffect, parent).Forget();
        }

        public void Dispose()
        {
            _disposed = true;
            AddressableInstance.ReleaseOrDestroy(_ambientEffect);
        }

        private async UniTaskVoid LoadAmbientEffect(AssetReferenceGameObject reference, Transform parent)
        {
            if (reference == null || !reference.RuntimeKeyIsValid())
            {
                return;
            }

            try
            {
                var instance = await AddressableInstance.Instantiate(reference.RuntimeKey, parent);

                if (_disposed)
                {
                    AddressableInstance.ReleaseOrDestroy(instance);
                    return;
                }

                _ambientEffect = instance;
                Tick();
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"AtmosphereModule: couldn't load the ambient effect. {exception.Message}");
            }
        }

        public void Tick()
        {
            if (_ambientEffect != null && _followTarget != null)
                _ambientEffect.transform.position = _followTarget.position + _effectOffset;
        }
    }
}