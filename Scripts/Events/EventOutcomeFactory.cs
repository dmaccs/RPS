using Godot;

public static class EventOutcomeFactory
{
    public static IEventOutcome Create(string outcomeType)
    {
        return outcomeType switch
        {
            "modify_health" => new ModifyHealthOutcome(),
            "add_gold" => new AddGoldOutcome(),
            "add_item" => new AddItemOutcome(),
            "add_relic" => new AddRelicOutcome(),
            "upgrade_move" => new UpgradeMoveOutcome(),
            "downgrade_move" => new DowngradeMoveOutcome(),
            "upgrade_all_moves" => new UpgradeAllMovesOutcome(),
            "increase_max_health" => new IncreaseMaxHealthOutcome(),
            "modify_max_health" => new ModifyMaxHealthOutcome(),
            "random_outcome" => new RandomOutcome(),
            _ => null
        };
    }
}
