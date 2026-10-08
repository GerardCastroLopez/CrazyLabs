using CrazyLabs.Upgrades.Data;

namespace CrazyLabs.Upgrades
{
    public class UpgradePurchasedEvent
    {
        public readonly UpgradeType Type;


        public UpgradePurchasedEvent(UpgradeType type)
        {
            Type = type;
        }
    }
}
