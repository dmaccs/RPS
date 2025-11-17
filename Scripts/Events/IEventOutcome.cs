using Godot;

// Interface for event outcomes
public interface IEventOutcome
{
    void Apply(Player player, EventOutcomeData data);
    string GetResultText(EventOutcomeData data);
}
