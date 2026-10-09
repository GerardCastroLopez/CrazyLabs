using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CrazyLabs.Characters
{
    public enum CharacterVoice
    {
        Female,
        Male,
    }

    [Serializable]
    public class CharacterData
    {
        public string Id, Name;
        public AudioClip SelectedClip;
        public AssetReferenceGameObject VisualsPrefab;
        public CharacterVoice Voice;
        public AnimationClip[] LaunchClips, CrashClips, VictoryClips;
    }
}
