using CrazyLabs.Levels.Data;
using UnityEngine;

namespace CrazyLabs.Gameplay.Modules
{
    public class AtmosphereModule
    {
        private readonly Transform _followTarget;
        private readonly Vector3 _effectOffset;
        private readonly GameObject _ambientEffect;

        
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
            
            if (level.AmbientEffect != null)
            {
                var origin = followTarget != null ? followTarget.position + effectOffset : effectOffset;
                _ambientEffect = Object.Instantiate(level.AmbientEffect, origin, Quaternion.identity, parent);
            }
        }

        public void Tick()
        {
            if (_ambientEffect != null && _followTarget != null)
                _ambientEffect.transform.position = _followTarget.position + _effectOffset;
        }
    }
}