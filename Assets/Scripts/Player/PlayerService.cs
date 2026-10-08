using System.Collections.Generic;
using CrazyLabs.Characters;
using CrazyLabs.SaveData;
using CrazyLabs.Upgrades;
using CrazyLabs.Upgrades.Data;
using gSDK;
using gSDK.EventSystem;
using gSDK.Services;
using UnityEngine;

namespace CrazyLabs.Player
{
    public class PlayerService : BaseService, IEventHandler<UpgradePurchasedEvent>
    {
        private const string kPlayerDataKey = "PlayerData";
        
        public IReadOnlyList<CharacterData> AllCharacters => _allCharacters;
        public readonly Reactive<CharacterData> Character = new();
        public UpgradesModule UpgradesModule { get; private set; }
        public Reactive<int> Currency => _playerData.Currency;
        
        private readonly List<CharacterData> _allCharacters = new();
        private ISaveData _saveData;
        private PlayerData _playerData;
        
        
        public PlayerService() : base(false)
        {
        }

        protected override void OnServicesRegistered()
        {
            base.OnServicesRegistered();
            
            _saveData = _locator.GetService<ISaveData>();
            
            var charConfigs = _locator.GetConfig<AllCharactersSO>();
            foreach (var character in charConfigs.Data)
            {
                _allCharacters.Add(character.Data);
            }
            
            _playerData = _saveData.Get<PlayerData>(kPlayerDataKey);

            if (_playerData == null)
            {
                _playerData = new();
                SetCharacter(0);
            }
            else
            {
                int index = _allCharacters.FindIndex(c => c.Id == _playerData.CurrentCharacterId.Value);
                SetCharacter(Mathf.Max(0, index));
            }
         
            var upgradeConfigs = _locator.GetConfig<AllUpgradeDefinitionsSO>().Data;
            var upgrades = new List<UpgradeDefinition>();
            upgradeConfigs.Foreach(c => upgrades.Add(c.Data));
            UpgradesModule = new(upgrades, _playerData);
        }

        public void ChangeCharacter(int delta)
        {
            int count = _allCharacters.Count;
            int current = _allCharacters.IndexOf(Character.Value);
            SetCharacter(((current + delta) % count + count) % count);
        }

        public void Handle(UpgradePurchasedEvent evt)
        {
            Save();
        }

        public void AddCurrency(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _playerData.Currency.Value += amount;
            Save();
        }

        private void SetCharacter(int index)
        {
            Character.Value = _allCharacters[index];
            
            _playerData.CurrentCharacterId.Value = Character.Value.Id;
            Save();
        }

        private void Save()
        {
            _saveData.Set(kPlayerDataKey, _playerData);
        }
    }
}
