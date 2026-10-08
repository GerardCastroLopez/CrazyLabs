using UnityEngine;

namespace CrazyLabs.Levels.Data
{
    [CreateAssetMenu]
    public class AllLevelsSO : ScriptableObject
    {
        public LevelSO[] Data;
    }
}