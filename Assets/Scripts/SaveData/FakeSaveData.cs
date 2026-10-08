namespace CrazyLabs.SaveData
{
    public class FakeSaveData : ISaveData
    {
        public T Get<T>(string key) => default;
        public void Set<T>(string key, T value) { }
        public void Remove(string key) { }
        public void Dispose() { }
    }
}