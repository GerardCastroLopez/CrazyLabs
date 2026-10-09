using Cysharp.Threading.Tasks;
using gSDK.Patterns.StateMachine;
using UnityEngine;

namespace CrazyLabs.Gameplay.Player.States
{
    public class ResultState : IState
    {
        private readonly PlayerController _player;

        public RunResult Result { get; set; }


        public ResultState(PlayerController player)
        {
            _player = player;
        }

        public UniTask Enter()
        {
            if (Result.Outcome == eRunOutcome.Crashed)
            {
                _player.Sled.StopImmediately();
            }
            else
            {
                _player.Sled.Stop();
            }

            _player.BankRewards();
            _player.OnRunEnded(Result);
            return UniTask.CompletedTask;
        }

        public void Update()
        {
            _player.Sled.Tick(Time.deltaTime, 0f);
        }

        public UniTask Exit()
        {
            return UniTask.CompletedTask;
        }
    }
}
