using System.Collections.Generic;

namespace Rps
{
    public class ThrowEffectData
    {
        public string EffectType { get; set; }              // "standard", "synergy_damage", etc.
        public Throws ThrowType { get; set; }               // rock/paper/scissors

        // Base stats that apply with outcome multipliers
        public ThrowStats BaseStats { get; set; }

        // Per-outcome overrides (optional) - keys: "win", "draw", "loss"
        // If present, replaces base calculation for that outcome (no multiplier applied)
        public Dictionary<string, ThrowStats> OutcomeOverrides { get; set; }

        // Fallback damage for effects that don't use BaseStats
        public int BaseDamage { get; set; }

        public Dictionary<string, object> Parameters { get; set; }  // Effect-specific parameters

        public ThrowEffectData()
        {
            EffectType = "standard";
            ThrowType = Throws.rock;
            BaseStats = new ThrowStats();
            OutcomeOverrides = null;
            BaseDamage = 0;
            Parameters = new Dictionary<string, object>();
        }
    }
}
