using System;
using System.Collections.Generic;
using CrazyLabs.Player;
using CrazyLabs.Progression;
using CrazyLabs.Selection;
using CrazyLabs.Track;
using UnityEngine;

namespace CrazyLabs.Core
{
    /// <summary>
    /// Orchestrates the run loop: Menu -> Aiming (slingshot) -> Running -> Result -> (retry) Aiming.
    /// Everything else (UI, audio, animation) observes the events exposed here.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameFlow : MonoBehaviour
    {
        [SerializeField] SledTuning tuning;
        [SerializeField] UpgradeDefinition[] upgrades;
        [SerializeField] CharacterDefinition[] characters;
        [SerializeField] LevelDefinition[] levels;
        [SerializeField] LevelAtmosphere atmosphere;
        [SerializeField] SledMotor sled;
        [SerializeField] PlayerSensor sensor;
        [SerializeField] SlingshotLauncher slingshot;
        [SerializeField] TrackBuilder track;
        [SerializeField] FollowCamera followCamera;
        [Tooltip("Z position (meters from the top of the slope) where the sled starts.")]
        [SerializeField] float startZ = 5f;
        [Tooltip("How far ahead of the start position the slingshot posts stand.")]
        [SerializeField] float slingshotForwardOffset = 1.2f;

        ProgressionService progression;
        SledStats stats;

        public GameState State { get; private set; }
        public RunSession Session { get; private set; } = new RunSession();
        public SledMotor Sled => sled;
        public IReadOnlyList<CharacterDefinition> Characters => characters;
        public IReadOnlyList<LevelDefinition> Levels => levels;
        public int CharacterIndex => Mathf.Clamp(Progression.SelectedCharacter, 0, characters.Length - 1);
        public int LevelIndex => Mathf.Clamp(Progression.SelectedLevel, 0, levels.Length - 1);
        public CharacterDefinition Character => characters[CharacterIndex];
        public LevelDefinition Level => levels[LevelIndex];

        public float TrackLength => track.Profile != null ? track.Profile.FinishZ - startZ : 1f;

        /// <summary>Wallet and upgrades. Created lazily so UI can bind before Start runs.</summary>
        public ProgressionService Progression =>
            progression ??= new ProgressionService(upgrades, new PlayerPrefsProfileStore());

        public event Action<GameState> StateChanged;
        /// <summary>Raised when the chosen character or level changes (and once at startup).</summary>
        public event Action SelectionChanged;
        /// <summary>Raised only when the player picks a different character from the menu.</summary>
        public event Action CharacterPicked;
        public event Action Launched;
        public event Action<int, Vector3> CollectibleCollected;
        public event Action<Obstacle> ObstacleHit;
        public event Action<RunResult> RunEnded;

        void OnEnable()
        {
            slingshot.Fired += OnSlingshotFired;
            sensor.CollectibleTouched += OnCollectibleTouched;
            sensor.ObstacleTouched += OnObstacleTouched;
            sensor.FinishTouched += OnFinishTouched;
            sled.Stalled += OnStalled;
        }

        void OnDisable()
        {
            slingshot.Fired -= OnSlingshotFired;
            sensor.CollectibleTouched -= OnCollectibleTouched;
            sensor.ObstacleTouched -= OnObstacleTouched;
            sensor.FinishTouched -= OnFinishTouched;
            sled.Stalled -= OnStalled;
        }

        void Start()
        {
            PrepareRun();
            SetState(GameState.Menu);
            SelectionChanged?.Invoke();
        }

        // ---------- commands (called by the UI) ----------

        /// <summary>Menu "Launch" button: the slingshot becomes active.</summary>
        public void BeginAiming()
        {
            if (State != GameState.Menu) return;
            slingshot.Begin();
            SetState(GameState.Aiming);
        }

        public void SelectCharacter(int index)
        {
            if (State != GameState.Menu) return;
            Progression.SetSelection(Mathf.Clamp(index, 0, characters.Length - 1), LevelIndex);
            SelectionChanged?.Invoke();
            CharacterPicked?.Invoke();
        }

        public void SelectLevel(int index)
        {
            if (State != GameState.Menu) return;
            Progression.SetSelection(CharacterIndex, Mathf.Clamp(index, 0, levels.Length - 1));
            PrepareRun();
            SelectionChanged?.Invoke();
        }

        /// <summary>Result "Menu" button: back to the start screen to change character or level.</summary>
        public void ReturnToMenu()
        {
            if (State != GameState.Result) return;
            PrepareRun();
            SetState(GameState.Menu);
        }

        /// <summary>Result "Retry" button: new track, upgrades kept, straight back to the slingshot.</summary>
        public void Retry()
        {
            if (State != GameState.Result) return;
            PrepareRun();
            slingshot.Begin();
            SetState(GameState.Aiming);
        }

        // ---------- run lifecycle ----------

        void PrepareRun()
        {
            Session = new RunSession();
            stats = SledStats.Create(tuning, Progression);

            atmosphere.Apply(Level);
            track.Build(UnityEngine.Random.Range(int.MinValue, int.MaxValue), Level);
            sled.Prepare(stats, track.Profile, startZ);

            var slope = Quaternion.Euler(track.Profile.PitchAt(startZ) * Mathf.Rad2Deg, 0f, 0f);
            slingshot.PlaceAt(new Vector3(0f, track.Profile.HeightAt(startZ + slingshotForwardOffset), startZ + slingshotForwardOffset), slope);
            followCamera.Snap();
        }

        void OnSlingshotFired(float pull)
        {
            if (State != GameState.Aiming) return;
            slingshot.End();
            sled.Launch(stats.LaunchSpeedForPull(pull));
            SetState(GameState.Running);
            Launched?.Invoke();
        }

        void OnCollectibleTouched(Collectible collectible)
        {
            if (State != GameState.Running) return;

            int value = Mathf.Max(1, Mathf.RoundToInt(collectible.BaseValue * stats.CoinMultiplier));
            collectible.Collect();
            Session.AddCoins(value);
            CollectibleCollected?.Invoke(value, collectible.transform.position);
        }

        void OnObstacleTouched(Obstacle obstacle)
        {
            if (State != GameState.Running) return;

            obstacle.Consume();
            ObstacleHit?.Invoke(obstacle);

            if (obstacle.Kind == ObstacleKind.Crash) EndRun(RunOutcome.Crashed);
            else sled.ApplySlow(obstacle.SpeedKept);
        }

        void OnFinishTouched()
        {
            if (State == GameState.Running) EndRun(RunOutcome.ReachedFinish);
        }

        void OnStalled()
        {
            if (State == GameState.Running) EndRun(RunOutcome.LostMomentum);
        }

        void EndRun(RunOutcome outcome)
        {
            sled.Stop();
            Progression.AddCoins(Session.Coins);

            float distance = sled.Distance;
            var result = new RunResult(outcome, distance, Mathf.Clamp01(distance / TrackLength), Session.Coins);

            SetState(GameState.Result);
            RunEnded?.Invoke(result);
        }

        void SetState(GameState next)
        {
            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
