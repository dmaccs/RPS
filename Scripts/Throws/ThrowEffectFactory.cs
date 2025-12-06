using Godot;
using System.Collections.Generic;

namespace Rps
{
    public static class ThrowEffectFactory
    {
        private static Dictionary<string, IThrowEffect> effectCache = new();

        public static IThrowEffect Create(string effectType)
        {
            // Default to standard if not specified
            if (string.IsNullOrEmpty(effectType))
                effectType = "standard";

            // Use cached instance if available
            if (effectCache.TryGetValue(effectType, out var cached))
                return cached;

            IThrowEffect effect = effectType switch
            {
                "standard" => new StandardEffect(),
                "synergy_damage" => new SynergyDamageEffect(),
                "shale" => new ShaleEffect(),
                "shale_shards" => new ShaleShardsEffect(),
                "uranium" => new UraniumEffect(),
                "grievances" => new GrievancesEffect(),
                _ => new StandardEffect()
            };

            effectCache[effectType] = effect;

            // Log warning for unknown effect types
            var knownTypes = new[] { "standard", "synergy_damage", "shale", "shale_shards", "uranium", "grievances" };
            if (!System.Array.Exists(knownTypes, t => t == effectType))
            {
                GD.PrintErr($"Unknown throw effect type: {effectType}, falling back to standard");
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
