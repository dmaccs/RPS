using System.Collections.Generic;
using Godot;

namespace Rps
{
    // Round outcome for effect callbacks
    public enum RoundOutcome
    {
        PlayerWin,
        EnemyWin,
        Draw
    }

    // Stats calculated for a throw each round (damage, block, heal, etc.)
    public class ThrowStats
    {
        public int Damage { get; set; } = 0;      // Damage dealt to enemy
        public int Block { get; set; } = 0;       // Damage reduction this round
        public int Heal { get; set; } = 0;        // Self-heal amount
        public int Lifesteal { get; set; } = 0;   // Heal % of damage dealt

        // Additional properties for special effects
        public float IncomingDamageMultiplier { get; set; } = 1.0f;  // Multiplier for damage taken
        public List<StatusEffect> EnemyStatusEffects { get; set; } = new List<StatusEffect>();
        public List<BuffApplication> BuffsToApply { get; set; } = new List<BuffApplication>();
        public string TransformToThrowId { get; set; } = null;
        public string SpecialMessage { get; set; } = null;

        public ThrowStats() { }

        // Copy constructor
        public ThrowStats(ThrowStats other)
        {
            if (other == null) return;
            Damage = other.Damage;
            Block = other.Block;
            Heal = other.Heal;
            Lifesteal = other.Lifesteal;
            IncomingDamageMultiplier = other.IncomingDamageMultiplier;
        }

        // Apply a multiplier to the base stats (damage, block, heal)
        public ThrowStats ApplyMultiplier(float multiplier)
        {
            return new ThrowStats
            {
                Damage = Mathf.RoundToInt(Damage * multiplier),
                Block = Mathf.RoundToInt(Block * multiplier),
                Heal = Mathf.RoundToInt(Heal * multiplier),
                Lifesteal = Lifesteal, // Lifesteal % doesn't scale
                IncomingDamageMultiplier = IncomingDamageMultiplier,
                EnemyStatusEffects = new List<StatusEffect>(EnemyStatusEffects),
                BuffsToApply = new List<BuffApplication>(BuffsToApply),
                TransformToThrowId = TransformToThrowId,
                SpecialMessage = SpecialMessage
            };
        }
    }

    // Context passed to throw effects during battle resolution
    public class ThrowContext
    {
        public Player Player { get; set; }
        public Enemy Enemy { get; set; }
        public ThrowData Throw { get; set; }
        public RoundOutcome Outcome { get; set; }
        public Throws EnemyThrow { get; set; }              // What the enemy threw this round
        public ThrowData[] EquippedThrows { get; set; }     // For synergy calculations
        public ThrowData[] InventoryThrows { get; set; }    // For inventory-aware effects
        public List<ThrowData> PlayerThrowHistory { get; set; }  // Throws used this battle
        public Dictionary<string, object> BattleState { get; set; }  // Shared battle state
    }

    // Result returned from throw effect execution
    public class ThrowResult
    {
        public int DamageDealt { get; set; }                    // Damage to deal to enemy (on win)
        public int DamageBlocked { get; set; }                  // Damage reduction (on loss)
        public float IncomingDamageMultiplier { get; set; }     // Multiplier for damage taken (1.0 = normal)
        public int HealAmount { get; set; }                     // Self-heal amount
        public List<BuffApplication> BuffsToApply { get; set; } // Status effects on player
        public List<StatusEffect> EnemyStatusEffects { get; set; } // Status effects on enemy
        public string TransformToThrowId { get; set; }          // Transform throw for rest of battle
        public string SpecialMessage { get; set; }              // UI feedback message

        public ThrowResult()
        {
            DamageDealt = 0;
            DamageBlocked = 0;
            IncomingDamageMultiplier = 1.0f;
            HealAmount = 0;
            BuffsToApply = new List<BuffApplication>();
            EnemyStatusEffects = new List<StatusEffect>();
            TransformToThrowId = null;
            SpecialMessage = null;
        }
    }

    // Status effect applied to enemy
    public class StatusEffect
    {
        public string Type { get; set; }    // "radioactive", "poison", "stun", etc.
        public int Stacks { get; set; }     // Number of stacks
        public int Duration { get; set; }   // Turns remaining (0 = permanent until cleared)
    }

    // Buff to be applied after combat resolution
    public class BuffApplication
    {
        public string Target { get; set; }      // "player" or "enemy"
        public string BuffType { get; set; }    // "damage_boost", "damage_reduction", etc.
        public int Amount { get; set; }
        public int Duration { get; set; }       // 0 = permanent
    }

    // Interface for throw effects
    public interface IThrowEffect
    {
        // Get the RPS throw type for combat resolution (used for standard RPS rules)
        Throws GetThrowType(ThrowData throwData);

        // Override standard RPS resolution. Returns null to use standard rules.
        // Use this for throws with completely custom win/loss conditions.
        RoundOutcome? GetCustomOutcome(ThrowData throwData, Throws enemyThrow);

        // Calculate stats for this round based on outcome multiplier
        ThrowStats CalculateStats(ThrowContext context, float outcomeMultiplier);

        // Apply additional effects after stats are calculated (status effects, transformations, etc.)
        void ApplyAdditionalEffects(ThrowContext context, ThrowStats appliedStats);

        // Calculate result when player wins the round
        ThrowResult OnPlayerWin(ThrowContext context);

        // Calculate result when player loses the round
        ThrowResult OnPlayerLose(ThrowContext context);

        // Calculate result on draw
        ThrowResult OnDraw(ThrowContext context);

        // Called after damage is resolved - for effects that need to react to final damage values
        void OnAfterRound(ThrowContext context, RoundOutcome outcome, int damageDealt, int damageTaken);
    }

    // Base class with default implementations for convenience
    public abstract class BaseThrowEffect : IThrowEffect
    {
        public abstract Throws GetThrowType(ThrowData throwData);
        public abstract ThrowResult OnPlayerWin(ThrowContext context);

        // Default: use standard RPS rules (return null)
        public virtual RoundOutcome? GetCustomOutcome(ThrowData throwData, Throws enemyThrow) => null;

        public virtual ThrowResult OnPlayerLose(ThrowContext context) => new ThrowResult();
        public virtual ThrowResult OnDraw(ThrowContext context) => new ThrowResult();
        public virtual void OnAfterRound(ThrowContext context, RoundOutcome outcome, int damageDealt, int damageTaken) { }

        public virtual ThrowStats CalculateStats(ThrowContext context, float outcomeMultiplier)
        {
            var baseStats = context.Throw.Effect?.BaseStats;
            if (baseStats == null)
            {
                // Fallback for legacy effects: use BaseDamage
                return new ThrowStats
                {
                    Damage = Mathf.RoundToInt((context.Throw.Effect?.BaseDamage ?? 0) * outcomeMultiplier)
                };
            }

            // Check for outcome-specific overrides
            var overrides = context.Throw.Effect?.OutcomeOverrides;
            if (overrides != null)
            {
                string outcomeKey = context.Outcome switch
                {
                    RoundOutcome.PlayerWin => "win",
                    RoundOutcome.Draw => "draw",
                    RoundOutcome.EnemyWin => "loss",
                    _ => "draw"
                };

                if (overrides.TryGetValue(outcomeKey, out var overrideStats))
                {
                    // Use override stats directly (no multiplier)
                    return new ThrowStats(overrideStats);
                }
            }

            // Apply multiplier to base stats
            return baseStats.ApplyMultiplier(outcomeMultiplier);
        }

        public virtual void ApplyAdditionalEffects(ThrowContext context, ThrowStats appliedStats) { }
    }
}
