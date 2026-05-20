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
	public int CoinsThisRun = 0;
	public int TotalCoins = 0;

	public int CurrentScore = 0;
	public bool IsRunning = false;

	public const int RevivalCost = 120;
	public bool PendingRevival = false;
	public Vector2 SavedPlayerPosition = Vector2.Zero;
	public int SavedLevel = 1;
	public float SavedStress = 25f;

	private const string SavePath =
		"user://highscores.json";
	private const string CoinSavePath =
		"user://coins.save";

	public override void _Ready()
	{
		LoadTotalCoins();
	}

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
		CoinsThisRun = 0;

		CurrentScore = 0;
	}

	public void CollectCoin(int scoreValue)
	{
		CoinsThisRun++;
		TotalCoins++;
		BonusScore += scoreValue;
		SaveTotalCoins();
	}

	public void StartScoring()
	{
		IsRunning = true;
	}

	public void StopScoring()
	{
		IsRunning = false;
	}

	public void SaveDeathState(Vector2 playerPosition, int level, float stress)
	{
		SavedPlayerPosition = playerPosition;
		SavedLevel = level;
		SavedStress = stress;
		PendingRevival = false;
	}

	public bool CanAffordRevival()
	{
		return TotalCoins >= RevivalCost;
	}

	public bool TryPurchaseRevival()
	{
		if (!CanAffordRevival())
			return false;

		TotalCoins -= RevivalCost;
		SaveTotalCoins();
		PendingRevival = true;
		return true;
	}

	public void ClearRevivalState()
	{
		PendingRevival = false;
	}

	private void SaveTotalCoins()
	{
		var file = FileAccess.Open(
			CoinSavePath,
			FileAccess.ModeFlags.Write
		);

		file.StoreString(TotalCoins.ToString());
		file.Close();
	}

	private void LoadTotalCoins()
	{
		if (!FileAccess.FileExists(CoinSavePath))
		{
			TotalCoins = 0;
			return;
		}

		var file = FileAccess.Open(
			CoinSavePath,
			FileAccess.ModeFlags.Read
		);

		string content = file.GetAsText().Trim();
		file.Close();

		if (!int.TryParse(content, out TotalCoins))
			TotalCoins = 0;
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
