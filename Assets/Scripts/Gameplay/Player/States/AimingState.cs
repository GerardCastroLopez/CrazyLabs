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
                var drag = inputs.DragFromPressNormalized;
                slingshot.SetPull(-drag.y / fullDrag);
                slingshot.SetAim(-drag.x / _player.Config.Controls.HorizontalDragFullFraction);
            }
            else if (inputs.ActionHeld)
            {
                slingshot.Charge(Time.deltaTime);
                slingshot.SetAim(inputs.Horizontal);
            }

            _player.Sled.SetPullback(slingshot.PullbackMeters, slingshot.PouchSideMeters, slingshot.AimHeadingRadians);
        }

        public UniTask Exit()
        {
            _player.SetTutorialVisible(false);
            return UniTask.CompletedTask;
        }
    }
}
