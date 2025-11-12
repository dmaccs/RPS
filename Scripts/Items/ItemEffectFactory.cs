using Godot;
using System.Collections.Generic;

public static class ItemEffectFactory
{
    private static Dictionary<string, IItemEffect> effectCache = new();

    public static IItemEffect Create(string effectType)
    {
        // Use cached instance if available
        if (effectCache.TryGetValue(effectType, out var cached))
            return cached;

        IItemEffect effect = effectType switch
        {
            "heal" => new HealEffect(),
            "damage_boost" => new DamageBoostEffect(),
            "direct_damage" => new DirectDamageEffect(),
            "damage_reduction" => new DamageReductionEffect(),
            "permanent_damage_boost" => new PermanentDamageBoostEffect(),
            "max_hp_boost" => new MaxHpBoostEffect(),
            "gold_boost" => new GoldBoostEffect(),
            _ => null
        };

        if (effect != null)
        {
            effectCache[effectType] = effect;
        }
        else
        {
            GD.PrintErr($"Unknown item effect type: {effectType}");
        }

        return effect;
    }
}
