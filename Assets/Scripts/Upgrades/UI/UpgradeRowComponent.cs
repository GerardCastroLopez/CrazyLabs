using System;
using CrazyLabs.Upgrades.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrazyLabs.MainMenu.UI
{
    public sealed class UpgradeRowComponent : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameTxt, _levelTxt, _costTxt;
        [SerializeField] private Button _upgradeBtn;

        public UpgradeType UpgradeType => _definition.Type;
        
        private UpgradeDefinition _definition;
        private Action<UpgradeType> _onUpgrade;


        public void Init(UpgradeDefinition definition, Action<UpgradeType> onUpgrade)
        {
            _definition = definition;
            _onUpgrade = onUpgrade;
        }

        public void Refresh(int level, int? upgradeCost, bool canUpgrade)
        {
            _nameTxt.text = _definition.DisplayName;
            _levelTxt.text = $"Lvl. {level}/{_definition.MaxLevel}";
            _costTxt.text = !upgradeCost.HasValue ? "MAX" : upgradeCost.Value.ToString();
            _upgradeBtn.interactable = canUpgrade;
        }

        public void OnUpgradeTouch()
        {
            _onUpgrade?.Invoke(_definition.Type);
        }
    }
}
