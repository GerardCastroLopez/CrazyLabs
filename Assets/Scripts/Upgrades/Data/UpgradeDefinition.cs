using System;
using UnityEngine;

namespace CrazyLabs.Upgrades.Data
{
    public enum UpgradeType
    {
        LaunchPower,
        Speed,
        Steering,
        CollectibleValue,
    }
    
    [Serializable]
    public class UpgradeDefinition
    {
        public UpgradeType Type;
        public string DisplayName = "Upgrade";
        [TextArea]
        public string Description;
        public int MaxLevel = 5;
        public int BaseCost = 20;
        public float CostGrowth = 1.6f;
        [Tooltip("Fractional bonus granted per level, e.g. 0.12 = +12% per level.")]
        public float BonusPerLevel = 0.12f;
    }
}