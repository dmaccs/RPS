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
    public int DamageTaken { get; set; }
    public int HealAmount { get; set; }
    public string SpecialMessage { get; set; }
}

public class BattleManager
{
    private Player player;
    private Enemy enemy;

    public List<List<Throws>> EncounterPlayerThrows { get; private set; } = new List<List<Throws>>();
    public List<List<Throws>> EncounterEnemyThrows { get; private set; } = new List<List<Throws>>();
    public Dictionary<string, object> BattleState { get; private set; } = new Dictionary<string, object>();
    public List<ThrowData> PlayerThrowDataHistory { get; private set; } = new List<ThrowData>();

    private Dictionary<string, string> transformedThrows = new Dictionary<string, string>();
    private int persistentBlock = 0;
    private int persistentBlockDuration = 0;

    public BattleManager(Player player, Enemy enemy)
    {
        this.player = player;
        this.enemy = enemy;
    }

    public void AddPersistentBlock(int amount, int duration)
    {
        persistentBlock += amount;
        persistentBlockDuration = Godot.Mathf.Max(persistentBlockDuration, duration);
    }

    public ThrowData GetEffectiveThrowData(ThrowData original)
    {
        if (original == null) return null;

        if (transformedThrows.TryGetValue(original.Id, out string replacementId))
        {
            var replacement = ThrowDatabase.Instance.CreateInstance(replacementId);
            if (replacement != null)
            {
                foreach (var kvp in original.State)
                    replacement.State[kvp.Key] = kvp.Value;
                return replacement;
            }
        }
        return original;
    }

    public void TransformThrow(string originalId, string newId)
    {
        transformedThrows[originalId] = newId;
    }

    public RoundResult ResolveRound(ThrowData playerThrowData)
    {
        enemy.ProcessStatusEffects();

        if (enemy.IsTrapped)
            return ResolveTrappedRound(playerThrowData);

        playerThrowData = GetEffectiveThrowData(playerThrowData);
        var effect = ThrowEffectFactory.Create(playerThrowData.Effect.EffectType);
        Throws playerThrow = effect.GetThrowType(playerThrowData);

        // Get enemy throw (boss throws twice and picks best)
        Throws enemyThrow;
        bool isBossDraw;
        (enemyThrow, isBossDraw) = GetEnemyThrow(playerThrow);

        RecordThrows(playerThrow, enemyThrow);

        // Determine outcome
        RoundOutcome outcome = DetermineOutcome(effect, playerThrowData, playerThrow, enemyThrow, isBossDraw);
        var context = CreateContext(playerThrowData, enemyThrow, outcome);

        // Calculate and apply combat
        float playerMultiplier = CombatConfig.GetPlayerMultiplier(outcome);
        ThrowStats stats = effect.CalculateStats(context, playerMultiplier);

        int damageDealt = CalculateAndDealPlayerDamage(stats, isBossDraw);
        int damageTaken = CalculateAndDealIncomingDamage(stats, outcome);
        int healAmount = ApplyHealing(stats, damageDealt);

        // Apply effects
        effect.ApplyAdditionalEffects(context, stats);
        ApplyStatusEffectsAndBuffs(stats, playerThrowData.Id);

        if (outcome == RoundOutcome.Draw)
            enemy.OnDraw();

        effect.OnAfterRound(context, outcome, damageDealt, damageTaken);
        FinalizeRound(playerThrow, playerThrowData);

        return new RoundResult
        {
            PlayerThrowData = playerThrowData,
            PlayerThrow = playerThrow,
            EnemyThrow = enemyThrow,
            Outcome = outcome,
            DamageDealt = damageDealt,
            DamageTaken = damageTaken,
            HealAmount = healAmount,
            SpecialMessage = stats.SpecialMessage
        };
    }

    private RoundResult ResolveTrappedRound(ThrowData playerThrowData)
    {
        playerThrowData = GetEffectiveThrowData(playerThrowData);
        var effect = ThrowEffectFactory.Create(playerThrowData.Effect.EffectType);
        Throws playerThrow = effect.GetThrowType(playerThrowData);

        RecordThrows(playerThrow, Throws.rock);

        var context = CreateContext(playerThrowData, Throws.rock, RoundOutcome.PlayerWin);
        ThrowStats stats = effect.CalculateStats(context, CombatConfig.PlayerWinMultiplier);

        int damageDealt = CalculateAndDealPlayerDamage(stats, false);

        // Enemy attacks trap instead of player
        bool trapBroken = enemy.DamageTrap(enemy.strength);
        string message = trapBroken
            ? "Enemy broke free from the trap!"
            : $"Enemy attacks trap! ({enemy.TrapHealth} HP remaining)";

        int healAmount = ApplyHealing(stats, damageDealt);

        effect.ApplyAdditionalEffects(context, stats);
        ApplyStatusEffectsAndBuffs(stats, playerThrowData.Id);
        effect.OnAfterRound(context, RoundOutcome.PlayerWin, damageDealt, 0);
        FinalizeRound(playerThrow, playerThrowData);

        return new RoundResult
        {
            PlayerThrowData = playerThrowData,
            PlayerThrow = playerThrow,
            EnemyThrow = Throws.rock,
            Outcome = RoundOutcome.PlayerWin,
            DamageDealt = damageDealt,
            DamageTaken = 0,
            HealAmount = healAmount,
            SpecialMessage = message
        };
    }

    private (Throws throw_, bool isBossDraw) GetEnemyThrow(Throws playerThrow)
    {
        Throws enemyThrow = enemy.ChooseThrow(player.LastThrow, player.ThrowHistory, EncounterPlayerThrows, EncounterEnemyThrows);
        bool isBossDraw = false;

        if (!enemy.isBoss)
            return (enemyThrow, false);

        // Boss throws twice and picks best result
        Throws enemyThrow2 = enemyThrow;
        for (int i = 0; i < 10 && enemyThrow2 == enemyThrow; i++)
            enemyThrow2 = enemy.ChooseThrow(player.LastThrow, player.ThrowHistory, EncounterPlayerThrows, EncounterEnemyThrows);

        int result1 = CompareThrows(enemyThrow, playerThrow);
        int result2 = CompareThrows(enemyThrow2, playerThrow);
        int bestResult = Godot.Mathf.Max(result1, result2);

        if (bestResult == 0)
        {
            isBossDraw = true;
            return (result1 == 0 ? enemyThrow : enemyThrow2, true);
        }

        if (result1 > result2) return (enemyThrow, false);
        if (result2 > result1) return (enemyThrow2, false);
        return (RngManager.Instance.Rng.Randf() > 0.5f ? enemyThrow : enemyThrow2, false);
    }

    private RoundOutcome DetermineOutcome(IThrowEffect effect, ThrowData throwData, Throws playerThrow, Throws enemyThrow, bool isBossDraw)
    {
        var customOutcome = effect.GetCustomOutcome(throwData, enemyThrow);
        if (customOutcome.HasValue)
            return customOutcome.Value;

        bool playerWon = DetermineWinner(playerThrow, enemyThrow) || isBossDraw;

        if (playerWon) return RoundOutcome.PlayerWin;
        if (playerThrow == enemyThrow) return RoundOutcome.Draw;
        return RoundOutcome.EnemyWin;
    }

    private ThrowContext CreateContext(ThrowData throwData, Throws enemyThrow, RoundOutcome outcome)
    {
        return new ThrowContext
        {
            Player = player,
            Enemy = enemy,
            Throw = throwData,
            EnemyThrow = enemyThrow,
            Outcome = outcome,
            EquippedThrows = player.EquippedThrows,
            InventoryThrows = player.InventoryThrows,
            PlayerThrowHistory = PlayerThrowDataHistory,
            BattleState = BattleState
        };
    }

    private int CalculateAndDealPlayerDamage(ThrowStats stats, bool isBossDraw)
    {
        int damage = stats.Damage + GameState.Instance.GetBuffAmount("damage_boost");

        float vulnerableMultiplier = enemy.GetDamageMultiplier();
        if (vulnerableMultiplier != 1.0f)
            damage = Godot.Mathf.RoundToInt(damage * vulnerableMultiplier);

        if (enemy.BehaviorName == "momentum")
            damage = MomentumBehavior.GetStreakDamage(MomentumBehavior.GetCurrentStreak(EncounterPlayerThrows, EncounterEnemyThrows));

        if (isBossDraw)
            damage = Godot.Mathf.Max(1, damage / 2);

        if (damage > 0)
            enemy.TakeDamage(damage);

        return damage;
    }

    private int CalculateAndDealIncomingDamage(ThrowStats stats, RoundOutcome outcome)
    {
        int baseDamage = enemy.strength;

        if (enemy.BehaviorName == "momentum")
            baseDamage = MomentumBehavior.GetStreakDamage(MomentumBehavior.GetCurrentStreak(EncounterPlayerThrows, EncounterEnemyThrows));

        float incomingMultiplier = CombatConfig.GetIncomingMultiplier(outcome);
        int damage = Godot.Mathf.RoundToInt(baseDamage * incomingMultiplier);

        if (stats.IncomingDamageMultiplier != 1.0f)
            damage = Godot.Mathf.RoundToInt(damage * stats.IncomingDamageMultiplier);

        damage -= (stats.Block + persistentBlock);
        damage -= GameState.Instance.GetBuffAmount("damage_reduction");
        damage = Godot.Mathf.Max(0, damage);

        if (damage > 0)
            player.Damage(damage);

        return damage;
    }

    private int ApplyHealing(ThrowStats stats, int damageDealt)
    {
        int heal = stats.Heal;

        if (stats.Lifesteal > 0 && damageDealt > 0)
            heal += Godot.Mathf.Max(1, damageDealt * stats.Lifesteal / 100);

        if (heal > 0)
            player.Heal(heal);

        return heal;
    }

    private void ApplyStatusEffectsAndBuffs(ThrowStats stats, string throwId)
    {
        if (stats.BuffsToApply != null)
        {
            foreach (var buff in stats.BuffsToApply)
            {
                if (buff.Target == "player")
                    GameState.Instance.AddBuff(buff.BuffType, buff.Amount, buff.Duration);
            }
        }

        if (stats.EnemyStatusEffects != null)
        {
            foreach (var status in stats.EnemyStatusEffects)
                enemy.ApplyStatusEffect(status.Type, status.Stacks);
        }

        if (!string.IsNullOrEmpty(stats.TransformToThrowId))
            TransformThrow(throwId, stats.TransformToThrowId);
    }

    private void RecordThrows(Throws playerThrow, Throws enemyThrow)
    {
        EncounterPlayerThrows.Add(new List<Throws> { playerThrow });
        EncounterEnemyThrows.Add(new List<Throws> { enemyThrow });
    }

    private void FinalizeRound(Throws playerThrow, ThrowData throwData)
    {
        player.ThrowHistory.Add(playerThrow);
        player.LastThrow = playerThrow;
        PlayerThrowDataHistory.Add(throwData);

        // Process durations
        if (persistentBlockDuration > 0 && --persistentBlockDuration <= 0)
            persistentBlock = 0;

        enemy.ProcessStatusDurations();
        GameState.Instance.DecrementBuffDurations();
    }

    private bool DetermineWinner(Throws player, Throws enemy)
    {
        if (player == enemy) return false;
        return (player == Throws.rock && enemy == Throws.scissors)
            || (player == Throws.paper && enemy == Throws.rock)
            || (player == Throws.scissors && enemy == Throws.paper);
    }

    private int CompareThrows(Throws enemyThrow, Throws playerThrow)
    {
        if (enemyThrow == playerThrow) return 0;
        bool enemyWins = (enemyThrow == Throws.rock && playerThrow == Throws.scissors)
                      || (enemyThrow == Throws.paper && playerThrow == Throws.rock)
                      || (enemyThrow == Throws.scissors && playerThrow == Throws.paper);
        return enemyWins ? 1 : -1;
    }
}
