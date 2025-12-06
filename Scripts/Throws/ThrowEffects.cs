using System.Linq;
using System.Collections.Generic;
using Godot;

namespace Rps
{
    // StandardEffect - The new default effect that uses BaseStats with outcome multipliers
    // Most throws should use this effect type
    public class StandardEffect : BaseThrowEffect
    {
        public override Throws GetThrowType(ThrowData throwData)
        {
            return throwData.Effect.ThrowType;
        }

        // CalculateStats is inherited from BaseThrowEffect and handles:
        // - OutcomeOverrides (if present, uses those directly)
        // - BaseStats with multiplier applied
        // - Fallback to BaseDamage for legacy throws

        public override ThrowResult OnPlayerWin(ThrowContext context)
        {
            // Legacy method - convert from new stats system for backward compatibility
            var stats = CalculateStats(context, CombatConfig.PlayerWinMultiplier);
            return ConvertStatsToResult(stats);
        }

        public override ThrowResult OnPlayerLose(ThrowContext context)
        {
            var stats = CalculateStats(context, CombatConfig.PlayerLossMultiplier);
            return ConvertStatsToResult(stats);
        }

        public override ThrowResult OnDraw(ThrowContext context)
        {
            var stats = CalculateStats(context, CombatConfig.PlayerDrawMultiplier);
            return ConvertStatsToResult(stats);
        }

        private ThrowResult ConvertStatsToResult(ThrowStats stats)
        {
            return new ThrowResult
            {
                DamageDealt = stats.Damage,
                DamageBlocked = stats.Block,
                HealAmount = stats.Heal + (stats.Lifesteal > 0 ? Mathf.Max(1, stats.Damage * stats.Lifesteal / 100) : 0),
                IncomingDamageMultiplier = stats.IncomingDamageMultiplier,
                EnemyStatusEffects = stats.EnemyStatusEffects,
                BuffsToApply = stats.BuffsToApply,
                TransformToThrowId = stats.TransformToThrowId,
                SpecialMessage = stats.SpecialMessage
            };
        }
    }

    // Combat configuration - all multipliers are configurable
    public static class CombatConfig
    {
        // Player output multipliers (damage, block, heal)
        public static float PlayerWinMultiplier = 1.5f;
        public static float PlayerDrawMultiplier = 1.0f;
        public static float PlayerLossMultiplier = 0.5f;

        // Incoming enemy damage multipliers (inverse scaling)
        public static float IncomingWinMultiplier = 0.5f;   // Less damage when winning
        public static float IncomingDrawMultiplier = 1.0f;
        public static float IncomingLossMultiplier = 1.5f;  // More damage when losing

        // Get the player output multiplier for an outcome
        public static float GetPlayerMultiplier(RoundOutcome outcome)
        {
            return outcome switch
            {
                RoundOutcome.PlayerWin => PlayerWinMultiplier,
                RoundOutcome.Draw => PlayerDrawMultiplier,
                RoundOutcome.EnemyWin => PlayerLossMultiplier,
                _ => 1.0f
            };
        }

        // Get the incoming damage multiplier for an outcome
        public static float GetIncomingMultiplier(RoundOutcome outcome)
        {
            return outcome switch
            {
                RoundOutcome.PlayerWin => IncomingWinMultiplier,
                RoundOutcome.Draw => IncomingDrawMultiplier,
                RoundOutcome.EnemyWin => IncomingLossMultiplier,
                _ => 1.0f
            };
        }
    }

    // Synergy damage - scales with attribute count across equipped throws
    public class SynergyDamageEffect : BaseThrowEffect
    {
        public override Throws GetThrowType(ThrowData throwData)
        {
            return throwData.Effect.ThrowType;
        }

        public override ThrowStats CalculateStats(ThrowContext context, float outcomeMultiplier)
        {
            int baseDamage = context.Throw.Effect?.BaseDamage ?? 0;

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

            // Apply outcome multiplier
            totalDamage = Mathf.RoundToInt(totalDamage * outcomeMultiplier);

            return new ThrowStats
            {
                Damage = totalDamage,
                SpecialMessage = attrCount > 0 ? $"Synergy: +{attrCount * perAttribute}" : null
            };
        }

        public override ThrowResult OnPlayerWin(ThrowContext context)
        {
            var stats = CalculateStats(context, CombatConfig.PlayerWinMultiplier);
            return new ThrowResult
            {
                DamageDealt = stats.Damage,
                SpecialMessage = stats.SpecialMessage
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

        public override ThrowStats CalculateStats(ThrowContext context, float outcomeMultiplier)
        {
            var stats = new ThrowStats
            {
                Damage = 0, // Shale does no damage (always loses)
                Block = 0
            };

            // Custom damage multipliers based on what beat us
            switch (context.EnemyThrow)
            {
                case Throws.rock:
                    // Loses to rock - take 2x damage, transform to ShaleShards
                    stats.IncomingDamageMultiplier = 2.0f;
                    stats.TransformToThrowId = "shale_shards";
                    stats.SpecialMessage = "Shale crumbles into shards!";
                    break;
                case Throws.paper:
                    // Loses to paper - take 1x damage, transform to ShaleShards
                    stats.IncomingDamageMultiplier = 1.0f;
                    stats.TransformToThrowId = "shale_shards";
                    stats.SpecialMessage = "Shale breaks apart!";
                    break;
                case Throws.scissors:
                    // Loses to scissors - take 0.5x damage, no transformation
                    stats.IncomingDamageMultiplier = 0.5f;
                    stats.SpecialMessage = "Shale resists the scissors!";
                    break;
            }

            return stats;
        }

        public override ThrowResult OnPlayerWin(ThrowContext context)
        {
            // Shale never wins, but just in case
            return new ThrowResult { DamageDealt = context.Throw.Effect?.BaseDamage ?? 0 };
        }

        public override ThrowResult OnPlayerLose(ThrowContext context)
        {
            var stats = CalculateStats(context, CombatConfig.PlayerLossMultiplier);
            return new ThrowResult
            {
                IncomingDamageMultiplier = stats.IncomingDamageMultiplier,
                TransformToThrowId = stats.TransformToThrowId,
                SpecialMessage = stats.SpecialMessage
            };
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

        public override ThrowStats CalculateStats(ThrowContext context, float outcomeMultiplier)
        {
            int baseDamage = context.Throw.Effect?.BaseDamage ?? 10;

            var stats = new ThrowStats();

            if (context.Outcome == RoundOutcome.PlayerWin)
            {
                // Deals 2x damage when winning (beating paper or scissors)
                stats.Damage = Mathf.RoundToInt(baseDamage * 2 * outcomeMultiplier);
                stats.SpecialMessage = "Shards pierce through!";
            }
            else if (context.Outcome == RoundOutcome.EnemyWin)
            {
                // Takes 2x damage from rock
                stats.Damage = 0;
                stats.IncomingDamageMultiplier = 2.0f;
                stats.SpecialMessage = "Shards are crushed!";
            }

            return stats;
        }

        public override ThrowResult OnPlayerWin(ThrowContext context)
        {
            int baseDamage = context.Throw.Effect?.BaseDamage ?? 10;

            return new ThrowResult
            {
                DamageDealt = baseDamage * 2,
                SpecialMessage = "Shards pierce through!"
            };
        }

        public override ThrowResult OnPlayerLose(ThrowContext context)
        {
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

        public override ThrowStats CalculateStats(ThrowContext context, float outcomeMultiplier)
        {
            // Uranium does no damage, but applies radioactive via ApplyAdditionalEffects
            return new ThrowStats
            {
                Damage = 0,
                Block = 0,
                SpecialMessage = "Radioactive exposure!"
            };
        }

        public override void ApplyAdditionalEffects(ThrowContext context, ThrowStats appliedStats)
        {
            // Apply radioactive on any outcome except loss
            if (context.Outcome != RoundOutcome.EnemyWin)
            {
                int stacks = ThrowEffectHelpers.GetIntParam(context.Throw, "radioactive_stacks", 1);
                context.Enemy.ApplyStatusEffect("radioactive", stacks);
            }
        }

        public override ThrowResult OnPlayerWin(ThrowContext context)
        {
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
                Duration = 0
            });

            return result;
        }

        public override ThrowResult OnDraw(ThrowContext context)
        {
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

        public override ThrowStats CalculateStats(ThrowContext context, float outcomeMultiplier)
        {
            int baseDamage = context.Throw.Effect?.BaseDamage ?? 4;
            int storedDamage = context.Throw.GetState<int>(StoredDamageKey, 0);

            // Base damage scales with multiplier, stored damage does not
            int scaledBase = Mathf.RoundToInt(baseDamage * outcomeMultiplier);
            int totalDamage = scaledBase + storedDamage;

            string message = storedDamage > 0 ? $"Released {storedDamage} grievances!" : null;

            // Clear stored damage on any outcome (it's released)
            if (storedDamage > 0)
            {
                context.Throw.SetState(StoredDamageKey, 0);
            }

            return new ThrowStats
            {
                Damage = totalDamage,
                SpecialMessage = message
            };
        }

        public override ThrowResult OnPlayerWin(ThrowContext context)
        {
            var stats = CalculateStats(context, CombatConfig.PlayerWinMultiplier);
            return new ThrowResult
            {
                DamageDealt = stats.Damage,
                SpecialMessage = stats.SpecialMessage
            };
        }

        public override void OnAfterRound(ThrowContext context, RoundOutcome outcome, int damageDealt, int damageTaken)
        {
            // Store damage taken when we lose
            if (damageTaken > 0)
            {
                int currentStored = context.Throw.GetState<int>(StoredDamageKey, 0);
                context.Throw.SetState(StoredDamageKey, currentStored + damageTaken);
                GD.Print($"Grievances stored {damageTaken} damage. Total: {currentStored + damageTaken}");
            }
        }
    }
}
