using System.Collections.Generic;
using Godot;
using Rps;
using RPS.Scripts.Behaviors;

public class RoundResult
{
    public ThrowData PlayerThrowData { get; set; }
    public Throws PlayerThrow { get; set; }
    public Throws EnemyThrow { get; set; }
    public RoundOutcome Outcome { get; set; }
    public int DamageDealt { get; set; }
    public string DamageTarget { get; set; }
    public int HealAmount { get; set; }
    public string SpecialMessage { get; set; }
}

public class BattleManager
{
    private Player player;
    private Enemy enemy;

    // Track throws per round during this encounter
    // Each round is a list of throws (usually 1, but can be multiple for special abilities)
    public List<List<Throws>> EncounterPlayerThrows { get; private set; } = new List<List<Throws>>();
    public List<List<Throws>> EncounterEnemyThrows { get; private set; } = new List<List<Throws>>();

    // Battle-specific state shared across effects
    public Dictionary<string, object> BattleState { get; private set; } = new Dictionary<string, object>();

    // Track which throws have been transformed this battle (Id -> replacement Id)
    private Dictionary<string, string> transformedThrows = new Dictionary<string, string>();

    // Player throw history for this battle (ThrowData objects)
    public List<ThrowData> PlayerThrowDataHistory { get; private set; } = new List<ThrowData>();

    // Persistent block system
    private int persistentBlock = 0;
    private int persistentBlockDuration = 0;

    public BattleManager(Player player, Enemy enemy)
    {
        this.player = player;
        this.enemy = enemy;
    }

    // Get total block (round block + persistent block)
    public int GetTotalBlock(int roundBlock)
    {
        return roundBlock + persistentBlock;
    }

    // Add persistent block that lasts multiple rounds
    public void AddPersistentBlock(int amount, int duration)
    {
        persistentBlock += amount;
        persistentBlockDuration = Godot.Mathf.Max(persistentBlockDuration, duration);
        GD.Print($"Added persistent block: {amount} for {duration} rounds. Total: {persistentBlock}");
    }

    // Process persistent block at end of round
    private void ProcessPersistentBlock()
    {
        if (persistentBlockDuration > 0)
        {
            persistentBlockDuration--;
            if (persistentBlockDuration <= 0)
            {
                GD.Print($"Persistent block expired: {persistentBlock}");
                persistentBlock = 0;
            }
        }
    }

    // Get the effective throw data, accounting for transformations
    public ThrowData GetEffectiveThrowData(ThrowData original)
    {
        if (original == null) return null;

        if (transformedThrows.TryGetValue(original.Id, out string replacementId))
        {
            var replacement = ThrowDatabase.Instance.CreateInstance(replacementId);
            if (replacement != null)
            {
                // Copy over the original's state
                foreach (var kvp in original.State)
                    replacement.State[kvp.Key] = kvp.Value;
                return replacement;
            }
        }
        return original;
    }

    // Apply a transformation for the rest of the battle
    public void TransformThrow(string originalId, string newId)
    {
        transformedThrows[originalId] = newId;
        GD.Print($"Throw transformed: {originalId} -> {newId}");
    }

    public RoundResult ResolveRound(ThrowData playerThrowData)
    {
        // Process status effects at the start of the round
        int statusDamage = enemy.ProcessStatusEffects();

        // Check for trapped state - use special trapped resolution
        if (enemy.IsTrapped)
        {
            return ResolveTrappedRound(playerThrowData);
        }

        // Apply any transformations for this battle
        playerThrowData = GetEffectiveThrowData(playerThrowData);

        // Get the effect for this throw
        var effect = ThrowEffectFactory.Create(playerThrowData.Effect.EffectType);

        // Determine the actual throw type from the effect
        Throws playerThrow = effect.GetThrowType(playerThrowData);

        var result = new RoundResult
        {
            PlayerThrowData = playerThrowData,
            PlayerThrow = playerThrow,
            EnemyThrow = Throws.rock,
            Outcome = RoundOutcome.Draw,
            DamageDealt = 0,
            DamageTarget = "",
            HealAmount = 0,
            SpecialMessage = null
        };

        // Get enemy throw
        Throws enemyThrow = enemy.ChooseThrow(player.LastThrow, player.ThrowHistory, EncounterPlayerThrows, EncounterEnemyThrows);

        // Boss mechanic: throw twice and pick the best result
        bool isBossDraw = false;
        if (enemy.isBoss)
        {
            Throws enemyThrow2 = enemy.ChooseThrow(player.LastThrow, player.ThrowHistory, EncounterPlayerThrows, EncounterEnemyThrows);
            int attempts = 0;
            while (enemyThrow2 == enemyThrow && attempts < 10)
            {
                enemyThrow2 = enemy.ChooseThrow(player.LastThrow, player.ThrowHistory, EncounterPlayerThrows, EncounterEnemyThrows);
                attempts++;
            }

            int result1 = CompareThrows(enemyThrow, playerThrow);
            int result2 = CompareThrows(enemyThrow2, playerThrow);

            GD.Print($"Boss threw: {enemyThrow} (result: {result1}) and {enemyThrow2} (result: {result2})");

            int bestResult = Godot.Mathf.Max(result1, result2);

            if (bestResult == 0)
            {
                isBossDraw = true;
                enemyThrow = result1 == 0 ? enemyThrow : enemyThrow2;
                GD.Print($"Boss's best result is DRAW - player deals half damage");
            }
            else if (result1 > result2)
            {
                GD.Print($"Boss chose {enemyThrow}");
            }
            else if (result2 > result1)
            {
                enemyThrow = enemyThrow2;
                GD.Print($"Boss chose {enemyThrow2}");
            }
            else
            {
                enemyThrow = RngManager.Instance.Rng.Randf() > 0.5f ? enemyThrow : enemyThrow2;
                GD.Print($"Boss chose {enemyThrow} (random between equal results)");
            }
        }

        result.EnemyThrow = enemyThrow;
        GD.Print($"Player threw: {playerThrowData.Name} ({playerThrow}), Enemy threw: {enemyThrow}");

        // Record throws for this round
        EncounterPlayerThrows.Add(new List<Throws> { playerThrow });
        EncounterEnemyThrows.Add(new List<Throws> { enemyThrow });

        // Determine outcome - check for custom outcome first, then fall back to standard RPS
        RoundOutcome outcome;
        var customOutcome = effect.GetCustomOutcome(playerThrowData, enemyThrow);
        if (customOutcome.HasValue)
        {
            outcome = customOutcome.Value;
            GD.Print($"Custom outcome: {outcome}");
        }
        else
        {
            bool playerWon = DetermineWinner(playerThrow, enemyThrow);
            if (isBossDraw) playerWon = true;

            if (playerWon)
                outcome = RoundOutcome.PlayerWin;
            else if (playerThrow == enemyThrow)
                outcome = RoundOutcome.Draw;
            else
                outcome = RoundOutcome.EnemyWin;
        }

        // Create context for effect
        var context = new ThrowContext
        {
            Player = player,
            Enemy = enemy,
            Throw = playerThrowData,
            EnemyThrow = enemyThrow,
            EquippedThrows = player.EquippedThrows,
            InventoryThrows = player.InventoryThrows,
            PlayerThrowHistory = PlayerThrowDataHistory,
            BattleState = BattleState
        };

        // Apply effect based on outcome
        result.Outcome = outcome;
        context.Outcome = outcome;
        
        // Get the outcome multiplier
        float playerMultiplier = CombatConfig.GetPlayerMultiplier(outcome);
        float incomingMultiplier = CombatConfig.GetIncomingMultiplier(outcome);

        // Calculate stats using the new system
        ThrowStats stats = effect.CalculateStats(context, playerMultiplier);
        
        int totalDamage = stats.Damage;

        // Apply damage boost buffs
        int damageBoost = GameState.Instance.GetBuffAmount("damage_boost");
        totalDamage += damageBoost;

        // Apply vulnerable status effect on enemy
        float vulnerableMultiplier = enemy.GetDamageMultiplier();
        if (vulnerableMultiplier != 1.0f)
        {
            totalDamage = Godot.Mathf.RoundToInt(totalDamage * vulnerableMultiplier);
            GD.Print($"Vulnerable multiplier: {vulnerableMultiplier}x -> {totalDamage}");
        }

        // Momentum enemy: override damage with streak
        if (enemy.BehaviorName == "momentum")
        {
            int streak = MomentumBehavior.GetCurrentStreak(EncounterPlayerThrows, EncounterEnemyThrows);
            totalDamage = MomentumBehavior.GetStreakDamage(streak);
            GD.Print($"Momentum enemy - streak: {streak}, damage: {totalDamage}");
        }

        // Boss draw halves damage
        if (isBossDraw)
        {
            totalDamage = Godot.Mathf.Max(1, totalDamage / 2);
            GD.Print($"Boss draw - player deals half damage: {totalDamage}");
        }

        // Deal damage to enemy
        if (totalDamage > 0)
        {
            enemy.TakeDamage(totalDamage);
            GD.Print($"Player dealt {totalDamage} damage to enemy");
        }
        
        int baseIncomingDamage = enemy.strength;

        // Momentum enemy: override damage
        if (enemy.BehaviorName == "momentum")
        {
            int streak = MomentumBehavior.GetCurrentStreak(EncounterPlayerThrows, EncounterEnemyThrows);
            baseIncomingDamage = MomentumBehavior.GetStreakDamage(streak);
        }

        // Apply incoming damage multiplier based on outcome
        int incomingDamage = Godot.Mathf.RoundToInt(baseIncomingDamage * incomingMultiplier);

        // Apply effect's custom incoming damage multiplier (for special throws like Paper Cannon)
        if (stats.IncomingDamageMultiplier != 1.0f)
        {
            incomingDamage = Godot.Mathf.RoundToInt(incomingDamage * stats.IncomingDamageMultiplier);
            GD.Print($"Effect incoming multiplier: {stats.IncomingDamageMultiplier}x -> {incomingDamage}");
        }

        // Apply block (round block + persistent block)
        int totalBlock = GetTotalBlock(stats.Block);
        incomingDamage -= totalBlock;

        // Apply buff damage reduction
        int buffReduction = GameState.Instance.GetBuffAmount("damage_reduction");
        int finalDamage = Godot.Mathf.Max(0, incomingDamage - buffReduction);

        // Deal damage to player
        if (finalDamage > 0)
        {
            player.Damage(finalDamage);
            GD.Print($"Player took {finalDamage} damage (base: {baseIncomingDamage}, incoming mult: {incomingMultiplier}x, blocked: {totalBlock}, buff: {buffReduction})");
        }
        
        int totalHeal = stats.Heal;

        // Apply lifesteal
        if (stats.Lifesteal > 0 && totalDamage > 0)
        {
            int lifestealHeal = Godot.Mathf.Max(1, totalDamage * stats.Lifesteal / 100);
            totalHeal += lifestealHeal;
            GD.Print($"Lifesteal: {lifestealHeal} HP (from {totalDamage} damage)");
        }

        if (totalHeal > 0)
        {
            player.Heal(totalHeal);
            GD.Print($"Player healed for {totalHeal}");
        }

        // Set result values
        result.DamageDealt = totalDamage;  // For display purposes, show damage dealt
        result.DamageTarget = "both";       // New: both sides take damage
        result.HealAmount = totalHeal;
        result.SpecialMessage = stats.SpecialMessage;

        // Apply additional effects from the effect class
        effect.ApplyAdditionalEffects(context, stats);

        // Handle draw-specific enemy behavior
        if (outcome == RoundOutcome.Draw)
        {
            enemy.OnDraw();
        }

        // Convert stats to legacy ThrowResult for compatibility
        ThrowResult effectResult = new ThrowResult
        {
            DamageDealt = stats.Damage,
            DamageBlocked = stats.Block,
            HealAmount = totalHeal,
            IncomingDamageMultiplier = stats.IncomingDamageMultiplier,
            EnemyStatusEffects = stats.EnemyStatusEffects,
            BuffsToApply = stats.BuffsToApply,
            TransformToThrowId = stats.TransformToThrowId,
            SpecialMessage = stats.SpecialMessage
        };

        // Apply any buffs from effect
        if (effectResult.BuffsToApply != null)
        {
            foreach (var buff in effectResult.BuffsToApply)
            {
                if (buff.Target == "player")
                {
                    GameState.Instance.AddBuff(buff.BuffType, buff.Amount, buff.Duration);
                }
            }
        }

        // Apply status effects to enemy
        if (effectResult.EnemyStatusEffects != null)
        {
            foreach (var status in effectResult.EnemyStatusEffects)
            {
                enemy.ApplyStatusEffect(status.Type, status.Stacks);
            }
        }

        // Handle throw transformation
        if (!string.IsNullOrEmpty(effectResult.TransformToThrowId))
        {
            TransformThrow(playerThrowData.Id, effectResult.TransformToThrowId);
        }

        // Calculate actual damage dealt/taken for OnAfterRound callback
        // In new system, both happen so we track both
        int damageDealtToEnemy = totalDamage;
        int damageTakenByPlayer = finalDamage;

        // Call OnAfterRound callback for effects that need to react to final values
        effect.OnAfterRound(context, result.Outcome, damageDealtToEnemy, damageTakenByPlayer);

        // Update history
        player.ThrowHistory.Add(playerThrow);
        player.LastThrow = playerThrow;
        PlayerThrowDataHistory.Add(playerThrowData);

        // Process persistent block duration
        ProcessPersistentBlock();

        // Process enemy status effect durations (vulnerable, etc.)
        enemy.ProcessStatusDurations();

        // Decrement buff durations
        GameState.Instance.DecrementBuffDurations();

        return result;
    }

    // Resolve a round when enemy is trapped (player gets free attack)
    private RoundResult ResolveTrappedRound(ThrowData playerThrowData)
    {
        GD.Print("Enemy is trapped - resolving trapped round");

        // Apply any transformations for this battle
        playerThrowData = GetEffectiveThrowData(playerThrowData);

        var effect = ThrowEffectFactory.Create(playerThrowData.Effect.EffectType);
        Throws playerThrow = effect.GetThrowType(playerThrowData);

        var result = new RoundResult
        {
            PlayerThrowData = playerThrowData,
            PlayerThrow = playerThrow,
            EnemyThrow = Throws.rock, // Doesn't matter, enemy attacks trap
            Outcome = RoundOutcome.PlayerWin, // Player always wins when enemy is trapped
            DamageDealt = 0,
            DamageTarget = "enemy",
            HealAmount = 0,
            SpecialMessage = "Enemy is trapped!"
        };

        // Record throws
        EncounterPlayerThrows.Add(new List<Throws> { playerThrow });
        EncounterEnemyThrows.Add(new List<Throws> { Throws.rock });

        // Create context
        var context = new ThrowContext
        {
            Player = player,
            Enemy = enemy,
            Throw = playerThrowData,
            EnemyThrow = Throws.rock,
            Outcome = RoundOutcome.PlayerWin,
            EquippedThrows = player.EquippedThrows,
            InventoryThrows = player.InventoryThrows,
            PlayerThrowHistory = PlayerThrowDataHistory,
            BattleState = BattleState
        };

        // Player gets full win multiplier for free attack
        float playerMultiplier = CombatConfig.PlayerWinMultiplier;
        ThrowStats stats = effect.CalculateStats(context, playerMultiplier);

        // Player deals damage to enemy (free attack)
        int totalDamage = stats.Damage;
        int damageBoost = GameState.Instance.GetBuffAmount("damage_boost");
        totalDamage += damageBoost;

        float vulnerableMultiplier = enemy.GetDamageMultiplier();
        if (vulnerableMultiplier != 1.0f)
        {
            totalDamage = Godot.Mathf.RoundToInt(totalDamage * vulnerableMultiplier);
        }

        if (totalDamage > 0)
        {
            enemy.TakeDamage(totalDamage);
            GD.Print($"Player dealt {totalDamage} free damage to trapped enemy");
        }

        // Enemy attacks the trap instead of player
        int trapDamage = enemy.strength;
        bool trapBroken = enemy.DamageTrap(trapDamage);
        if (trapBroken)
        {
            result.SpecialMessage = "Enemy broke free from the trap!";
        }
        else
        {
            result.SpecialMessage = $"Enemy attacks trap! ({enemy.TrapHealth} HP remaining)";
        }

        // Player takes no damage while enemy is trapped
        // Healing still applies
        int totalHeal = stats.Heal;
        if (stats.Lifesteal > 0 && totalDamage > 0)
        {
            totalHeal += Godot.Mathf.Max(1, totalDamage * stats.Lifesteal / 100);
        }
        if (totalHeal > 0)
        {
            player.Heal(totalHeal);
        }

        result.DamageDealt = totalDamage;
        result.HealAmount = totalHeal;

        // Apply additional effects
        effect.ApplyAdditionalEffects(context, stats);

        // Apply status effects and transformations
        if (stats.EnemyStatusEffects != null)
        {
            foreach (var status in stats.EnemyStatusEffects)
            {
                enemy.ApplyStatusEffect(status.Type, status.Stacks);
            }
        }

        if (!string.IsNullOrEmpty(stats.TransformToThrowId))
        {
            TransformThrow(playerThrowData.Id, stats.TransformToThrowId);
        }

        // Update history
        player.ThrowHistory.Add(playerThrow);
        player.LastThrow = playerThrow;
        PlayerThrowDataHistory.Add(playerThrowData);

        effect.OnAfterRound(context, result.Outcome, totalDamage, 0);

        ProcessPersistentBlock();
        enemy.ProcessStatusDurations();
        GameState.Instance.DecrementBuffDurations();

        return result;
    }

    private bool DetermineWinner(Throws player, Throws enemy)
    {
        if (player == enemy) return false;
        return (player == Throws.rock && enemy == Throws.scissors)
            || (player == Throws.paper && enemy == Throws.rock)
            || (player == Throws.scissors && enemy == Throws.paper);
    }

    // Compare throws from enemy's perspective: 1=enemy wins, 0=draw, -1=enemy loses
    private int CompareThrows(Throws enemyThrow, Throws playerThrow)
    {
        if (enemyThrow == playerThrow) return 0; // Draw

        bool enemyWins = (enemyThrow == Throws.rock && playerThrow == Throws.scissors)
                      || (enemyThrow == Throws.paper && playerThrow == Throws.rock)
                      || (enemyThrow == Throws.scissors && playerThrow == Throws.paper);

        return enemyWins ? 1 : -1;
    }
}
