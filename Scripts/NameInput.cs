using Godot;

public partial class NameInput : Control
{
	private LineEdit NameField;
	private Label ScoreLabel;

	public override void _Ready()
	{
		NameField = FindChild("LineEdit", true, false) as LineEdit;
		ScoreLabel = FindChild("ScoreLabel", true, false) as Label;

		var sm = GetNode<ScoreManager>("/root/ScoreManager");

		// 🔥 Score anzeigen
		ScoreLabel.Text = $"Dein Score: {sm.CurrentScore}";

		NameField.GrabFocus();
	}

	private void OnLineEditTextSubmitted(string text)
	{
		SaveAndExit();
	}

	private void OnButtonPressed()
	{
		SaveAndExit();
	}

	private void SaveAndExit()
	{
		string name = NameField.Text.Trim();

		if (string.IsNullOrEmpty(name))
			name = "Player";

		GetNode<ScoreManager>("/root/ScoreManager").SaveScore(name);

		GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
	}
}
