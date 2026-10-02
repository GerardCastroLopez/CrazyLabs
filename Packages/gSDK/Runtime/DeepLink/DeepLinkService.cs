using System;
using System.Collections.Generic;
using UnityEngine;
using gSDK.Services;

namespace gSDK.DeepLink
{
    public class DeepLinkService : IService
    {
        private Dictionary<string, Action<object>> _links = new();


        public void Register(string link, Action<object> callback)
        {
            if (_links.ContainsKey(link))
            {
                Debug.LogWarning($"Tried to register existing link {link}");
            }
            else
            {
                _links.Add(link, callback);
            }
        }

        public void Unregister(string link)
        {
            if (_links.ContainsKey(link))
            {
                _links.Remove(link);
            }
            else
            {
                Debug.LogWarning($"Tried to unregister a non-existing link {link}");
            }
        }

        public void Call(string link, object @params = null)
        {
            if (_links.ContainsKey(link))
            {
                var callback = _links[link];

                if(callback != null)
                {
                    callback(@params);
                    return;
                }

                _links.Remove(link);
            }

            Debug.LogWarning($"There's no registered link '{link}'");
        }

        public void Dispose()
        {
            _links.Clear();
        }
    }
}