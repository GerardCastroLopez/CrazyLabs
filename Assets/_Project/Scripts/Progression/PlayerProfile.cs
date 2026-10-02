using System;
using UnityEngine;

namespace CrazyLabs.Progression
{
    /// <summary>Persistent player data: banked coins and the level of every upgrade.</summary>
    [Serializable]
    public sealed class PlayerProfile
    {
        public int coins;
        public int selectedCharacter;
        public int selectedLevel;
        public int[] upgradeLevels = new int[Enum.GetValues(typeof(UpgradeType)).Length];

        /// <summary>Keeps old saves valid when new upgrade types are added.</summary>
        public void EnsureValid()
        {
            int required = Enum.GetValues(typeof(UpgradeType)).Length;
            if (upgradeLevels == null) upgradeLevels = new int[required];
            else if (upgradeLevels.Length != required) Array.Resize(ref upgradeLevels, required);
            coins = Mathf.Max(0, coins);
            selectedCharacter = Mathf.Max(0, selectedCharacter);
            selectedLevel = Mathf.Max(0, selectedLevel);
        }
    }

    public interface IProfileStore
    {
        PlayerProfile Load();
        void Save(PlayerProfile profile);
    }

    public sealed class PlayerPrefsProfileStore : IProfileStore
    {
        public const string Key = "sledrun.profile.v1";

        public PlayerProfile Load()
        {
            var profile = new PlayerProfile();
            string json = PlayerPrefs.GetString(Key, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try { JsonUtility.FromJsonOverwrite(json, profile); }
                catch (ArgumentException) { profile = new PlayerProfile(); }
            }
            profile.EnsureValid();
            return profile;
        }

        public void Save(PlayerProfile profile)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(profile));
            PlayerPrefs.Save();
        }
    }

    public sealed class InMemoryProfileStore : IProfileStore
    {
        PlayerProfile stored = new PlayerProfile();

        public PlayerProfile Load()
        {
            var copy = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(stored));
            copy.EnsureValid();
            return copy;
        }

        public void Save(PlayerProfile profile) => stored = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(profile));
    }
}
