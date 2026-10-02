using System;
using System.Collections.Generic;
using UnityEngine;
using gSDK.Events;
using gSDK.EventSystem;

namespace gSDK.Services
{
    public class Locator
    {
        private Dictionary<Type, object> _configsDict = new();
        private Dictionary<Type, IService> _servicesDict = new();
        private List<BaseService> _baseServices = new();
        private bool _allRegistered = false;


        public void Teardown()
        {
            foreach (var service in _servicesDict.Values)
            {
                service.Dispose();
            }
            
            _configsDict.Clear();
            _servicesDict.Clear();
            _baseServices.Clear();
            _allRegistered = false;
        }

#region services
        public T RegisterService<T>(T service) where T : IService
        {
            UnregisterService<T>();

            _servicesDict.Add(typeof(T), service);

            if(service is BaseService baseService)
            {
                baseService.SetServiceLocator(this);
                _baseServices.Add(baseService);
                baseService.OnReady(OnServiceReady);
            }

            return service;
        }

        public T GetService<T>() where T : IService
        {
            var type = typeof(T);
            if(_servicesDict.TryGetValue(type, out var service))
            {
                return (T)service;
            }
            Debug.LogError($"No service defined for type {type}");
            return default;
        }

        public void UnregisterService<T>() where T : IService
        {
            var type = typeof(T);
            if(_servicesDict.ContainsKey(type))
            {
                _servicesDict.Remove(type);
            }
        }

        public void AllServicesRegistered()
        {
            _allRegistered = true;

            EventDispatcher.Raise(new ServicesRegisteredEvent());

            // maybe we have no delayed services
            OnServiceReady();
        }
#endregion // services

#region configs

        public T RegisterConfig<T>(T config)
        {
            UnregisterConfig(config);
            
            _configsDict.Add(config.GetType(), config);
            
            return config;
        }

        public T GetConfig<T>()
        {
            var type = typeof(T);
            
            if (_configsDict.TryGetValue(type, out var value))
            {
                return (T)value;
            }
            Debug.LogError($"No config defined for type {type}");
            return default;
        }

        public void UnregisterConfig(object config)
        {
            var type = config.GetType();

            if (_configsDict.ContainsKey(type))
            {
                _configsDict.Remove(type);
            }
        }

#endregion

        private void OnServiceReady()
        {
            if (_allRegistered && _baseServices.Find(s => !s.IsReady) == null)
            {
                EventDispatcher.Raise(new AllServicesReadyEvent());
            }
        }
    }
}