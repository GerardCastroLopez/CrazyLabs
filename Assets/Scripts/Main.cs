using CrazyLabs.Gameplay;
using CrazyLabs.Levels;
using CrazyLabs.MainMenu;
using CrazyLabs.Player;
using CrazyLabs.SaveData;
using CrazyLabs.UI;
using gSDK.Services;
using gSDK.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CrazyLabs
{
    public class Main : MonoBehaviour
    {
        [SerializeField] private RectTransform _uiRoot;
        [SerializeField] private LoadingView _loading;
        [SerializeField] private AssetReference _popupBackgroundRef;
        [SerializeField] private ScriptableObject[] _configs;
        
        
        void Awake()
        {
            var locator = new Locator();

            foreach (var config in _configs)
            {
                locator.RegisterConfig(config);
            }

            // basic services
            locator.RegisterService(new UIService(_uiRoot, _loading));
            locator.RegisterService(new PopupService(_popupBackgroundRef));
            locator.RegisterService<ISaveData>(new DefaultSaveData());
            
            // game-specific services
            locator.RegisterService(new PlayerService());
            locator.RegisterService(new MainMenuService());
            locator.RegisterService(new LevelService());
            locator.RegisterService(new GameplayService());
            
            locator.AllServicesRegistered();
        }
    }
}