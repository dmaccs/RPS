using System.Collections.Generic;
using Godot;
using Rps;

public static class BehaviorFactory
{
    // behaviorName: string from JSON (e.g. "random", "copy_last", etc.)
    public static IEnemyBehavior Create(string behaviorName, EnemyData data, RandomNumberGenerator rng)
    {
        // parse allowed throws
        var allowed = new List<Throws>();
        if (data?.allowedThrows != null)
        {
            foreach (var name in data.allowedThrows)
            {
                if (System.Enum.TryParse<Throws>(name, true, out var parsed))
                    allowed.Add(parsed);
            }
        }
        if (allowed.Count == 0)
        {
            allowed.Add(Throws.rock);
            allowed.Add(Throws.paper);
            allowed.Add(Throws.scissors);
        }

        string key = behaviorName?.ToLower();
        switch (key)
        {
            case "random":
                return new RandomBehavior(allowed, rng);
            case "copy_last":
                return new CopyLastBehavior(allowed, rng);
            case "counter_most_common":
                return new CounterMostCommonBehavior(allowed, rng);
            case "not_seen":
                return new RPS.Scripts.Behaviors.NotSeenBehavior(allowed, rng);
            case "momentum":
                return new RPS.Scripts.Behaviors.MomentumBehavior(allowed, rng);
            default:
                // default to frequency behavior if frequencies present else random
                if (data?.frequencies != null && data.frequencies.Count > 0)
                    return new FrequencyBehavior(allowed, data.frequencies, rng);
                return new RandomBehavior(allowed, rng);
        }
    }
}
