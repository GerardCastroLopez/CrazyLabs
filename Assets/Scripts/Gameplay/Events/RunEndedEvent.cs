using CrazyLabs.Characters;
using CrazyLabs.Gameplay.Player;

namespace CrazyLabs.Gameplay.Events
{
    public class RunEndedEvent
    {
        public readonly RunResult Result;
        public readonly CharacterData Character;


        public RunEndedEvent(RunResult result, CharacterData character)
        {
            Result = result;
            Character = character;
        }
    }
}
