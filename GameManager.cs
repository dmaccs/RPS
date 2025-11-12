using Godot;
using System.Collections.Generic;
using System.Threading;

public partial class GameManager : Node
{

	public Player Player { get; private set; } = new Player();
	
	public static GameManager Instance { get; private set; }
	private int stageIndex = 0;
    private List<string> sceneOrder;

    public override void _Ready()
    {
        Instance = this;

        // Hardcoded progression
        sceneOrder = new List<string>
        {
            "res://Scenes/BattleScene.tscn",
            "res://Scenes/Rewards.tscn",
            "res://Scenes/ShopScene.tscn",
            "res://Scenes/BattleScene.tscn",
            "res://Scenes/RestScene.tscn",
            "res://Scenes/BossScene.tscn",
            "res://Scenes/VictoryScene.tscn",
            //"res://Scenes/BossScene.tscn"
        };

        LoadNextScene();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            if (Player != null)
            {
                Player.QueueFree();
                Player = null;
            }
        }
    }
    
    public void ResetGame()
    {
        // Free the old Player before creating a new one
        if (Player != null)
        {
            Player.QueueFree();
        }

        Player = new Player();
        stageIndex = 0;
        LoadNextScene();
        GameState.Instance.RefreshUI();
    }

    public void LoadNextScene()
    {
        if (stageIndex >= sceneOrder.Count)
        {
            GD.Print("Game Over or Victory!");
            CallDeferred(nameof(DeferredChangeScene), "res://Scenes/VictoryScene.tscn");
            return;
        }

        string nextScenePath = sceneOrder[stageIndex];
        stageIndex++;
        CallDeferred(nameof(DeferredChangeScene), nextScenePath);
    }

    private void DeferredChangeScene(string scenePath)
    {
        GetTree().ChangeSceneToFile(scenePath);
    }
}
