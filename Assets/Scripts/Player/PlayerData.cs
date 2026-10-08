using System;
using System.Collections.Generic;
using CrazyLabs.Upgrades.Data;
using gSDK;

namespace CrazyLabs.Player
{
    [Serializable]
    public class PlayerData
    {
        public rInt Currency = new();
        public rString CurrentCharacterId = new();
        public List<UpgradeData> UpgradeLevels = new();
    }
}