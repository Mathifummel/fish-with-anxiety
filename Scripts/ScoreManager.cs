using Godot;
using System;
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

	public int LifetimeCoinsCollected = 0;
	public int LifetimeGreenBoosts = 0;
	public int LifetimeOverBoosts = 0;
	public int LifetimeAlcoholUses = 0;
	public int LifetimeChorusUses = 0;
	public int LifetimeTrashCollected = 0;
	public int LifetimePassiveFishBonus = 0;
	public int LifetimeRuns = 0;
	public int LifetimeStartItemsPurchased = 0;
	public int LifetimeExtraLivesUsed = 0;
	public int BestScore = 0;
	public int BestLevelReached = 1;
	public float BestSurvivalTime = 0f;
	public string SelectedSkinId { get; private set; } = ShopCatalog.DefaultSkinId;

	private const string SavePath = "user://highscores.json";
	private const string CoinSavePath = "user://coins.save";
	private const string ProgressSavePath = "user://shop_progress.cfg";
	private const string ShopSection = "shop";
	private const string ItemsSection = "items";
	private const string MissionsSection = "missions";
	private const string StatsSection = "stats";

	private readonly HashSet<string> ownedSkins = new HashSet<string>();
	private readonly HashSet<string> claimedMissions = new HashSet<string>();
	private readonly Dictionary<StartItemKind, int> startItemInventory =
		new Dictionary<StartItemKind, int>
		{
			{ StartItemKind.ExtraHeart, 0 },
			{ StartItemKind.Alcohol, 0 },
			{ StartItemKind.ChorusFruit, 0 }
		};

	public override void _Ready()
	{
		LoadTotalCoins();
		LoadProgress();
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
		LifetimeGreenBoosts++;
		SaveProgress();
	}

	public void RegisterOverBoost()
	{
		OverBoosts++;
		LifetimeOverBoosts++;
		SaveProgress();
	}

	public void RegisterRunStarted()
	{
		LifetimeRuns++;
		SaveProgress();
	}

	public void RegisterLevelReached(int level)
	{
		if (level <= BestLevelReached)
			return;

		BestLevelReached = level;
		SaveProgress();
	}

	public void RegisterAlcoholUsed()
	{
		LifetimeAlcoholUses++;
		SaveProgress();
	}

	public void RegisterChorusUsed()
	{
		LifetimeChorusUses++;
		SaveProgress();
	}

	public void RegisterTrashCollected()
	{
		LifetimeTrashCollected++;
		SaveProgress();
	}

	public void RegisterPassiveFishBonus()
	{
		LifetimePassiveFishBonus++;
		SaveProgress();
	}

	public void RegisterExtraLifeUsed()
	{
		LifetimeExtraLivesUsed++;
		SaveProgress();
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
		LifetimeCoinsCollected++;
		BonusScore += scoreValue;
		SaveTotalCoins();
		SaveProgress();
	}

	public void AddBonusScore(int scoreValue)
	{
		BonusScore += scoreValue;
	}

	public void StartScoring()
	{
		IsRunning = true;
	}

	public void StopScoring()
	{
		RecordRunProgress(BestLevelReached);
		IsRunning = false;
	}

	public void SaveDeathState(Vector2 playerPosition, int level, float stress)
	{
		SavedPlayerPosition = playerPosition;
		SavedLevel = level;
		SavedStress = stress;
		PendingRevival = false;
		RecordRunProgress(level);
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

	public bool IsSkinOwned(string skinId)
	{
		return ownedSkins.Contains(skinId);
	}

	public int GetOwnedCustomSkinCount()
	{
		int count = 0;
		foreach (string skinId in ownedSkins)
		{
			if (skinId != ShopCatalog.DefaultSkinId)
				count++;
		}

		return count;
	}

	public bool TryPurchaseSkin(string skinId)
	{
		SkinDefinition skin = ShopCatalog.GetSkin(skinId);
		if (skin == null ||
			skin.Id == ShopCatalog.DefaultSkinId ||
			IsSkinOwned(skin.Id) ||
			TotalCoins < skin.Price)
		{
			return false;
		}

		TotalCoins -= skin.Price;
		ownedSkins.Add(skin.Id);
		SaveTotalCoins();
		SaveProgress();
		return true;
	}

	public bool SelectSkin(string skinId)
	{
		if (!ShopCatalog.IsKnownSkin(skinId) || !IsSkinOwned(skinId))
			return false;

		SelectedSkinId = skinId;
		SaveProgress();
		return true;
	}

	public int GetStartItemCount(StartItemKind kind)
	{
		EnsureStartItemKey(kind);
		return startItemInventory[kind];
	}

	public bool TryPurchaseStartItem(StartItemKind kind)
	{
		StartItemDefinition item = ShopCatalog.GetStartItem(kind);
		if (TotalCoins < item.Price)
			return false;

		EnsureStartItemKey(kind);
		TotalCoins -= item.Price;
		startItemInventory[kind]++;
		LifetimeStartItemsPurchased++;
		SaveTotalCoins();
		SaveProgress();
		return true;
	}

	public bool TryConsumeStartItem(StartItemKind kind)
	{
		EnsureStartItemKey(kind);
		if (startItemInventory[kind] <= 0)
			return false;

		startItemInventory[kind]--;
		SaveProgress();
		return true;
	}

	public bool IsMissionClaimed(string missionId)
	{
		return claimedMissions.Contains(missionId);
	}

	public int GetMissionProgress(MissionDefinition mission)
	{
		if (mission == null)
			return 0;

		return mission.Stat switch
		{
			MissionStat.TotalCoinsCollected => LifetimeCoinsCollected,
			MissionStat.BestScore => BestScore,
			MissionStat.BestLevelReached => BestLevelReached,
			MissionStat.BestSurvivalSeconds => Mathf.FloorToInt(BestSurvivalTime),
			MissionStat.LifetimeGreenBoosts => LifetimeGreenBoosts,
			MissionStat.LifetimeOverBoosts => LifetimeOverBoosts,
			MissionStat.LifetimeAlcoholUses => LifetimeAlcoholUses,
			MissionStat.LifetimeChorusUses => LifetimeChorusUses,
			MissionStat.LifetimeTrashCollected => LifetimeTrashCollected,
			MissionStat.LifetimePassiveFishBonus => LifetimePassiveFishBonus,
			MissionStat.LifetimeRuns => LifetimeRuns,
			MissionStat.OwnedCustomSkins => GetOwnedCustomSkinCount(),
			MissionStat.LifetimeStartItemsPurchased => LifetimeStartItemsPurchased,
			MissionStat.LifetimeExtraLivesUsed => LifetimeExtraLivesUsed,
			_ => 0
		};
	}

	public bool CanClaimMission(MissionDefinition mission)
	{
		return mission != null &&
			!IsMissionClaimed(mission.Id) &&
			GetMissionProgress(mission) >= mission.Target;
	}

	public bool TryClaimMission(string missionId)
	{
		MissionDefinition mission = MissionCatalog.GetMission(missionId);
		if (!CanClaimMission(mission))
			return false;

		claimedMissions.Add(mission.Id);
		TotalCoins += MissionCatalog.Reward;
		SaveTotalCoins();
		SaveProgress();
		return true;
	}

	public void ResetAllProgress()
	{
		Reset();
		TotalCoins = 0;
		PendingRevival = false;
		SavedPlayerPosition = Vector2.Zero;
		SavedLevel = 1;
		SavedStress = 25f;
		ResetShopProgress();

		DeleteUserFile(SavePath);
		DeleteUserFile(CoinSavePath);
		DeleteUserFile(ProgressSavePath);
	}

	private void RecordRunProgress(int level)
	{
		bool changed = false;

		if (CurrentScore > BestScore)
		{
			BestScore = CurrentScore;
			changed = true;
		}

		if (SurvivalTime > BestSurvivalTime)
		{
			BestSurvivalTime = SurvivalTime;
			changed = true;
		}

		if (level > BestLevelReached)
		{
			BestLevelReached = level;
			changed = true;
		}

		if (changed)
			SaveProgress();
	}

	private void ResetShopProgress()
	{
		LifetimeCoinsCollected = 0;
		LifetimeGreenBoosts = 0;
		LifetimeOverBoosts = 0;
		LifetimeAlcoholUses = 0;
		LifetimeChorusUses = 0;
		LifetimeTrashCollected = 0;
		LifetimePassiveFishBonus = 0;
		LifetimeRuns = 0;
		LifetimeStartItemsPurchased = 0;
		LifetimeExtraLivesUsed = 0;
		BestScore = 0;
		BestLevelReached = 1;
		BestSurvivalTime = 0f;
		SelectedSkinId = ShopCatalog.DefaultSkinId;

		ownedSkins.Clear();
		ownedSkins.Add(ShopCatalog.DefaultSkinId);
		claimedMissions.Clear();

		foreach (StartItemKind kind in Enum.GetValues(typeof(StartItemKind)))
			startItemInventory[kind] = 0;
	}

	private void EnsureStartItemKey(StartItemKind kind)
	{
		if (!startItemInventory.ContainsKey(kind))
			startItemInventory[kind] = 0;
	}

	private void DeleteUserFile(string path)
	{
		if (!FileAccess.FileExists(path))
			return;

		Error error = DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));

		if (error != Error.Ok)
			GD.PushWarning($"Konnte Speicherdatei nicht loeschen: {path}");
	}

	private void SaveTotalCoins()
	{
		var file = FileAccess.Open(CoinSavePath, FileAccess.ModeFlags.Write);
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

		var file = FileAccess.Open(CoinSavePath, FileAccess.ModeFlags.Read);
		string content = file.GetAsText().Trim();
		file.Close();

		if (!int.TryParse(content, out TotalCoins))
			TotalCoins = 0;
	}

	private void SaveProgress()
	{
		ConfigFile config = new ConfigFile();

		config.SetValue(ShopSection, "selected_skin", SelectedSkinId);
		config.SetValue(ShopSection, "owned_skins", string.Join(",", ownedSkins));

		foreach (StartItemKind kind in Enum.GetValues(typeof(StartItemKind)))
		{
			EnsureStartItemKey(kind);
			config.SetValue(ItemsSection, kind.ToString(), startItemInventory[kind]);
		}

		config.SetValue(MissionsSection, "claimed", string.Join(",", claimedMissions));

		config.SetValue(StatsSection, "lifetime_coins_collected", LifetimeCoinsCollected);
		config.SetValue(StatsSection, "lifetime_green_boosts", LifetimeGreenBoosts);
		config.SetValue(StatsSection, "lifetime_over_boosts", LifetimeOverBoosts);
		config.SetValue(StatsSection, "lifetime_alcohol_uses", LifetimeAlcoholUses);
		config.SetValue(StatsSection, "lifetime_chorus_uses", LifetimeChorusUses);
		config.SetValue(StatsSection, "lifetime_trash_collected", LifetimeTrashCollected);
		config.SetValue(StatsSection, "lifetime_passive_fish_bonus", LifetimePassiveFishBonus);
		config.SetValue(StatsSection, "lifetime_runs", LifetimeRuns);
		config.SetValue(StatsSection, "lifetime_start_items_purchased", LifetimeStartItemsPurchased);
		config.SetValue(StatsSection, "lifetime_extra_lives_used", LifetimeExtraLivesUsed);
		config.SetValue(StatsSection, "best_score", BestScore);
		config.SetValue(StatsSection, "best_level_reached", BestLevelReached);
		config.SetValue(StatsSection, "best_survival_time", BestSurvivalTime);

		Error error = config.Save(ProgressSavePath);
		if (error != Error.Ok)
			GD.PushWarning($"Konnte Shop-Fortschritt nicht speichern: {error}");
	}

	private void LoadProgress()
	{
		ResetShopProgress();

		ConfigFile config = new ConfigFile();
		if (config.Load(ProgressSavePath) != Error.Ok)
		{
			LifetimeCoinsCollected = Mathf.Max(LifetimeCoinsCollected, TotalCoins);
			return;
		}

		string owned = config.GetValue(ShopSection, "owned_skins", ShopCatalog.DefaultSkinId).AsString();
		foreach (string skinId in SplitList(owned))
		{
			if (ShopCatalog.IsKnownSkin(skinId))
				ownedSkins.Add(skinId);
		}

		ownedSkins.Add(ShopCatalog.DefaultSkinId);
		string selected = config.GetValue(ShopSection, "selected_skin", ShopCatalog.DefaultSkinId).AsString();
		SelectedSkinId = ownedSkins.Contains(selected) && ShopCatalog.IsKnownSkin(selected)
			? selected
			: ShopCatalog.DefaultSkinId;

		foreach (StartItemKind kind in Enum.GetValues(typeof(StartItemKind)))
		{
			startItemInventory[kind] = config
				.GetValue(ItemsSection, kind.ToString(), 0)
				.AsInt32();
		}

		string claimed = config.GetValue(MissionsSection, "claimed", "").AsString();
		foreach (string missionId in SplitList(claimed))
		{
			if (MissionCatalog.GetMission(missionId) != null)
				claimedMissions.Add(missionId);
		}

		LifetimeCoinsCollected = config.GetValue(StatsSection, "lifetime_coins_collected", TotalCoins).AsInt32();
		LifetimeCoinsCollected = Mathf.Max(LifetimeCoinsCollected, TotalCoins);
		LifetimeGreenBoosts = config.GetValue(StatsSection, "lifetime_green_boosts", 0).AsInt32();
		LifetimeOverBoosts = config.GetValue(StatsSection, "lifetime_over_boosts", 0).AsInt32();
		LifetimeAlcoholUses = config.GetValue(StatsSection, "lifetime_alcohol_uses", 0).AsInt32();
		LifetimeChorusUses = config.GetValue(StatsSection, "lifetime_chorus_uses", 0).AsInt32();
		LifetimeTrashCollected = config.GetValue(StatsSection, "lifetime_trash_collected", 0).AsInt32();
		LifetimePassiveFishBonus = config.GetValue(StatsSection, "lifetime_passive_fish_bonus", 0).AsInt32();
		LifetimeRuns = config.GetValue(StatsSection, "lifetime_runs", 0).AsInt32();
		LifetimeStartItemsPurchased = config.GetValue(StatsSection, "lifetime_start_items_purchased", 0).AsInt32();
		LifetimeExtraLivesUsed = config.GetValue(StatsSection, "lifetime_extra_lives_used", 0).AsInt32();
		BestScore = config.GetValue(StatsSection, "best_score", 0).AsInt32();
		BestLevelReached = config.GetValue(StatsSection, "best_level_reached", 1).AsInt32();
		BestSurvivalTime = (float)config.GetValue(StatsSection, "best_survival_time", 0f).AsDouble();
	}

	private static string[] SplitList(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Array.Empty<string>();

		string[] raw = value.Split(',');
		List<string> result = new List<string>();

		foreach (string entry in raw)
		{
			string trimmed = entry.Trim();
			if (!string.IsNullOrEmpty(trimmed))
				result.Add(trimmed);
		}

		return result.ToArray();
	}

	public void SaveScore(string playerName)
	{
		RecordRunProgress(SavedLevel);
		var scores = LoadScores();

		var list = new List<Godot.Collections.Dictionary>();
		bool replaced = false;

		foreach (var item in scores)
		{
			var dict = item.AsGodotDictionary();
			string name = dict["name"].ToString();
			int score = (int)dict["score"];

			if (name == playerName)
			{
				if (CurrentScore > score)
					dict["score"] = CurrentScore;

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

		var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
		file.StoreString(Json.Stringify(result));
		file.Close();
	}

	public Godot.Collections.Array LoadScores()
	{
		if (!FileAccess.FileExists(SavePath))
			return new Godot.Collections.Array();

		var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
		string content = file.GetAsText();
		file.Close();

		if (string.IsNullOrEmpty(content))
			return new Godot.Collections.Array();

		var data = Json.ParseString(content);

		if (data.VariantType != Variant.Type.Array)
			return new Godot.Collections.Array();

		return data.AsGodotArray();
	}
}
