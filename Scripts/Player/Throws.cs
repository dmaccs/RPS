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

    // New throw data class with attributes, evolution, and effects
    public class ThrowData
    {
        public string Id { get; set; }                          // Unique identifier (e.g., "basic_rock", "super_scissors")
        public string Name { get; set; }                        // Display name (e.g., "Rock", "Super Scissors")
        public List<ThrowAttribute> Attributes { get; set; }    // 0-3 attributes for synergies
        public bool Evolved { get; set; }                       // Evolution state (for future use)
        public ThrowEffectData Effect { get; set; }             // Effect configuration

        public ThrowData()
        {
            Attributes = new List<ThrowAttribute>();
            Evolved = false;
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

    // DEPRECATED: Keep for backward compatibility during migration
    public class MoveData
    {
        public Throws Type { get; private set; }
        public int Level { get; set; }

        public MoveData(Throws type, int level = 1)
        {
            Type = type;
            Level = level;
        }
    }
}