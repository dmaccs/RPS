using System.Collections.Generic;

namespace Rps
{
    public class ThrowEffectData
    {
        public string EffectType { get; set; }              // "basic_damage", "synergy_damage", etc.
        public Throws ThrowType { get; set; }               // The RPS type this effect uses (rock/paper/scissors)
        public int BaseDamage { get; set; }                 // Base damage value
        public Dictionary<string, object> Parameters { get; set; }  // Effect-specific parameters

        public ThrowEffectData()
        {
            EffectType = "basic_damage";
            ThrowType = Throws.rock;
            BaseDamage = 1;
            Parameters = new Dictionary<string, object>();
        }
    }
}
