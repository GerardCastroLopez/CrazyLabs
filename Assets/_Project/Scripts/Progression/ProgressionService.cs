using System;
using System.Collections.Generic;

namespace CrazyLabs.Progression
{
    /// <summary>
    /// Owns the player's wallet and upgrade levels and persists every change.
    /// Pure C# (no scene dependencies) so it can be unit tested.
    /// </summary>
    public sealed class ProgressionService
    {
        readonly IProfileStore store;
        readonly PlayerProfile profile;

        public IReadOnlyList<UpgradeDefinition> Definitions { get; }
        public int Coins => profile.coins;
        public int SelectedCharacter => profile.selectedCharacter;
        public int SelectedLevel => profile.selectedLevel;

        public event Action Changed;
        public event Action<UpgradeDefinition> Purchased;

        public ProgressionService(IReadOnlyList<UpgradeDefinition> definitions, IProfileStore store)
        {
            Definitions = definitions;
            this.store = store;
            profile = store.Load();
        }

        public int GetLevel(UpgradeType type) => profile.upgradeLevels[(int)type];

        public int GetLevel(UpgradeDefinition definition) => GetLevel(definition.Type);

        /// <summary>Total fractional bonus currently granted by an upgrade (0 when never bought).</summary>
        public float GetBonus(UpgradeType type)
        {
            foreach (var definition in Definitions)
                if (definition.Type == type) return definition.BonusPerLevel * GetLevel(type);
            return 0f;
        }

        public bool IsMaxed(UpgradeDefinition definition) => GetLevel(definition) >= definition.MaxLevel;

        public int GetCost(UpgradeDefinition definition) => definition.CostForNextLevel(GetLevel(definition));

        public bool CanAfford(UpgradeDefinition definition) => !IsMaxed(definition) && Coins >= GetCost(definition);

        public bool TryPurchase(UpgradeDefinition definition)
        {
            if (!CanAfford(definition)) return false;

            profile.coins -= GetCost(definition);
            profile.upgradeLevels[(int)definition.Type]++;
            Commit();
            Purchased?.Invoke(definition);
            return true;
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            profile.coins += amount;
            Commit();
        }

        public void SetSelection(int character, int level)
        {
            profile.selectedCharacter = character;
            profile.selectedLevel = level;
            Commit();
        }

        void Commit()
        {
            store.Save(profile);
            Changed?.Invoke();
        }
    }
}
