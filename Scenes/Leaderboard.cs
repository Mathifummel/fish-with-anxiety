using Godot;

public partial class Leaderboard : Control
{
	private VBoxContainer TopContainer;
	private VBoxContainer RecentContainer;

	public override void _Ready()
	{
		TopContainer = FindChild("TopContainer", true, false) as VBoxContainer;
		RecentContainer = FindChild("RecentContainer", true, false) as VBoxContainer;

		LoadLeaderboard();
	}
	private void OnBackButtonPressed()
	{
	
		GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
	
	
	}

	private void LoadLeaderboard()
	{
		var sm = GetNode<ScoreManager>("/root/ScoreManager");
		var scores = sm.LoadScores();

		int rank = 1;

		foreach (var item in scores)
		{
			var dict = item.AsGodotDictionary();

			string name = dict["name"].ToString();
			int score = (int)dict["score"];

			Label entry = new Label();
			entry.Text = $"{rank}. {name} - {score}";

			TopContainer.AddChild(entry);

			rank++;
		}

		// 🔥 LETZTE 5 SPIELER
		int count = scores.Count;

		for (int i = Mathf.Max(0, count - 5); i < count; i++)
		{
			var dict = scores[i].AsGodotDictionary();

			Label entry = new Label();
			entry.Text = $"{dict["name"]} - {dict["score"]}";

			RecentContainer.AddChild(entry);
		}
	}
}
