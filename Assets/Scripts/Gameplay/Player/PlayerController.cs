using CrazyLabs.Characters;
using CrazyLabs.Gameplay.Config;
using CrazyLabs.Gameplay.Controls;
using CrazyLabs.Gameplay.Events;
using CrazyLabs.Gameplay.Modules;
using CrazyLabs.Gameplay.Player.States;
using CrazyLabs.Gameplay.Sled;
using CrazyLabs.Gameplay.Track;
using CrazyLabs.Player;
using Cysharp.Threading.Tasks;
using gSDK.EventSystem;
using gSDK.MVC;
using gSDK.Patterns.StateMachine;
using UnityEngine;
using SledStats = CrazyLabs.Gameplay.Sled.SledStats;

namespace CrazyLabs.Gameplay.Player
{
    public class PlayerController : ViewController<PlayerView>
    {
        private const float kMetersPerSecondToKmh = 1f;
        private const float kMinTrackLength = 1f;

        private readonly GameplayController _gameplay;
        private readonly PlayerService _playerService;
        private readonly StateMachine _stateMachine = new(false);

        private SledStats _stats;
        private bool _runActive;

        internal readonly GameplayConfigSO Config;
        internal readonly InputModule Inputs;
        internal readonly SledModule Sled = new();
        internal readonly SlingshotModule Slingshot;

        internal CharacterData Character => _playerService.Character.Value;
        internal float SpeedNormalized => Sled.SpeedNormalized;
        internal bool CanPause => _stateMachine.Current is AimingState or RunningState;
        internal bool IsAiming => _stateMachine.Current is AimingState;
        internal Vector3 PouchPosition => Sled.Position + Vector3.up * Config.Slingshot.PouchHeight;


        public PlayerController(GameplayController gameplay, GameplayConfigSO config, PlayerService playerService) : base("Prefabs/PlayerView", false)
        {
            _gameplay = gameplay;
            Config = config;
            _playerService = playerService;

            Inputs = new(config.Controls, gameplay.ActiveDevice);
            Slingshot = new(config.Slingshot, config.Controls);


            _stateMachine.RegisterStates(new AimingState(this), new RunningState(this), new ResultState(this));
        }

        public override async UniTask CacheView()
        {
            await base.CacheView();
            await RefreshVisuals();
        }

        internal async UniTask RefreshVisuals()
        {
            if (View)
            {
                await View.LoadVisuals(Character);
            }
        }

        internal void Tick(float deltaTime)
        {
            Inputs.Tick(deltaTime);
            _stateMachine.Update();

            _gameplay.Speed.Value = Mathf.Round(Mathf.Max(0f, Sled.Speed - Config.Sled.StallSpeed) * kMetersPerSecondToKmh);
            _gameplay.Distance.Value = Mathf.Round(Sled.Distance);

            View?.Tick(deltaTime);
        }

        internal void StartRun(TrackProfile profile)
        {
            _stats = new SledStats(Config.Sled, _playerService.UpgradesModule);
            Sled.Prepare(_stats, profile, Config.Slingshot.StartZ);
            Slingshot.Reset();

            _runActive = false;
            View.Tick(0f);
            View.PlayIdle();
            _stateMachine.ChangeState<AimingState>();
        }

        internal void Launch(float pullStrength)
        {
            _runActive = true;
            Sled.Launch(_stats.LaunchSpeedForPull(pullStrength));
            float animationSeconds = View.PlayLaunch();
            View.Bump(Config.RideFeel.LaunchKick);
            EventDispatcher.Raise(new RunLaunchedEvent(animationSeconds));
            _stateMachine.ChangeState<RunningState>();
        }

        internal void SetTutorialVisible(bool visible)
        {
            _gameplay.ShowTutorial.Value = visible;
        }

        internal void BankRewards()
        {
            _playerService.AddCurrency(_gameplay.Currency.Value);
        }

        internal void OnRunEnded(RunResult result)
        {
            if (result.IsWin)
            {
                View.PlayVictory();
            }
            else
            {
                View.PlayCrash();
            }

            EventDispatcher.Raise(new RunEndedEvent(result, Character));
            _gameplay.OnRunEnded(result);
        }

        internal void OnTrigger(Collider other)
        {
            if (!_runActive)
            {
                return;
            }

            if (other.TryGetComponent(out Collectible collectible))
            {
                int value = Mathf.Max(1, Mathf.RoundToInt(collectible.BaseValue * _stats.CoinMultiplier));
                collectible.Collect();
                _gameplay.Currency.Value += value;
                EventDispatcher.Raise(new CollectibleCollectedEvent(value, collectible.transform.position));
            }
            else if (other.TryGetComponent(out Obstacle obstacle))
            {
                obstacle.Consume();
                EventDispatcher.Raise(new ObstacleHitEvent(obstacle));
                View.Bump(obstacle.Kind == ObstacleKind.Crash ? Config.RideFeel.CrashBumpKick : Config.RideFeel.SlowBumpKick);

                if (obstacle.Kind == ObstacleKind.Crash)
                {
                    EndRun(eRunOutcome.Crashed);
                }
                else
                {
                    Sled.ApplySlow(obstacle.SpeedKept);
                }
            }
            else if (other.TryGetComponent(out FinishLine _))
            {
                EndRun(eRunOutcome.ReachedFinish);
            }
        }

        internal void OnStalled()
        {
            if (_runActive)
            {
                EndRun(eRunOutcome.LostMomentum);
            }
        }

        private void EndRun(eRunOutcome outcome)
        {
            _runActive = false;

            float distance = Sled.Distance;
            float progress = Mathf.Clamp01(distance / Mathf.Max(kMinTrackLength, _gameplay.Level.TrackLength));
            var result = new RunResult(outcome, distance, progress, _gameplay.Currency.Value);

            _stateMachine.ChangeState<ResultState>(state => state.Result = result);
        }
    }
}
