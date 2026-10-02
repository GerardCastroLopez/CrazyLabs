namespace CrazyLabs.Core
{
    public enum GameState
    {
        Menu,
        Aiming,
        Running,
        Result,
    }

    public enum RunOutcome
    {
        Crashed,
        LostMomentum,
        ReachedFinish,
    }

    /// <summary>Summary of a finished run, handed to the UI and the audio/visual feedback.</summary>
    public readonly struct RunResult
    {
        public readonly RunOutcome Outcome;
        public readonly float DistanceMeters;
        public readonly float Progress01;
        public readonly int CoinsCollected;

        public RunResult(RunOutcome outcome, float distanceMeters, float progress01, int coinsCollected)
        {
            Outcome = outcome;
            DistanceMeters = distanceMeters;
            Progress01 = progress01;
            CoinsCollected = coinsCollected;
        }

        public bool IsWin => Outcome == RunOutcome.ReachedFinish;
    }

    /// <summary>Mutable state of the run in progress.</summary>
    public sealed class RunSession
    {
        public int Coins { get; private set; }

        public void AddCoins(int amount) => Coins += amount;
    }
}
