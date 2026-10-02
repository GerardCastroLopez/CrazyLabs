using System;
using System.Collections.Generic;

namespace gSDK.EventSystem
{
    public static class EventDispatcher
    {
        private static Dictionary<Type, List<IEventHandler>> _handlersDict = new Dictionary<Type, List<IEventHandler>>();


        public static void Register(IEventHandler handler)
        {
            ForeachValidType(handler, evtType => {

                if(!_handlersDict.ContainsKey(evtType))
                {
                    _handlersDict.Add(evtType, new List<IEventHandler> { handler });
                }
                else
                {
                    _handlersDict[evtType].Add(handler);
                }
            });
        }

        public static void Unregister(IEventHandler handler)
        {
            ForeachValidType(handler, evtType => {

                if(_handlersDict.ContainsKey(evtType))
                {
                    var list = _handlersDict[evtType];

                    if(list.Contains(handler))
                    {
                        list.Remove(handler);

                        if(list.Count == 0)
                        {
                            _handlersDict.Remove(evtType);
                        }
                    }
                }
            });
        }

        public static void Raise<T>(T evt)
        {
            var type = typeof(T);
            if(_handlersDict.ContainsKey(type))
            {
                var handlers = _handlersDict[type];
                
                for(int i = handlers.Count -1; i >= 0; --i)
                {
                    var handler = (IEventHandler<T>)handlers[i];

                    if(handler != null)
                    {
                        handler.Handle(evt);
                    }
                    else
                    {
                        handlers.RemoveAt(i);
                    }
                }

                if(handlers.Count == 0)
                {
                    _handlersDict.Remove(type);
                }
            }
        }

        private static void ForeachValidType(IEventHandler handler, Action<Type> callback)
        {
            var interfaces = handler.GetType().GetInterfaces();
            var handlerType = typeof(IEventHandler<>);

            foreach(var i in interfaces)
            {
                if(i.IsGenericType && i.GetGenericTypeDefinition() == handlerType)
                {
                    callback(i.GetGenericArguments()[0]);
                }
            }
        }
    }
}