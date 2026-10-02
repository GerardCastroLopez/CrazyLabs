using UnityEngine;

namespace CrazyLabs.Progression
{
    public enum UpgradeType
    {
        LaunchPower,
        Speed,
        Steering,
        CollectibleValue,
    }

    /// <summary>Design-time description of one persistent upgrade. Tweak the asset to rebalance.</summary>
    [CreateAssetMenu(menuName = "CrazyLabs/Upgrade Definition", fileName = "Upgrade")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        [SerializeField] UpgradeType type;
        [SerializeField] string displayName = "Upgrade";
        [SerializeField, TextArea] string description;
        [SerializeField, Min(1)] int maxLevel = 5;
        [SerializeField, Min(1)] int baseCost = 20;
        [SerializeField, Min(1f)] float costGrowth = 1.6f;
        [Tooltip("Fractional bonus granted per level, e.g. 0.12 = +12% per level.")]
        [SerializeField, Min(0f)] float bonusPerLevel = 0.12f;

        public UpgradeType Type => type;
        public string DisplayName => displayName;
        public string Description => description;
        public int MaxLevel => maxLevel;
        public float BonusPerLevel => bonusPerLevel;

        /// <summary>Price of the next level, given the level currently owned.</summary>
        public int CostForNextLevel(int currentLevel) =>
            Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowth, currentLevel));
    }
}
