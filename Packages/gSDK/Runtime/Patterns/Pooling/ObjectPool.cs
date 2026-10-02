using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace gSDK.Patterns.Pooling
{
    public class ObjectPool<T> : BasePool<T> where T : class
    {
        private Func<T> _instantiator;
        private Action<T> _initialise, _return;
        private Action<List<T>> _dispose;


        public ObjectPool(Func<T> instantiator, int maxInstances, bool canGrow = true)
        {
            _instantiator = instantiator;
            Init(maxInstances, canGrow);
        }

        public void SetInitialise(Action<T> initialiseCallback)
        {
            _initialise = initialiseCallback;
        }

        public void SetDispose(Action<List<T>> disposeCallback)
        {
            _dispose = disposeCallback;
        }

        public void SetReturn(Action<T> returnCallback)
        {
            _return = returnCallback;
        }

        public override void Return(T instance)
        {
            if(!IsAvailable(instance))
            {
                _return?.Invoke(instance);
                base.Return(instance);
            }
        }

        protected override void Initialise(T instance)
        {
            _initialise?.Invoke(instance);
        }

        protected override UniTask<T> SpawnInstance()
        {
            return UniTask.FromResult(_instantiator());
        }

        protected override void InternalDispose(List<T> list)
        {
            _dispose?.Invoke(list);
        }
    }
}