using System.Collections.Generic;
using Godot;
using Rps;

public class CounterMostCommonBehavior : IEnemyBehavior
{
    private RandomNumberGenerator rng;
    private List<Throws> allowed;

    public CounterMostCommonBehavior(List<Throws> allowed, RandomNumberGenerator rng)
    {
        this.allowed = allowed ?? new List<Throws> { Throws.rock, Throws.paper, Throws.scissors };
        this.rng = rng ?? new RandomNumberGenerator();
    }

    public Throws ChooseThrow(Throws? previousPlayerThrow, List<Throws> playerHistory)
    {
        if (playerHistory != null && playerHistory.Count > 0)
        {
            var counts = new Dictionary<Throws, int>();
            foreach (var t in playerHistory)
            {
                if (!counts.ContainsKey(t)) counts[t] = 0;
                counts[t]++;
            }
            // find most common
            Throws most = Throws.rock;
            int best = -1;
            foreach (var kv in counts)
            {
                if (kv.Value > best)
                {
                    best = kv.Value;
                    most = kv.Key;
                }
            }
            // try to pick a throw that beats 'most'
            var counter = WinningThrowAgainst(most);
            if (allowed.Contains(counter)) return counter;
            foreach (var a in allowed)
                if (Beats(a, most)) return a;
        }

        // fallback to random allowed
        if (allowed.Count == 0) return Throws.rock;
        int idx = rng.RandiRange(0, allowed.Count - 1);
        return allowed[idx];
    }

    private bool Beats(Throws a, Throws b)
    {
        return (a == Throws.rock && b == Throws.scissors)
            || (a == Throws.paper && b == Throws.rock)
            || (a == Throws.scissors && b == Throws.paper);
    }

    private Throws WinningThrowAgainst(Throws t)
    {
        if (t == Throws.rock) return Throws.paper;
        if (t == Throws.paper) return Throws.scissors;
        if (t == Throws.scissors) return Throws.rock;
        return Throws.rock;
    }
}
