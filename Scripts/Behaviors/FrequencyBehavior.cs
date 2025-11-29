using System.Collections.Generic;
using Godot;
using Rps;

public class FrequencyBehavior : IEnemyBehavior
{
    private RandomNumberGenerator rng;
    private List<Throws> allowed;
    private List<int> frequencies;

    public FrequencyBehavior(List<Throws> allowed, List<int> frequencies, RandomNumberGenerator rng)
    {
        this.allowed = allowed ?? new List<Throws> { Throws.rock, Throws.paper, Throws.scissors };
        this.frequencies = frequencies ?? new List<int>();
        this.rng = rng ?? new RandomNumberGenerator();
    }

    public Throws ChooseThrow(Throws? previousPlayerThrow, List<Throws> playerHistory,
                              List<List<Throws>> encounterPlayerThrows = null,
                              List<List<Throws>> encounterEnemyThrows = null)
    {
        if (frequencies != null && frequencies.Count >= allowed.Count)
        {
            int total = 0;
            for (int i = 0; i < allowed.Count; i++) total += frequencies[i];
            if (total > 0)
            {
                int roll = rng.RandiRange(0, total - 1);
                int curr = 0;
                for (int i = 0; i < allowed.Count; i++)
                {
                    curr += frequencies[i];
                    if (roll < curr) return allowed[i];
                }
            }
        }
        // fallback
        if (allowed.Count == 0) return Throws.rock;
        int idx = rng.RandiRange(0, allowed.Count - 1);
        return allowed[idx];
    }
}
