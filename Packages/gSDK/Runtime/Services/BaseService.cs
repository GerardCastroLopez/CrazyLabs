using System;
using System.Collections.Generic;
using gSDK.Events;
using gSDK.EventSystem;

namespace gSDK.Services
{
    public abstract class BaseService : IService, IEventHandler<ServicesRegisteredEvent>, IEventHandler<AllServicesReadyEvent>
    {
        public bool IsReady { get; private set; } = false;

        protected Locator _locator;
        protected bool _internalInitialised = false, _initialised = false;
        private readonly List<Action> _onReady = new();


        protected BaseService(bool needsAsyncInitialisation)
        {
            EventDispatcher.Register(this);

            if (!needsAsyncInitialisation)
            {
                Ready();
            }
        }

        ~BaseService()
        {
            Dispose();
        }

        public void Dispose()
        {
            EventDispatcher.Unregister(this);
        }

        public void OnReady(Action callback)
        {
            if (IsReady)
            {
                callback?.Invoke();
            }
            else
            {
                _onReady.Add(callback);
            }
        }

        protected virtual void OnServicesRegistered()
        {
        }

        protected virtual void Init()
        {
        }

        protected virtual void Ready()
        {
            IsReady = true;
            foreach (var callback in _onReady)
            {
                callback?.Invoke();
            }
        }

        internal void SetServiceLocator(Locator locator)
        {
            _locator = locator;
        }

        public void Handle(ServicesRegisteredEvent evt)
        {
            if (!_internalInitialised)
            {
                _internalInitialised = true;
                OnServicesRegistered();
            }
        }

        public void Handle(AllServicesReadyEvent evt)
        {
            if (!_initialised)
            {
                _initialised = true;
                Init();
            }
        }
    }
}