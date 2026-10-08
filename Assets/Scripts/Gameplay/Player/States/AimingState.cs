using Cysharp.Threading.Tasks;
using gSDK.Patterns.StateMachine;
using UnityEngine;

namespace CrazyLabs.Gameplay.Player.States
{
    public class AimingState : IState
    {
        private readonly PlayerController _player;


        public AimingState(PlayerController player)
        {
            _player = player;
        }

        public UniTask Enter()
        {
            _player.Slingshot.Reset();
            _player.SetTutorialVisible(true);
            return UniTask.CompletedTask;
        }

        public void Update()
        {
            var inputs = _player.Inputs;
            var slingshot = _player.Slingshot;

            if (inputs.PointerUp || inputs.ActionUp)
            {
                if (slingshot.Release(out float strength))
                {
                    _player.Launch(strength);
                    return;
                }
            }
            else if (inputs.PointerHeld)
            {
                float fullDrag = _player.Config.Controls.VerticalDragFullFraction;
                slingshot.SetPull(-inputs.DragFromPressNormalized.y / fullDrag);
            }
            else if (inputs.ActionHeld)
            {
                slingshot.Charge(Time.deltaTime);
            }

            _player.Sled.SetPullback(slingshot.PullbackMeters);
        }

        public UniTask Exit()
        {
            _player.SetTutorialVisible(false);
            return UniTask.CompletedTask;
        }
    }
}
