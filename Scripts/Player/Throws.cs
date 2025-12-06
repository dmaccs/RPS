using System.Collections.Generic;
using System.Linq;

namespace Rps
{
    // RPS type for combat resolution
    public enum Throws
    {
        rock, paper, scissors, turkey, dynamite, wizard, lizard, spock
    }

    // Attributes for synergy tracking (separate from throw type)
    public enum ThrowAttribute
    {
        Rock,
        Paper,
        Scissors
    }

    public enum ThrowRarity
    {
        Starter,
        Common,
        Uncommon,
        Rare
    }

    // New throw data class with attributes, evolution, and effects
    public class ThrowData
    {
        public string Id { get; set; }                          // Unique identifier (e.g., "basic_rock", "super_scissors")
        public string Name { get; set; }                        // Display name (e.g., "Rock", "Super Scissors")
        public string Description { get; set; }                 // Description for tooltips
        public List<ThrowAttribute> Attributes { get; set; }    // 0-3 attributes for synergies
        public bool Evolved { get; set; }                       // Evolution state (for future use)
        public ThrowEffectData Effect { get; set; }             // Effect configuration
        public ThrowRarity Rarity { get; set; }
        public int Cost { get; set; }
        public Dictionary<string, object> State { get; set; }   // Persistent state for effects (damage stored, etc.)

        public ThrowData()
        {
            Attributes = new List<ThrowAttribute>();
            Evolved = false;
            Rarity = ThrowRarity.Common;
            Cost = 50;
            Description = "";
            State = new Dictionary<string, object>();
        }

        // Helper to get state value with default
        public T GetState<T>(string key, T defaultValue = default)
        {
            if (State.TryGetValue(key, out var val))
            {
                if (val is T typedVal)
                    return typedVal;
                if (val is System.Text.Json.JsonElement jsonVal)
                {
                    // Handle JSON deserialization
                    if (typeof(T) == typeof(int))
                        return (T)(object)jsonVal.GetInt32();
                    if (typeof(T) == typeof(float))
                        return (T)(object)jsonVal.GetSingle();
                    if (typeof(T) == typeof(string))
                        return (T)(object)jsonVal.GetString();
                }
                // Try conversion for numeric types
                try { return (T)System.Convert.ChangeType(val, typeof(T)); }
                catch { return defaultValue; }
            }
            return defaultValue;
        }

        // Helper to set state value
        public void SetState<T>(string key, T value)
        {
            State[key] = value;
        }

        // Helper to check if this throw has a specific attribute
        public bool HasAttribute(ThrowAttribute attr)
        {
            return Attributes.Contains(attr);
        }

        // Count how many of a specific attribute this throw has
        public int CountAttribute(ThrowAttribute attr)
        {
            return Attributes.Count(a => a == attr);
        }
    }
}