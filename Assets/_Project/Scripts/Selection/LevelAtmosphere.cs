using UnityEngine;

namespace CrazyLabs.Selection
{
    /// <summary>Applies a level's sky, fog and light colours to the scene.</summary>
    public sealed class LevelAtmosphere : MonoBehaviour
    {
        [SerializeField] Camera sceneCamera;
        [SerializeField] Light sun;
        [Tooltip("The ambient effect is kept at this offset from the target.")]
        [SerializeField] Transform followTarget;
        [SerializeField] Vector3 effectOffset = new Vector3(0f, 10f, 10f);

        GameObject ambientEffect;

        public void Apply(LevelDefinition level)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = level.SkyColor;
            RenderSettings.fogStartDistance = level.FogStart;
            RenderSettings.fogEndDistance = level.FogEnd;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = level.AmbientColor;

            if (sceneCamera != null)
            {
                sceneCamera.clearFlags = CameraClearFlags.SolidColor;
                sceneCamera.backgroundColor = level.SkyColor;
            }
            if (sun != null) sun.color = level.SunColor;

            ApplyAmbientEffect(level);
        }

        void LateUpdate()
        {
            if (ambientEffect != null && followTarget != null)
                ambientEffect.transform.position = followTarget.position + effectOffset;
        }

        void ApplyAmbientEffect(LevelDefinition level)
        {
            if (ambientEffect != null) Destroy(ambientEffect);
            ambientEffect = null;
            if (level.AmbientEffect == null) return;

            Vector3 origin = followTarget != null ? followTarget.position + effectOffset : effectOffset;
            ambientEffect = Instantiate(level.AmbientEffect, origin, Quaternion.identity, transform);
        }
    }
}
