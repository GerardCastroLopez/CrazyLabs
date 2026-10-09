namespace CrazyLabs.Gameplay.Events
{
    public class RunLaunchedEvent
    {
        public readonly float AnimationSeconds;


        public RunLaunchedEvent(float animationSeconds)
        {
            AnimationSeconds = animationSeconds;
        }
    }
}
