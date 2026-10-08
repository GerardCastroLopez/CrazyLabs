using UnityEngine;

namespace CrazyLabs.Characters
{
    [CreateAssetMenu]
    public class CharacterSO : ScriptableObject
    {
        public CharacterData Data;


        private void OnValidate()
        {
            Data.Id = name;
        }
    }
}