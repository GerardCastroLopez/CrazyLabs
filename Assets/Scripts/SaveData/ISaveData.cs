using gSDK.Services;

namespace CrazyLabs.SaveData
{
    public interface ISaveData : IService
    {
        T Get<T>(string key);
        void Set<T>(string key, T value);
        void Remove(string key);
    }
}