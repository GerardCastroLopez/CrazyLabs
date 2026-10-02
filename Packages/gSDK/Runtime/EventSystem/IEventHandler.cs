using System;

namespace gSDK.EventSystem
{
    public interface IEventHandler : IDisposable
    {}

    public interface IEventHandler<T> : IEventHandler
    {
        void Handle(T evt);
    }
}