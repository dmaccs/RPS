using Godot;

// Permanent damage boost to all moves
public class PermanentDamageBoostEffect : IItemEffect
{
    public void Apply(Player player, Enemy enemy, ItemEffectData effectData)
    {
        int boostAmount = effectData.Amount;

        // Add permanent buff to game state
        GameState.Instance.AddPermanentBuff("damage_boost", boostAmount);
        GD.Print($"All moves permanently deal +{boostAmount} damage");
    }
}

// Increase maximum HP
public class MaxHpBoostEffect : IItemEffect
{
    public void Apply(Player player, Enemy enemy, ItemEffectData effectData)
    {
        int hpBoost = effectData.Amount;

        // Increase max HP and heal to full
        GameState.Instance.IncreaseMaxHealth(hpBoost);
        GD.Print($"Maximum HP increased by {hpBoost}");
    }
}

// Passive gold gain after battles
public class GoldBoostEffect : IItemEffect
{
    public void Apply(Player player, Enemy enemy, ItemEffectData effectData)
    {
        int goldBoost = effectData.Amount;

        // Add passive gold relic to game state
        GameState.Instance.AddPermanentBuff("gold_boost", goldBoost);
        GD.Print($"Will gain +{goldBoost} extra gold after each battle");
    }
}
