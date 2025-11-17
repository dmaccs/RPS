using Godot;

public partial class RngManager : Node
{
    public static RngManager Instance { get; private set; }
    public RandomNumberGenerator Rng { get; private set; } = new RandomNumberGenerator();

    private ulong currentSeed;

    public override void _Ready()
    {
        Instance = this;
    }

    public void SetSeed(ulong seed)
    {
        currentSeed = seed;
        Rng.Seed = seed;
        GD.Print($"RNG seeded with {seed}");
    }

    public ulong GetSeed() => currentSeed;
}
