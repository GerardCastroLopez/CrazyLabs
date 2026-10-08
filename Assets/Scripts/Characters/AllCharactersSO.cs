using UnityEngine;

namespace CrazyLabs.Characters
{
    [CreateAssetMenu]
    public class AllCharactersSO : ScriptableObject
    {
        public CharacterSO[] Data;
    }
}