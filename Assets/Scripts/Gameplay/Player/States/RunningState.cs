using Cysharp.Threading.Tasks;
using gSDK.Patterns.StateMachine;
using UnityEngine;

namespace CrazyLabs.Gameplay.Player.States
{
    public class RunningState : IState
    {
        private readonly PlayerController _player;


        public RunningState(PlayerController player)
        {
            _player = player;
        }

        public UniTask Enter()
        {
            return UniTask.CompletedTask;
        }

        public void Update()
        {
            var inputs = _player.Inputs;

            float steer = inputs.PointerHeld
                ? Mathf.Clamp(inputs.DragFromPressNormalized.x / _player.Config.Controls.HorizontalDragFullFraction, -1f, 1f)
                : inputs.Horizontal;

            _player.Sled.Tick(Time.deltaTime, steer);

            if (_player.Sled.HasStalled)
            {
                _player.OnStalled();
            }
        }

        public UniTask Exit()
        {
            return UniTask.CompletedTask;
        }
    }
}
