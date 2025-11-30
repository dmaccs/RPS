using System.Linq;
using Godot;

namespace Rps
{
    // Basic damage - simple base damage on win
    public class BasicDamageEffect : IThrowEffect
    {
        public Throws GetThrowType(ThrowData throwData)
        {
            return throwData.Effect.ThrowType;
        }

        public ThrowResult OnPlayerWin(ThrowContext context)
        {
            int damage = context.Throw.Effect.BaseDamage;

            return new ThrowResult { DamageDealt = damage };
        }

        public ThrowResult OnPlayerLose(ThrowContext context)
        {
            return new ThrowResult();
        }

        public ThrowResult OnDraw(ThrowContext context)
        {
            return new ThrowResult();
        }
    }

    // Synergy damage - scales with attribute count across equipped throws
    public class SynergyDamageEffect : IThrowEffect
    {
        public Throws GetThrowType(ThrowData throwData)
        {
            return throwData.Effect.ThrowType;
        }

        public ThrowResult OnPlayerWin(ThrowContext context)
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
            int perAttribute = 1;
            if (context.Throw.Effect.Parameters != null &&
                context.Throw.Effect.Parameters.TryGetValue("per_attribute", out var val))
            {
                if (val is int intVal)
                    perAttribute = intVal;
                else if (val is long longVal)
                    perAttribute = (int)longVal;
                else if (val is System.Text.Json.JsonElement jsonVal)
                    perAttribute = jsonVal.GetInt32();
            }

            int totalDamage = baseDamage + (attrCount * perAttribute);

            return new ThrowResult
            {
                DamageDealt = totalDamage,
                SpecialMessage = attrCount > 0 ? $"Synergy: +{attrCount * perAttribute}" : null
            };
        }

        public ThrowResult OnPlayerLose(ThrowContext context)
        {
            return new ThrowResult();
        }

        public ThrowResult OnDraw(ThrowContext context)
        {
            return new ThrowResult();
        }
    }

    // Defensive - reduces damage taken on loss
    public class DefensiveEffect : IThrowEffect
    {
        public Throws GetThrowType(ThrowData throwData)
        {
            return throwData.Effect.ThrowType;
        }

        public ThrowResult OnPlayerWin(ThrowContext context)
        {
            return new ThrowResult { DamageDealt = context.Throw.Effect.BaseDamage };
        }

        public ThrowResult OnPlayerLose(ThrowContext context)
        {
            // Get block amount from parameters (default 1)
            int blockAmount = 1;
            if (context.Throw.Effect.Parameters != null &&
                context.Throw.Effect.Parameters.TryGetValue("block_amount", out var val))
            {
                if (val is int intVal)
                    blockAmount = intVal;
                else if (val is long longVal)
                    blockAmount = (int)longVal;
                else if (val is System.Text.Json.JsonElement jsonVal)
                    blockAmount = jsonVal.GetInt32();
            }

            return new ThrowResult
            {
                DamageBlocked = blockAmount,
                SpecialMessage = $"Blocked {blockAmount} damage!"
            };
        }

        public ThrowResult OnDraw(ThrowContext context)
        {
            return new ThrowResult();
        }
    }

    // Lifesteal - heals player on win based on damage dealt
    public class LifestealEffect : IThrowEffect
    {
        public Throws GetThrowType(ThrowData throwData)
        {
            return throwData.Effect.ThrowType;
        }

        public ThrowResult OnPlayerWin(ThrowContext context)
        {
            int damage = context.Throw.Effect.BaseDamage;

            // Get heal percentage from parameters (default 50%)
            int healPercent = 50;
            if (context.Throw.Effect.Parameters != null &&
                context.Throw.Effect.Parameters.TryGetValue("heal_percent", out var val))
            {
                if (val is int intVal)
                    healPercent = intVal;
                else if (val is long longVal)
                    healPercent = (int)longVal;
                else if (val is System.Text.Json.JsonElement jsonVal)
                    healPercent = jsonVal.GetInt32();
            }

            int healAmount = Mathf.Max(1, damage * healPercent / 100);

            return new ThrowResult
            {
                DamageDealt = damage,
                HealAmount = healAmount,
                SpecialMessage = $"Lifesteal: +{healAmount} HP"
            };
        }

        public ThrowResult OnPlayerLose(ThrowContext context)
        {
            return new ThrowResult();
        }

        public ThrowResult OnDraw(ThrowContext context)
        {
            return new ThrowResult();
        }
    }
}
