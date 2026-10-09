using System.Collections.Generic;
using CrazyLabs.Player;
using CrazyLabs.Upgrades.Data;
using gSDK.EventSystem;
using UnityEngine;

namespace CrazyLabs.Upgrades
{
    public class UpgradesModule
    {
        public readonly List<UpgradeDefinition> Upgrades;
        
        private readonly PlayerData _playerData;

        
        public UpgradesModule(List<UpgradeDefinition> upgrades, PlayerData playerData)
        {
            Upgrades = upgrades;
            _playerData = playerData;
        }

        public bool TryUpgrade(UpgradeType upgradeType)
        {
            int? cost = GetUpgradeCost(upgradeType);
            if (!cost.HasValue || _playerData.Currency.Value < cost.Value)
            {
                return false;
            }

            _playerData.Currency.Value -= cost.Value;
                _playerData.UpgradeLevels.GetOrAdd(u => u.Type == upgradeType, () => new() { Type = upgradeType }).Level.Value++;
            
            EventDispatcher.Raise(new UpgradePurchasedEvent(upgradeType));
            
            return true;
        }

        public int GetUpgradeLevel(UpgradeType type)
        {
            foreach (var kvp in _playerData.UpgradeLevels)
            {
                if (kvp.Type == type)
                {
                    return kvp.Level.Value;
                }
            }
            return 0;
        }

        public bool CanUpgrade(UpgradeType type)
        {
            int? cost = GetUpgradeCost(type);
            return cost.HasValue && _playerData.Currency.Value >= cost.Value;
        }

        public int? GetUpgradeCost(UpgradeType type)
        {
            var definition = GetDefinition(type);
            if (definition == null)
            {
                return null;
            }

            int level = GetUpgradeLevel(type);
            if (level >= definition.MaxLevel)
            {
                return null;
            }

            return Mathf.RoundToInt(definition.BaseCost * Mathf.Pow(definition.CostGrowth, level));
        }

        public float GetBonus(UpgradeType type)
        {
            var definition = GetDefinition(type);
            return definition == null ? 0f : definition.BonusPerLevel * GetUpgradeLevel(type);
        }

        private UpgradeDefinition GetDefinition(UpgradeType type)
        {
            return Upgrades.Find(u => u.Type == type);
        }
    }
}
