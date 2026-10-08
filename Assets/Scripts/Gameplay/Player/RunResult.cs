namespace CrazyLabs.Gameplay.Player
{
    public enum eRunOutcome
    {
        Crashed,
        LostMomentum,
        ReachedFinish,
    }

    public readonly struct RunResult
    {
        public readonly eRunOutcome Outcome;
        public readonly float DistanceMeters;
        public readonly float Progress01;
        public readonly int CoinsCollected;

        public bool IsWin => Outcome == eRunOutcome.ReachedFinish;


        public RunResult(eRunOutcome outcome, float distanceMeters, float progress01, int coinsCollected)
        {
            Outcome = outcome;
            DistanceMeters = distanceMeters;
            Progress01 = progress01;
            CoinsCollected = coinsCollected;
        }
    }
}
