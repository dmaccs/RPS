using Godot;
using System;
using System.Linq;

// Modify player health
public class ModifyHealthOutcome : IEventOutcome
{
    public void Apply(Player player, EventOutcomeData data)
    {
        GameState.Instance.ModifyHealth(data.Amount);
    }

    public string GetResultText(EventOutcomeData data)
    {
        if (data.Amount > 0)
            return $"You gained {data.Amount} HP!";
        else
            return $"You lost {-data.Amount} HP!";
    }
}

// Add gold to player
public class AddGoldOutcome : IEventOutcome
{
    public void Apply(Player player, EventOutcomeData data)
    {
        GameState.Instance.AddGold(data.Amount);
    }

    public string GetResultText(EventOutcomeData data)
    {
        if (data.Amount > 0)
            return $"You gained {data.Amount} gold!";
        else
            return $"You spent {-data.Amount} gold!";
    }
}

// Add consumable item to inventory
public class AddItemOutcome : IEventOutcome
{
    private bool wasAdded = false;

    public void Apply(Player player, EventOutcomeData data)
    {
        if (string.IsNullOrEmpty(data.ItemId))
        {
            GD.PrintErr("AddItemOutcome: ItemId is null or empty");
            return;
        }

        wasAdded = GameState.Instance.AddItem(data.ItemId);
    }

    public string GetResultText(EventOutcomeData data)
    {
        var item = ItemDatabase.Instance.GetConsumable(data.ItemId);
        if (!wasAdded)
        {
            return "Inventory full! Item was not added.";
        }
        return item != null ? $"You obtained {item.Name}!" : "You obtained an item!";
    }
}

// Add relic to inventory and apply effect
public class AddRelicOutcome : IEventOutcome
{
    public void Apply(Player player, EventOutcomeData data)
    {
        if (string.IsNullOrEmpty(data.ItemId))
        {
            GD.PrintErr("AddRelicOutcome: ItemId is null or empty");
            return;
        }

        GameState.Instance.AddRelic(data.ItemId);

        // Apply relic effect immediately
        var relic = ItemDatabase.Instance.GetRelic(data.ItemId);
        if (relic?.Effect != null)
        {
            var effect = ItemEffectFactory.Create(relic.Effect.EffectType);
            effect?.Apply(player, null, relic.Effect);
        }
    }

    public string GetResultText(EventOutcomeData data)
    {
        var relic = ItemDatabase.Instance.GetRelic(data.ItemId);
        return relic != null ? $"You obtained {relic.Name}!" : "You obtained a relic!";
    }
}

// Upgrade a specific move by throw type
public class UpgradeMoveOutcome : IEventOutcome
{
    public void Apply(Player player, EventOutcomeData data)
    {
        if (string.IsNullOrEmpty(data.TargetMove))
        {
            GD.PrintErr("UpgradeMoveOutcome: TargetMove is null or empty");
            return;
        }

        // Parse throw type from string
        if (Enum.TryParse<Rps.Throws>(data.TargetMove, true, out var throwType))
        {
            // Find equipped throw matching this type
            for (int i = 0; i < player.EquippedThrows.Length; i++)
            {
                var throwData = player.EquippedThrows[i];
                if (throwData?.Effect.ThrowType == throwType)
                {
                    for (int j = 0; j < data.Amount; j++)
                    {
                        player.UpgradeEquippedThrow(i);
                    }
                    break;
                }
            }
            GameState.Instance.RefreshUI();
        }
        else
        {
            GD.PrintErr($"Invalid throw type: {data.TargetMove}");
        }
    }

    public string GetResultText(EventOutcomeData data)
    {
        string moveName = char.ToUpper(data.TargetMove[0]) + data.TargetMove.Substring(1);
        return $"{moveName} upgraded by {data.Amount}!";
    }
}

// Downgrade a specific move by throw type
public class DowngradeMoveOutcome : IEventOutcome
{
    public void Apply(Player player, EventOutcomeData data)
    {
        if (string.IsNullOrEmpty(data.TargetMove))
        {
            GD.PrintErr("DowngradeMoveOutcome: TargetMove is null or empty");
            return;
        }

        // Parse throw type from string
        if (Enum.TryParse<Rps.Throws>(data.TargetMove, true, out var throwType))
        {
            // Find equipped throw matching this type and reduce its damage
            for (int i = 0; i < player.EquippedThrows.Length; i++)
            {
                var throwData = player.EquippedThrows[i];
                if (throwData?.Effect.ThrowType == throwType)
                {
                    throwData.Effect.BaseDamage = Math.Max(1, throwData.Effect.BaseDamage - data.Amount);
                    GameState.Instance.RefreshUI();
                    break;
                }
            }
        }
        else
        {
            GD.PrintErr($"Invalid throw type: {data.TargetMove}");
        }
    }

    public string GetResultText(EventOutcomeData data)
    {
        string moveName = char.ToUpper(data.TargetMove[0]) + data.TargetMove.Substring(1);
        return $"{moveName} downgraded by {data.Amount}!";
    }
}

// Upgrade all equipped moves
public class UpgradeAllMovesOutcome : IEventOutcome
{
    public void Apply(Player player, EventOutcomeData data)
    {
        for (int i = 0; i < player.EquippedThrows.Length; i++)
        {
            if (player.EquippedThrows[i] != null)
            {
                for (int j = 0; j < data.Amount; j++)
                {
                    player.UpgradeEquippedThrow(i);
                }
            }
        }
        GameState.Instance.RefreshUI();
    }

    public string GetResultText(EventOutcomeData data)
    {
        return $"All moves upgraded by {data.Amount}!";
    }
}

// Increase max health (also heals for the amount)
public class IncreaseMaxHealthOutcome : IEventOutcome
{
    public void Apply(Player player, EventOutcomeData data)
    {
        GameState.Instance.IncreaseMaxHealth(data.Amount);
    }

    public string GetResultText(EventOutcomeData data)
    {
        return $"Max HP increased by {data.Amount}!";
    }
}

// Modify max health (can be positive or negative, adjusts current HP accordingly)
public class ModifyMaxHealthOutcome : IEventOutcome
{
    public void Apply(Player player, EventOutcomeData data)
    {
        if (data.Amount > 0)
        {
            GameState.Instance.IncreaseMaxHealth(data.Amount);
        }
        else
        {
            // Decrease max health
            int newMax = Math.Max(1, GameState.Instance.MaxPlayerHealth + data.Amount);
            GameState.Instance.SetMaxHealth(newMax);
        }
    }

    public string GetResultText(EventOutcomeData data)
    {
        if (data.Amount > 0)
            return $"Max HP increased by {data.Amount}!";
        else
            return $"Max HP decreased by {-data.Amount}!";
    }
}

// Random outcome (success or failure based on success rate)
public class RandomOutcome : IEventOutcome
{
    public void Apply(Player player, EventOutcomeData data)
    {
        var rng = RngManager.Instance?.Rng ?? new RandomNumberGenerator();
        int roll = rng.RandiRange(1, 100);

        bool success = roll <= data.SuccessRate;

        var outcomesToApply = success ? data.SuccessOutcomes : data.FailureOutcomes;

        if (outcomesToApply != null)
        {
            foreach (var outcomeData in outcomesToApply)
            {
                var outcome = EventOutcomeFactory.Create(outcomeData.Type);
                outcome?.Apply(player, outcomeData);
            }
        }

        GD.Print($"Random outcome: roll={roll}, success={success}");
    }

    public string GetResultText(EventOutcomeData data)
    {
        var rng = RngManager.Instance?.Rng ?? new RandomNumberGenerator();
        int roll = rng.RandiRange(1, 100);
        bool success = roll <= data.SuccessRate;

        if (success && data.SuccessOutcomes != null && data.SuccessOutcomes.Count > 0)
        {
            return "Success! " + GetOutcomeText(data.SuccessOutcomes[0]);
        }
        else if (!success && data.FailureOutcomes != null && data.FailureOutcomes.Count > 0)
        {
            return "Failed! " + GetOutcomeText(data.FailureOutcomes[0]);
        }

        return success ? "Success!" : "Failed!";
    }

    private string GetOutcomeText(EventOutcomeData outcome)
    {
        var outcomeHandler = EventOutcomeFactory.Create(outcome.Type);
        return outcomeHandler?.GetResultText(outcome) ?? "";
    }
}
