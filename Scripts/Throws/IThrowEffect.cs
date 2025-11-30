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
        public ThrowData[] EquippedThrows { get; set; }     // For synergy calculations
        public ThrowData[] InventoryThrows { get; set; }    // For inventory-aware effects
    }

    // Result returned from throw effect execution
    public class ThrowResult
    {
        public int DamageDealt { get; set; }                    // Damage to deal to enemy (on win)
        public int DamageBlocked { get; set; }                  // Damage reduction (on loss)
        public int HealAmount { get; set; }                     // Self-heal amount
        public List<BuffApplication> BuffsToApply { get; set; } // Status effects to apply
        public string SpecialMessage { get; set; }              // UI feedback message

        public ThrowResult()
        {
            DamageDealt = 0;
            DamageBlocked = 0;
            HealAmount = 0;
            BuffsToApply = new List<BuffApplication>();
            SpecialMessage = null;
        }
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
        // Get the RPS throw type for combat resolution
        Throws GetThrowType(ThrowData throwData);

        // Calculate result when player wins the round
        ThrowResult OnPlayerWin(ThrowContext context);

        // Calculate result when player loses the round
        ThrowResult OnPlayerLose(ThrowContext context);

        // Calculate result on draw
        ThrowResult OnDraw(ThrowContext context);
    }
}
