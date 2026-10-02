using System;

namespace gSDK.UI
{
    public interface ILoadingView
    {
        void Toggle(bool toggle, bool animate, Action callback);
        void MoveToFront();
    }
}