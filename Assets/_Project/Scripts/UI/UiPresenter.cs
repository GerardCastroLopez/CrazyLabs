using CrazyLabs.Core;
using System.Linq;
using CrazyLabs.Player;
using UnityEngine;

namespace CrazyLabs.UI
{
    /// <summary>Shows the right screen for the current game state and forwards button clicks to the flow.</summary>
    public sealed class UiPresenter : MonoBehaviour
    {
        [SerializeField] GameFlow flow;
        [SerializeField] MenuView menu;
        [SerializeField] HudView hud;
        [SerializeField] ResultView result;
        [SerializeField] AudioFeedback audioFeedback;

        void Start()
        {
            menu.Upgrades.Bind(flow.Progression);
            result.Upgrades.Bind(flow.Progression);
            menu.CharacterSelector.Bind(flow.Characters.Select(c => c.DisplayName).ToList(), flow.CharacterIndex, flow.SelectCharacter);
            menu.LevelSelector.Bind(flow.Levels.Select(l => l.DisplayName).ToList(), flow.LevelIndex, flow.SelectLevel);
            ShowFor(flow.State);
        }

        void OnEnable()
        {
            flow.StateChanged += ShowFor;
            flow.RunEnded += result.Show;
            menu.LaunchClicked += OnLaunchClicked;
            result.RetryClicked += OnRetryClicked;
            result.MenuClicked += OnMenuClicked;
        }

        void OnDisable()
        {
            flow.StateChanged -= ShowFor;
            flow.RunEnded -= result.Show;
            menu.LaunchClicked -= OnLaunchClicked;
            result.RetryClicked -= OnRetryClicked;
            result.MenuClicked -= OnMenuClicked;
        }

        void OnLaunchClicked()
        {
            if (audioFeedback != null) audioFeedback.PlayUiClick();
            flow.BeginAiming();
        }

        void OnRetryClicked()
        {
            if (audioFeedback != null) audioFeedback.PlayUiClick();
            flow.Retry();
        }

        void OnMenuClicked()
        {
            if (audioFeedback != null) audioFeedback.PlayUiClick();
            flow.ReturnToMenu();
        }

        void ShowFor(GameState state)
        {
            menu.SetVisible(state == GameState.Menu);
            hud.SetVisible(state == GameState.Aiming || state == GameState.Running || state == GameState.Result);
            if (state != GameState.Result) result.Hide();
        }
    }
}
