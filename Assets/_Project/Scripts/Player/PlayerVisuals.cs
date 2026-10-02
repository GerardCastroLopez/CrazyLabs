using CrazyLabs.Core;
using UnityEngine;

namespace CrazyLabs.Player
{
    /// <summary>
    /// Drives the character animation and body lean from game flow events. The shared animator
    /// controller has placeholder clips for launch, crash and victory; an override controller swaps
    /// in a random clip from the selected character's own pool each time.
    /// </summary>
    public sealed class PlayerVisuals : MonoBehaviour
    {
        static readonly int IdleTrigger = Animator.StringToHash("Idle");
        static readonly int LaunchTrigger = Animator.StringToHash("Launch");
        static readonly int CrashTrigger = Animator.StringToHash("Crash");
        static readonly int VictoryTrigger = Animator.StringToHash("Victory");

        [SerializeField] GameFlow flow;
        [SerializeField] SledMotor sled;
        [Tooltip("Shared controller (idle / launch / ride / crash / victory) applied to whichever character is selected.")]
        [SerializeField] RuntimeAnimatorController controller;
        [Tooltip("The clips used by the controller's launch/crash/victory states; they are the keys for per-character overrides.")]
        [SerializeField] AnimationClip launchPlaceholder;
        [SerializeField] AnimationClip crashPlaceholder;
        [SerializeField] AnimationClip victoryPlaceholder;
        [SerializeField] Transform leanRoot;
        [SerializeField] float maxLeanDegrees = 22f;

        Animator animator;
        AnimatorOverrideController overrides;
        GameObject model;
        int lastLaunch = -1;
        int lastCrash = -1;
        int lastVictory = -1;

        void OnEnable()
        {
            flow.SelectionChanged += OnSelectionChanged;
            flow.StateChanged += OnStateChanged;
            flow.Launched += OnLaunched;
            flow.RunEnded += OnRunEnded;
        }

        void OnDisable()
        {
            flow.SelectionChanged -= OnSelectionChanged;
            flow.StateChanged -= OnStateChanged;
            flow.Launched -= OnLaunched;
            flow.RunEnded -= OnRunEnded;
        }

        void LateUpdate()
        {
            if (leanRoot == null) return;
            var target = Quaternion.Euler(0f, 0f, -sled.TurnNormalized * maxLeanDegrees);
            leanRoot.localRotation = Quaternion.Slerp(leanRoot.localRotation, target, Time.deltaTime * 8f);
        }

        void OnSelectionChanged()
        {
            var prefab = flow.Character.ModelPrefab;
            if (prefab == null) return;

            if (model != null)
            {
                model.SetActive(false);
                Destroy(model);
            }

            model = Instantiate(prefab, leanRoot);
            animator = model.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                overrides = new AnimatorOverrideController(controller);
                animator.runtimeAnimatorController = overrides;
                animator.applyRootMotion = false;
            }
            OnStateChanged(flow.State);
        }

        void OnStateChanged(GameState state)
        {
            if (state == GameState.Menu || state == GameState.Aiming) Trigger(IdleTrigger);
        }

        void OnLaunched()
        {
            Override(launchPlaceholder, flow.Character.LaunchClips, ref lastLaunch);
            Trigger(LaunchTrigger); // the controller moves on to the ride state when the clip ends
        }

        void OnRunEnded(RunResult result)
        {
            if (result.IsWin)
            {
                Override(victoryPlaceholder, flow.Character.VictoryClips, ref lastVictory);
                Trigger(VictoryTrigger);
            }
            else
            {
                Override(crashPlaceholder, flow.Character.CrashClips, ref lastCrash);
                Trigger(CrashTrigger);
            }
        }

        /// <summary>Swaps a random clip from the character's pool into a placeholder slot (no repeats in a row).</summary>
        void Override(AnimationClip placeholder, AnimationClip[] pool, ref int last)
        {
            if (overrides == null || placeholder == null || pool == null || pool.Length == 0) return;

            last = RandomPick.Index(pool.Length, last);
            overrides[placeholder] = pool[last];
        }

        void Trigger(int hash)
        {
            if (animator == null) return;
            animator.ResetTrigger(IdleTrigger);
            animator.ResetTrigger(LaunchTrigger);
            animator.ResetTrigger(CrashTrigger);
            animator.ResetTrigger(VictoryTrigger);
            animator.SetTrigger(hash);
        }
    }
}
