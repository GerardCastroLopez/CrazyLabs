using CrazyLabs.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace CrazyLabs.UI
{
    /// <summary>One line of the upgrade list: name, level, price and a buy button.</summary>
    public sealed class UpgradeRowView : MonoBehaviour
    {
        [SerializeField] Text nameText;
        [SerializeField] Text levelText;
        [SerializeField] Text costText;
        [SerializeField] Button buyButton;

        UpgradeDefinition definition;
        ProgressionService progression;

        public void Bind(UpgradeDefinition upgrade, ProgressionService service)
        {
            Unbind();
            definition = upgrade;
            progression = service;
            progression.Changed += Refresh;
            buyButton.onClick.AddListener(OnBuyClicked);
            Refresh();
        }

        void OnDestroy() => Unbind();

        void Unbind()
        {
            if (progression == null) return;
            progression.Changed -= Refresh;
            buyButton.onClick.RemoveListener(OnBuyClicked);
        }

        void OnBuyClicked() => progression.TryPurchase(definition);

        void Refresh()
        {
            int level = progression.GetLevel(definition);
            bool maxed = progression.IsMaxed(definition);

            nameText.text = definition.DisplayName;
            levelText.text = $"Lv {level}/{definition.MaxLevel}";
            costText.text = maxed ? "MAX" : progression.GetCost(definition).ToString();
            buyButton.interactable = progression.CanAfford(definition);
        }
    }
}
