using System;
using System.Collections;
using UnityEngine;

namespace CrazyLabs.SaveData
{
    public class DefaultSaveData : ISaveData
    {
        [Serializable]
        public class Container<T>
        {
            public T Data;
        }

        private const int kTrue = 1;
        private const int kFalse = 0;


        public T Get<T>(string key)
        {
            var type = typeof(T);
            if (type.IsValueType)
            {
                object value;
                if (type == typeof(int))
                {
                    value = PlayerPrefs.GetInt(key);
                }
                else if (type == typeof(float))
                {
                    value = PlayerPrefs.GetFloat(key);
                }
                else if (type == typeof(bool))
                {
                    value = PlayerPrefs.GetInt(key) == kTrue;
                }
                else
                {
                    value = PlayerPrefs.GetString(key);
                }
                return (T)value;
            }

            string json = PlayerPrefs.GetString(key);
            if (!json.IsNullOrEmpty())
            {
                if (typeof(IList).IsAssignableFrom(type) || typeof(IDictionary).IsAssignableFrom(type))
                {
                    return JsonUtility.FromJson<Container<T>>(json).Data;
                }
                
                return JsonUtility.FromJson<T>(json);
            }
            return default;
        }

        public void Set<T>(string key, T value)
        {
            var type = typeof(T);
            if (type.IsValueType)
            {
                object v = value;
                if (type == typeof(int))
                {
                    PlayerPrefs.SetInt(key, (int)v);
                }
                else if (type == typeof(float))
                {
                    PlayerPrefs.SetFloat(key, (float)v);
                }
                else if (type == typeof(bool))
                {
                    bool b = (bool)v;
                    PlayerPrefs.SetInt(key, b ? kTrue : kFalse);
                }
                else
                {
                    PlayerPrefs.SetString(key, (string)v);
                }
                return;
            }

            string json;
            if (typeof(IList).IsAssignableFrom(type) || typeof(IDictionary).IsAssignableFrom(type))
            {
                json = JsonUtility.ToJson(new Container<T> { Data = value });
            }
            else
            {
                json = JsonUtility.ToJson(value);
            }
            PlayerPrefs.SetString(key, json);
        }

        public void Remove(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }

        public void Dispose()
        {}
    }
}