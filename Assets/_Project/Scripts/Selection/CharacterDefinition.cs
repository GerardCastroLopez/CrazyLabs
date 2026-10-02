using UnityEngine;

namespace CrazyLabs.Selection
{
    /// <summary>Which set of reaction voices a character uses.</summary>
    public enum CharacterVoice
    {
        Female,
        Male,
    }

    /// <summary>A playable character: display name plus a ready-to-instantiate, humanoid model prefab.</summary>
    [CreateAssetMenu(menuName = "CrazyLabs/Character", fileName = "Character")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField] string displayName = "Character";
        [Tooltip("Humanoid model with an Animator (avatar assigned). The shared sled animator controller is applied at runtime.")]
        [SerializeField] GameObject modelPrefab;
        [SerializeField] CharacterVoice voice;
        [Tooltip("Played when this character is picked in the menu (the transformation sound).")]
        [SerializeField] AudioClip selectSound;

        [Header("Animation pools (one is picked at random each time)")]
        [SerializeField] AnimationClip[] launchClips;
        [SerializeField] AnimationClip[] crashClips;
        [SerializeField] AnimationClip[] victoryClips;

        public string DisplayName => displayName;
        public GameObject ModelPrefab => modelPrefab;
        public CharacterVoice Voice => voice;
        public AudioClip SelectSound => selectSound;
        public AnimationClip[] LaunchClips => launchClips;
        public AnimationClip[] CrashClips => crashClips;
        public AnimationClip[] VictoryClips => victoryClips;
    }
}
