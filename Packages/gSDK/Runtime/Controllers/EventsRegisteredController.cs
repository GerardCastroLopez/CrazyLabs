using gSDK.EventSystem;

namespace gSDK.Controllers
{
    public abstract class EventsRegisteredController : IEventHandler
    {
        protected EventsRegisteredController()
        {
            EventDispatcher.Register(this);
        }
        
        ~EventsRegisteredController()
        {
            Dispose();
        }

        public void Dispose()
        {
            EventDispatcher.Unregister(this);
        }
    }
}