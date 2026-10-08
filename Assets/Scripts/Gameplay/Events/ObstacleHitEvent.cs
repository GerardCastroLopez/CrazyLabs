using CrazyLabs.Gameplay.Track;

namespace CrazyLabs.Gameplay.Events
{
    public class ObstacleHitEvent
    {
        public readonly Obstacle Obstacle;


        public ObstacleHitEvent(Obstacle obstacle)
        {
            Obstacle = obstacle;
        }
    }
}
