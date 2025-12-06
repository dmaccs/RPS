using Godot;
using System.Collections.Generic;

namespace Rps
{
    public static class ThrowEffectFactory
    {
        private static Dictionary<string, IThrowEffect> effectCache = new();

        public static IThrowEffect Create(string effectType)
        {
            // Default to basic_damage if not specified
            if (string.IsNullOrEmpty(effectType))
                effectType = "basic_damage";

            // Use cached instance if available
            if (effectCache.TryGetValue(effectType, out var cached))
                return cached;

            IThrowEffect effect = effectType switch
            {
                "basic_damage" => new BasicDamageEffect(),
                "synergy_damage" => new SynergyDamageEffect(),
                "defensive" => new DefensiveEffect(),
                "lifesteal" => new LifestealEffect(),
                "shale" => new ShaleEffect(),
                "shale_shards" => new ShaleShardsEffect(),
                "uranium" => new UraniumEffect(),
                "grievances" => new GrievancesEffect(),
                _ => new BasicDamageEffect()  // Fallback to basic damage
            };

            effectCache[effectType] = effect;

            if (effectType != "basic_damage" && effect is BasicDamageEffect)
            {
                GD.PrintErr($"Unknown throw effect type: {effectType}, falling back to basic_damage");
            }

            return effect;
        }

        // Clear the cache (useful for testing or hot-reloading)
        public static void ClearCache()
        {
            effectCache.Clear();
        }
    }
}
