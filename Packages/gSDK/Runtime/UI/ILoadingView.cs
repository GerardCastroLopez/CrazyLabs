using System;
using Cysharp.Threading.Tasks;

namespace gSDK.UI
{
    public interface ILoadingView
    {
        void Toggle(bool toggle, bool animate, Action callback = null);
        UniTask AsyncToggle(bool toggle, bool animate);
        void MoveToFront();
    }
}