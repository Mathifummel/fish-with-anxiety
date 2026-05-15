// ===============================
// SCOREMANAGER.CS
// KOMPLETT ERSETZEN
// ===============================

using Godot;
using System.Collections.Generic;

public partial class ScoreManager : Node
{
	public int GreenBoosts = 0;
	public int OverBoosts = 0;

	public float SurvivalTime = 0f;

	public int BonusScore = 0;

	public int CurrentScore = 0;
	public bool IsRunning = false;

	private const string SavePath =
		"user://highscores.json";

	public override void _Process(double delta)
	{
		if (!IsRunning)
			return;

		SurvivalTime += (float)delta;

		CurrentScore =
			(int)(SurvivalTime * 10f) +
			(GreenBoosts * 50) +
			(OverBoosts * 20) +
			BonusScore;
	}

	public void RegisterGreenBoost()
	{
		GreenBoosts++;
	}

	public void RegisterOverBoost()
	{
		OverBoosts++;
	}

	public void Reset()
	{
		IsRunning = false;

		GreenBoosts = 0;
		OverBoosts = 0;

		SurvivalTime = 0f;

		BonusScore = 0;

		CurrentScore = 0;
	}

	public void StartScoring()
	{
		IsRunning = true;
	}

	public void StopScoring()
	{
		IsRunning = false;
	}

	// SAVE
	public void SaveScore(string playerName)
	{
		var scores = LoadScores();

		var list =
			new List<Godot.Collections.Dictionary>();

		bool replaced = false;

		foreach (var item in scores)
		{
			var dict = item.AsGodotDictionary();

			string name = dict["name"].ToString();

			int score = (int)dict["score"];

			if (name == playerName)
			{
				if (CurrentScore > score)
				{
					dict["score"] = CurrentScore;
				}

				replaced = true;
			}

			list.Add(dict);
		}

		if (!replaced)
		{
			list.Add(
				new Godot.Collections.Dictionary
				{
					{ "name", playerName },
					{ "score", CurrentScore }
				}
			);
		}

		list.Sort((a, b) =>
		{
			int sa = (int)a["score"];
			int sb = (int)b["score"];

			return sb.CompareTo(sa);
		});

		while (list.Count > 10)
			list.RemoveAt(list.Count - 1);

		var result = new Godot.Collections.Array();

		foreach (var item in list)
			result.Add(item);

		var file = FileAccess.Open(
			SavePath,
			FileAccess.ModeFlags.Write
		);

		file.StoreString(Json.Stringify(result));

		file.Close();
	}

	// LOAD
	public Godot.Collections.Array LoadScores()
	{
		if (!FileAccess.FileExists(SavePath))
		{
			return new Godot.Collections.Array();
		}

		var file = FileAccess.Open(
			SavePath,
			FileAccess.ModeFlags.Read
		);

		string content = file.GetAsText();

		if (string.IsNullOrEmpty(content))
		{
			return new Godot.Collections.Array();
		}

		var data = Json.ParseString(content);

		if (data.VariantType != Variant.Type.Array)
		{
			return new Godot.Collections.Array();
		}

		return data.AsGodotArray();
	}
}
