using UnityEngine;

namespace CrazyLabs.Upgrades.Data
{
    [CreateAssetMenu]
    public class AllUpgradeDefinitionsSO : ScriptableObject
    {
        public UpgradeDefinitionSO[] Data;
    }
}