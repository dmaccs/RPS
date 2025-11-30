using System.Collections.Generic;
using Godot;
using Rps;
using RPS.Scripts.Behaviors;

// RoundOutcome is now defined in Scripts/Throws/IThrowEffect.cs

public class RoundResult
{
    public ThrowData PlayerThrowData { get; set; }      // NEW: Full throw data
    public Throws PlayerThrow { get; set; }              // Resolved throw type
    public Throws EnemyThrow { get; set; }
    public RoundOutcome Outcome { get; set; }
    public int DamageDealt { get; set; }
    public string DamageTarget { get; set; }             // "player" or "enemy"
    public int HealAmount { get; set; }                  // NEW: Healing from effects
    public string SpecialMessage { get; set; }           // NEW: Effect message
}

public class BattleManager
{
    private Player player;
    private Enemy enemy;

    // Track throws per round during this encounter
    // Each round is a list of throws (usually 1, but can be multiple for special abilities)
    public List<List<Throws>> EncounterPlayerThrows { get; private set; } = new List<List<Throws>>();
    public List<List<Throws>> EncounterEnemyThrows { get; private set; } = new List<List<Throws>>();

    public BattleManager(Player player, Enemy enemy)
    {
        this.player = player;
        this.enemy = enemy;
    }

    // NEW: Resolve round using ThrowData (new throw system)
    public RoundResult ResolveRound(ThrowData playerThrowData)
    {
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

        // Determine outcome
        bool playerWon = DetermineWinner(playerThrow, enemyThrow);
        if (isBossDraw) playerWon = true;

        // Create context for effect
        var context = new ThrowContext
        {
            Player = player,
            Enemy = enemy,
            Throw = playerThrowData,
            EquippedThrows = player.EquippedThrows,
            InventoryThrows = player.InventoryThrows
        };

        // Apply effect based on outcome
        ThrowResult effectResult;

        if (playerWon)
        {
            result.Outcome = RoundOutcome.PlayerWin;
            context.Outcome = RoundOutcome.PlayerWin;
            effectResult = effect.OnPlayerWin(context);

            int totalDamage = effectResult.DamageDealt;

            // Apply damage boost buffs
            int damageBoost = GameState.Instance.GetBuffAmount("damage_boost");
            totalDamage += damageBoost;

            // Momentum enemy: override damage with streak
            if (enemy.BehaviorName == "momentum")
            {
                int streak = MomentumBehavior.GetCurrentStreak(EncounterPlayerThrows, EncounterEnemyThrows);
                totalDamage = MomentumBehavior.GetStreakDamage(streak);
                GD.Print($"Momentum enemy - player streak: {streak}, damage: {totalDamage}");
            }

            // Boss draw halves damage
            if (isBossDraw)
            {
                totalDamage = Godot.Mathf.Max(1, totalDamage / 2);
                GD.Print($"Boss draw - player deals half damage: {totalDamage}");
            }

            result.DamageDealt = totalDamage;
            result.DamageTarget = "enemy";
            result.SpecialMessage = effectResult.SpecialMessage;

            GD.Print($"Player won! Damage: {totalDamage}");
            enemy.TakeDamage(totalDamage);

            // Apply healing from effect
            if (effectResult.HealAmount > 0)
            {
                player.Heal(effectResult.HealAmount);
                result.HealAmount = effectResult.HealAmount;
                GD.Print($"Player healed for {effectResult.HealAmount}");
            }
        }
        else if (playerThrow != enemyThrow)
        {
            result.Outcome = RoundOutcome.EnemyWin;
            context.Outcome = RoundOutcome.EnemyWin;
            effectResult = effect.OnPlayerLose(context);

            int incomingDamage = enemy.strength;

            // Momentum enemy: override damage with streak
            if (enemy.BehaviorName == "momentum")
            {
                int streak = MomentumBehavior.GetCurrentStreak(EncounterPlayerThrows, EncounterEnemyThrows);
                incomingDamage = MomentumBehavior.GetStreakDamage(streak);
                GD.Print($"Momentum enemy - enemy streak: {streak}, damage: {incomingDamage}");
            }

            // Apply damage reduction from effect
            incomingDamage -= effectResult.DamageBlocked;

            // Apply buff damage reduction
            int buffReduction = GameState.Instance.GetBuffAmount("damage_reduction");
            int finalDamage = Godot.Mathf.Max(0, incomingDamage - buffReduction);

            result.DamageDealt = finalDamage;
            result.DamageTarget = "player";
            result.SpecialMessage = effectResult.SpecialMessage;

            GD.Print($"Player lost! Incoming: {enemy.strength}, Blocked: {effectResult.DamageBlocked}, Buff reduction: {buffReduction}, Final: {finalDamage}");
            player.Damage(finalDamage);
        }
        else
        {
            result.Outcome = RoundOutcome.Draw;
            context.Outcome = RoundOutcome.Draw;
            effectResult = effect.OnDraw(context);
            result.SpecialMessage = effectResult.SpecialMessage;
            GD.Print("Draw - no damage");
            enemy.OnDraw();
        }

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

        // Update history
        player.ThrowHistory.Add(playerThrow);
        player.LastThrow = playerThrow;

        // Decrement buff durations
        GameState.Instance.DecrementBuffDurations();

        return result;
    }

    // DEPRECATED: Keep for backward compatibility during migration
    public RoundResult ResolveRound(Throws playerThrow)
    {
        // Try to find matching ThrowData from equipped throws
        ThrowData throwData = null;
        foreach (var equipped in player.EquippedThrows)
        {
            if (equipped == null) continue;
            var eff = ThrowEffectFactory.Create(equipped.Effect.EffectType);
            if (eff.GetThrowType(equipped) == playerThrow)
            {
                throwData = equipped;
                break;
            }
        }

        // If found, use new system
        if (throwData != null)
        {
            return ResolveRound(throwData);
        }

        // Fallback to old system for backward compatibility
        var result = new RoundResult
        {
            PlayerThrow = playerThrow,
            EnemyThrow = Throws.rock,
            Outcome = RoundOutcome.Draw,
            DamageDealt = 0,
            DamageTarget = ""
        };

        Throws enemyThrow = enemy.ChooseThrow(player.LastThrow, player.ThrowHistory, EncounterPlayerThrows, EncounterEnemyThrows);

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

            int bestResult = Godot.Mathf.Max(result1, result2);

            if (bestResult == 0)
            {
                isBossDraw = true;
                enemyThrow = result1 == 0 ? enemyThrow : enemyThrow2;
            }
            else if (result2 > result1)
            {
                enemyThrow = enemyThrow2;
            }
            else if (result1 == result2)
            {
                enemyThrow = RngManager.Instance.Rng.Randf() > 0.5f ? enemyThrow : enemyThrow2;
            }
        }

        result.EnemyThrow = enemyThrow;

        EncounterPlayerThrows.Add(new List<Throws> { playerThrow });
        EncounterEnemyThrows.Add(new List<Throws> { enemyThrow });

        bool playerWon = DetermineWinner(playerThrow, enemyThrow);
        if (isBossDraw) playerWon = true;

        if (playerWon)
        {
            var move = player.CurrentThrows.Find(m => m.Type == playerThrow);
            int baseDamage = move != null ? move.Level : 1;
            int damageBoost = GameState.Instance.GetBuffAmount("damage_boost");
            int totalDamage = baseDamage + damageBoost;

            if (enemy.BehaviorName == "momentum")
            {
                int streak = MomentumBehavior.GetCurrentStreak(EncounterPlayerThrows, EncounterEnemyThrows);
                totalDamage = MomentumBehavior.GetStreakDamage(streak);
            }

            if (isBossDraw)
            {
                totalDamage = Godot.Mathf.Max(1, totalDamage / 2);
            }

            result.Outcome = RoundOutcome.PlayerWin;
            result.DamageDealt = totalDamage;
            result.DamageTarget = "enemy";
            enemy.TakeDamage(totalDamage);
        }
        else if (playerThrow != enemyThrow)
        {
            int incomingDamage = enemy.strength;

            if (enemy.BehaviorName == "momentum")
            {
                int streak = MomentumBehavior.GetCurrentStreak(EncounterPlayerThrows, EncounterEnemyThrows);
                incomingDamage = MomentumBehavior.GetStreakDamage(streak);
            }

            int damageReduction = GameState.Instance.GetBuffAmount("damage_reduction");
            int finalDamage = Godot.Mathf.Max(0, incomingDamage - damageReduction);

            result.Outcome = RoundOutcome.EnemyWin;
            result.DamageDealt = finalDamage;
            result.DamageTarget = "player";
            player.Damage(finalDamage);
        }
        else
        {
            result.Outcome = RoundOutcome.Draw;
            enemy.OnDraw();
        }

        player.ThrowHistory.Add(playerThrow);
        player.LastThrow = playerThrow;
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
