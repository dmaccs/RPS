using System.Collections.Generic;
using Godot;
using Rps;

public class CopyLastBehavior : IEnemyBehavior
{
    private RandomNumberGenerator rng;
    private List<Throws> allowed;

    public CopyLastBehavior(List<Throws> allowed, RandomNumberGenerator rng)
    {
        this.allowed = allowed ?? new List<Throws> { Throws.rock, Throws.paper, Throws.scissors };
        this.rng = rng ?? new RandomNumberGenerator();
    }

    public Throws ChooseThrow(Throws? previousPlayerThrow, List<Throws> playerHistory)
    {
        if (previousPlayerThrow != null && allowed.Contains(previousPlayerThrow.Value))
            return previousPlayerThrow.Value;

        // fallback to random allowed
        if (allowed.Count == 0) return Throws.rock;
        int idx = rng.RandiRange(0, allowed.Count - 1);
        return allowed[idx];
    }
}
