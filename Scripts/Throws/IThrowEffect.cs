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
    }
}
