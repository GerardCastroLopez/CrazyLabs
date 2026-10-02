using Cysharp.Threading.Tasks;

namespace gSDK.Patterns.StateMachine
{
	public interface IState
	{
		UniTask Enter();
		void Update();
		UniTask Exit();
	}
}