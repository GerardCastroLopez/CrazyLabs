using System.Collections.Generic;
using CrazyLabs.Levels.Data;
using gSDK;
using gSDK.Services;

namespace CrazyLabs.Levels
{
    public class LevelService : BaseService
    {
        public readonly Reactive<LevelData> Level = new();

        private readonly List<LevelData> _allLevels = new();
        private int _levelIndex = 0;
        
        
        public LevelService() : base(false)
        {
        }

        protected override void OnServicesRegistered()
        {
            base.OnServicesRegistered();

            var configs = _locator.GetConfig<AllLevelsSO>();
            foreach (var config in configs.Data)
            {
                _allLevels.Add(config.Data);
            }
            
            Level.Value = _allLevels[_levelIndex];
        }

        public void ChangeLevel(int delta)
        {
            int count = _allLevels.Count;
            _levelIndex = ((_levelIndex + delta) % count + count) % count;
            Level.Value = _allLevels[_levelIndex];
        }
    }
}