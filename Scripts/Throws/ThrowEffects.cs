using System.Linq;
using Godot;

namespace Rps
{
    // Basic damage - simple base damage on win
    public class BasicDamageEffect : BaseThrowEffect
    {
        public override Throws GetThrowType(ThrowData throwData)
        {
            return throwData.Effect.ThrowType;
        }

        public override ThrowResult OnPlayerWin(ThrowContext context)
        {
            int damage = context.Throw.Effect.BaseDamage;

            return new ThrowResult { DamageDealt = damage };
        }
    }

    // Synergy damage - scales with attribute count across equipped throws
    public class SynergyDamageEffect : BaseThrowEffect
    {
        public override Throws GetThrowType(ThrowData throwData)
        {
            return throwData.Effect.ThrowType;
        }

        public override ThrowResult OnPlayerWin(ThrowContext context)
        {
            int baseDamage = context.Throw.Effect.BaseDamage;

            // Get the primary attribute of this throw for synergy calculation
            var targetAttribute = context.Throw.Attributes.FirstOrDefault();

            // Count matching attributes across all equipped throws
            int attrCount = 0;
            if (context.EquippedThrows != null)
            {
                foreach (var equippedThrow in context.EquippedThrows)
                {
                    if (equippedThrow != null)
                        attrCount += equippedThrow.Attributes.Count(a => a == targetAttribute);
                }
            }

            // Get per-attribute multiplier from parameters (default 1)
            int perAttribute = ThrowEffectHelpers.GetIntParam(context.Throw, "per_attribute", 1);

            int totalDamage = baseDamage + (attrCount * perAttribute);

            return new ThrowResult
            {
                DamageDealt = totalDamage,
                SpecialMessage = attrCount > 0 ? $"Synergy: +{attrCount * perAttribute}" : null
            };
        }
    }

    // Defensive - reduces damage taken on loss
    public class DefensiveEffect : BaseThrowEffect
    {
        public override Throws GetThrowType(ThrowData throwData)
        {
            return throwData.Effect.ThrowType;
        }

        public override ThrowResult OnPlayerWin(ThrowContext context)
        {
            return new ThrowResult { DamageDealt = context.Throw.Effect.BaseDamage };
        }

        public override ThrowResult OnPlayerLose(ThrowContext context)
        {
            int blockAmount = ThrowEffectHelpers.GetIntParam(context.Throw, "block_amount", 1);

            return new ThrowResult
            {
                DamageBlocked = blockAmount,
                SpecialMessage = $"Blocked {blockAmount} damage!"
            };
        }
    }

    // Lifesteal - heals player on win based on damage dealt
    public class LifestealEffect : BaseThrowEffect
    {
        public override Throws GetThrowType(ThrowData throwData)
        {
            return throwData.Effect.ThrowType;
        }

        public override ThrowResult OnPlayerWin(ThrowContext context)
        {
            int damage = context.Throw.Effect.BaseDamage;
            int healPercent = ThrowEffectHelpers.GetIntParam(context.Throw, "heal_percent", 50);
            int healAmount = Mathf.Max(1, damage * healPercent / 100);

            return new ThrowResult
            {
                DamageDealt = damage,
                HealAmount = healAmount,
                SpecialMessage = $"Lifesteal: +{healAmount} HP"
            };
        }
    }

    // Helper class for getting parameters from throw effect data
    public static class ThrowEffectHelpers
    {
        public static int GetIntParam(ThrowData throwData, string key, int defaultValue = 0)
        {
            if (throwData.Effect.Parameters == null ||
                !throwData.Effect.Parameters.TryGetValue(key, out var val))
                return defaultValue;

            if (val is int intVal)
                return intVal;
            if (val is long longVal)
                return (int)longVal;
            if (val is System.Text.Json.JsonElement jsonVal)
                return jsonVal.GetInt32();

            return defaultValue;
        }

        public static float GetFloatParam(ThrowData throwData, string key, float defaultValue = 0f)
        {
            if (throwData.Effect.Parameters == null ||
                !throwData.Effect.Parameters.TryGetValue(key, out var val))
                return defaultValue;

            if (val is float floatVal)
                return floatVal;
            if (val is double doubleVal)
                return (float)doubleVal;
            if (val is System.Text.Json.JsonElement jsonVal)
                return jsonVal.GetSingle();

            return defaultValue;
        }
    }

    // Shale - Always loses, but with different damage multipliers. Transforms to ShaleShards on loss to rock/paper.
    public class ShaleEffect : BaseThrowEffect
    {
        public override Throws GetThrowType(ThrowData throwData)
        {
            return Throws.rock; // Only used as fallback, custom outcome overrides
        }

        // Shale always loses - custom RPS rules
        public override RoundOutcome? GetCustomOutcome(ThrowData throwData, Throws enemyThrow)
        {
            // Shale loses to everything
            return RoundOutcome.EnemyWin;
        }

        public override ThrowResult OnPlayerWin(ThrowContext context)
        {
            // Shale never wins, but just in case
            return new ThrowResult { DamageDealt = context.Throw.Effect.BaseDamage };
        }

        public override ThrowResult OnPlayerLose(ThrowContext context)
        {
            var result = new ThrowResult();

            // Custom damage multipliers based on what beat us
            switch (context.EnemyThrow)
            {
                case Throws.rock:
                    // Loses to rock - take 2x damage, transform to ShaleShards
                    result.IncomingDamageMultiplier = 2.0f;
                    result.TransformToThrowId = "shale_shards";
                    result.SpecialMessage = "Shale crumbles into shards!";
                    break;
                case Throws.paper:
                    // Loses to paper - take 1x damage, transform to ShaleShards
                    result.IncomingDamageMultiplier = 1.0f;
                    result.TransformToThrowId = "shale_shards";
                    result.SpecialMessage = "Shale breaks apart!";
                    break;
                case Throws.scissors:
                    // Loses to scissors - take 0.5x damage, no transformation
                    result.IncomingDamageMultiplier = 0.5f;
                    result.SpecialMessage = "Shale resists the scissors!";
                    break;
            }

            return result;
        }
    }

    // ShaleShards - Transformed form of Shale. Beats paper and scissors, loses to rock.
    public class ShaleShardsEffect : BaseThrowEffect
    {
        public override Throws GetThrowType(ThrowData throwData)
        {
            return Throws.rock; // Only used as fallback, custom outcome overrides
        }

        // Custom RPS: beats paper and scissors, loses to rock
        public override RoundOutcome? GetCustomOutcome(ThrowData throwData, Throws enemyThrow)
        {
            return enemyThrow switch
            {
                Throws.rock => RoundOutcome.EnemyWin,      // Loses to rock
                Throws.paper => RoundOutcome.PlayerWin,   // Beats paper
                Throws.scissors => RoundOutcome.PlayerWin, // Beats scissors
                _ => null // Use standard rules for other throw types
            };
        }

        public override ThrowResult OnPlayerWin(ThrowContext context)
        {
            int baseDamage = context.Throw.Effect.BaseDamage;

            // Deals 2x damage when beating paper or scissors
            return new ThrowResult
            {
                DamageDealt = baseDamage * 2,
                SpecialMessage = "Shards pierce through!"
            };
        }

        public override ThrowResult OnPlayerLose(ThrowContext context)
        {
            // Takes 2x damage from rock
            return new ThrowResult
            {
                IncomingDamageMultiplier = 2.0f,
                SpecialMessage = "Shards are crushed!"
            };
        }
    }

    // Uranium - No damage, applies radioactive status to enemy
    public class UraniumEffect : BaseThrowEffect
    {
        public override Throws GetThrowType(ThrowData throwData)
        {
            return Throws.rock;
        }

        public override ThrowResult OnPlayerWin(ThrowContext context)
        {
            // Uranium does no direct damage, but applies radioactive
            var result = new ThrowResult
            {
                DamageDealt = 0,
                SpecialMessage = "Radioactive exposure!"
            };

            int stacks = ThrowEffectHelpers.GetIntParam(context.Throw, "radioactive_stacks", 1);
            result.EnemyStatusEffects.Add(new StatusEffect
            {
                Type = "radioactive",
                Stacks = stacks,
                Duration = 0 // Permanent for this battle
            });

            return result;
        }

        public override ThrowResult OnDraw(ThrowContext context)
        {
            // Still applies radioactive on draw (vs rock)
            var result = new ThrowResult
            {
                SpecialMessage = "Radioactive exposure!"
            };

            int stacks = ThrowEffectHelpers.GetIntParam(context.Throw, "radioactive_stacks", 1);
            result.EnemyStatusEffects.Add(new StatusEffect
            {
                Type = "radioactive",
                Stacks = stacks,
                Duration = 0
            });

            return result;
        }
    }

    // Grievances Letter - Stores damage taken, releases all on win
    public class GrievancesEffect : BaseThrowEffect
    {
        private const string StoredDamageKey = "stored_damage";

        public override Throws GetThrowType(ThrowData throwData)
        {
            return Throws.paper;
        }

        public override ThrowResult OnPlayerWin(ThrowContext context)
        {
            int baseDamage = context.Throw.Effect.BaseDamage;
            int storedDamage = context.Throw.GetState<int>(StoredDamageKey, 0);

            int totalDamage = baseDamage + storedDamage;

            // Clear stored damage after releasing
            context.Throw.SetState(StoredDamageKey, 0);

            var result = new ThrowResult { DamageDealt = totalDamage };

            if (storedDamage > 0)
            {
                result.SpecialMessage = $"Released {storedDamage} grievances!";
            }

            return result;
        }

        public override void OnAfterRound(ThrowContext context, RoundOutcome outcome, int damageDealt, int damageTaken)
        {
            // Store damage taken when we lose
            if (outcome == RoundOutcome.EnemyWin && damageTaken > 0)
            {
                int currentStored = context.Throw.GetState<int>(StoredDamageKey, 0);
                context.Throw.SetState(StoredDamageKey, currentStored + damageTaken);
                GD.Print($"Grievances stored {damageTaken} damage. Total: {currentStored + damageTaken}");
            }
        }
    }
}
