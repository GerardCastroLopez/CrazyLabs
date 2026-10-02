using CrazyLabs.Track;
using UnityEngine;

namespace CrazyLabs.Selection
{
    /// <summary>A level theme: slope layout, ground look, atmosphere and the props that populate it.</summary>
    [CreateAssetMenu(menuName = "CrazyLabs/Level", fileName = "Level")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField] string displayName = "Level";

        [Header("Slope")]
        [SerializeField] TrackLayout layout;
        [SerializeField] Material trackMaterial;
        [SerializeField] Material surroundMaterial;

        [Header("Atmosphere")]
        [SerializeField] Color skyColor = new Color(0.78f, 0.88f, 0.98f);
        [SerializeField] Color ambientColor = new Color(0.62f, 0.68f, 0.78f);
        [SerializeField] Color sunColor = new Color(1f, 0.96f, 0.88f);
        [SerializeField] float fogStart = 70f;
        [SerializeField] float fogEnd = 230f;

        [Header("Props")]
        [Tooltip("Hitting one of these ends the run.")]
        [SerializeField] GameObject[] crashObstacles;
        [Tooltip("Hitting one of these sheds speed.")]
        [SerializeField] GameObject[] slowObstacles;
        [SerializeField] GameObject[] scenery;

        [Header("Ambient")]
        [Tooltip("Optional looping particle effect that follows the player (e.g. falling snow).")]
        [SerializeField] GameObject ambientEffect;

        public string DisplayName => displayName;
        public TrackLayout Layout => layout;
        public Material TrackMaterial => trackMaterial;
        public Material SurroundMaterial => surroundMaterial;
        public Color SkyColor => skyColor;
        public Color AmbientColor => ambientColor;
        public Color SunColor => sunColor;
        public float FogStart => fogStart;
        public float FogEnd => fogEnd;
        public GameObject[] CrashObstacles => crashObstacles;
        public GameObject[] SlowObstacles => slowObstacles;
        public GameObject[] Scenery => scenery;
        public GameObject AmbientEffect => ambientEffect;
    }
}
