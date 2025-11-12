using Godot;

// Instant heal effect
public class HealEffect : IItemEffect
{
    public void Apply(Player player, Enemy enemy, ItemEffectData effectData)
    {
        int healAmount = effectData.Amount;
        player.Heal(healAmount);
        GD.Print($"Healed for {healAmount} HP");
    }
}

// Temporary damage boost effect (duration in turns)
public class DamageBoostEffect : IItemEffect
{
    public void Apply(Player player, Enemy enemy, ItemEffectData effectData)
    {
        int boostAmount = effectData.Amount;
        int duration = effectData.Duration;

        // Add buff to game state
        GameState.Instance.AddBuff("damage_boost", boostAmount, duration);
        GD.Print($"Damage increased by {boostAmount} for {duration} turns");
    }
}

// Direct damage to enemy
public class DirectDamageEffect : IItemEffect
{
    public void Apply(Player player, Enemy enemy, ItemEffectData effectData)
    {
        int damage = effectData.Amount;
        enemy.TakeDamage(damage);
        GD.Print($"Dealt {damage} direct damage to enemy");
    }
}

// Temporary damage reduction effect (reduces incoming damage)
public class DamageReductionEffect : IItemEffect
{
    public void Apply(Player player, Enemy enemy, ItemEffectData effectData)
    {
        int reductionAmount = effectData.Amount;
        int duration = effectData.Duration;

        // Add buff to game state
        GameState.Instance.AddBuff("damage_reduction", reductionAmount, duration);
        GD.Print($"Incoming damage reduced by {reductionAmount} for {duration} turns");
    }
}
