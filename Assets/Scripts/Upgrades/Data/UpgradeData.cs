using System;
using gSDK;

namespace CrazyLabs.Upgrades.Data
{
    [Serializable]
    public class UpgradeData
    {
        public UpgradeType Type;
        public rInt Level = new(0);
    }
}