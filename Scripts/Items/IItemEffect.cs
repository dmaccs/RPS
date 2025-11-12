using Godot;

public interface IItemEffect
{
    // Apply the item effect in battle context
    // player: The player object
    // enemy: The current enemy
    // effectData: The effect data from ItemData
    void Apply(Player player, Enemy enemy, ItemEffectData effectData);
}
