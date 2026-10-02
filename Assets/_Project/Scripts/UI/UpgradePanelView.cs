using CrazyLabs.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace CrazyLabs.UI
{
    /// <summary>List of upgrade rows plus the wallet balance. Reused by the menu and the result screen.</summary>
    public sealed class UpgradePanelView : MonoBehaviour
    {
        [SerializeField] UpgradeRowView[] rows;
        [SerializeField] Text coinsText;

        ProgressionService progression;

        public void Bind(ProgressionService service)
        {
            if (progression != null) progression.Changed -= RefreshCoins;
            progression = service;
            progression.Changed += RefreshCoins;

            for (int i = 0; i < rows.Length; i++)
            {
                bool hasDefinition = i < service.Definitions.Count;
                rows[i].gameObject.SetActive(hasDefinition);
                if (hasDefinition) rows[i].Bind(service.Definitions[i], service);
            }
            RefreshCoins();
        }

        void OnDestroy()
        {
            if (progression != null) progression.Changed -= RefreshCoins;
        }

        void RefreshCoins() => coinsText.text = progression.Coins.ToString();
    }
}
