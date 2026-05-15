using Godot;

public partial class MainMenu : Control
{
	public override void _Ready()
	{
		// Optional: Debug
		GD.Print("Main Menu geladen");
	}

	// START BUTTON
	private void _on_button_pressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/main.tscn");
	}

	// OPTIONS BUTTON
	private void OnOptionsButtonPressed()
	{
		GD.Print("Optionen gedrückt");
		// später kannst du hier ein Options-Menü laden
	}
	// 🏆 LEADERBOARD
	private void OnLeaderboardPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/leaderboard.tscn");
	}
	
	// QUIT BUTTON
	private void OnQuitButtonPressed()
	{
		GetTree().Quit();
	}
}
