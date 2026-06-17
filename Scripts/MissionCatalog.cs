using System.Collections.Generic;

public enum MissionStat
{
	TotalCoinsCollected,
	BestScore,
	BestLevelReached,
	BestSurvivalSeconds,
	LifetimeGreenBoosts,
	LifetimeOverBoosts,
	LifetimeAlcoholUses,
	LifetimeChorusUses,
	LifetimeTrashCollected,
	LifetimePassiveFishBonus,
	LifetimeRuns,
	OwnedCustomSkins,
	LifetimeStartItemsPurchased,
	LifetimeExtraLivesUsed
}

public sealed class MissionDefinition
{
	public readonly string Id;
	public readonly string Title;
	public readonly string Description;
	public readonly MissionStat Stat;
	public readonly int Target;
	public readonly bool UsesSeconds;

	public MissionDefinition(string id, string title, string description, MissionStat stat, int target, bool usesSeconds = false)
	{
		Id = id;
		Title = title;
		Description = description;
		Stat = stat;
		Target = target;
		UsesSeconds = usesSeconds;
	}
}

public static class MissionCatalog
{
	public const int Reward = 100;

	public static readonly MissionDefinition[] Missions =
	{
		new MissionDefinition("coins_10", "Münzen I", "Sammle insgesamt 10 Münzen.", MissionStat.TotalCoinsCollected, 10),
		new MissionDefinition("coins_25", "Münzen II", "Sammle insgesamt 25 Münzen.", MissionStat.TotalCoinsCollected, 25),
		new MissionDefinition("coins_50", "Münzen III", "Sammle insgesamt 50 Münzen.", MissionStat.TotalCoinsCollected, 50),
		new MissionDefinition("coins_100", "Münzen IV", "Sammle insgesamt 100 Münzen.", MissionStat.TotalCoinsCollected, 100),
		new MissionDefinition("coins_250", "Münzen V", "Sammle insgesamt 250 Münzen.", MissionStat.TotalCoinsCollected, 250),
		new MissionDefinition("score_500", "Score I", "Erreiche einen Score von 500.", MissionStat.BestScore, 500),
		new MissionDefinition("score_1000", "Score II", "Erreiche einen Score von 1000.", MissionStat.BestScore, 1000),
		new MissionDefinition("score_2000", "Score III", "Erreiche einen Score von 2000.", MissionStat.BestScore, 2000),
		new MissionDefinition("score_3500", "Score IV", "Erreiche einen Score von 3500.", MissionStat.BestScore, 3500),
		new MissionDefinition("score_5000", "Score V", "Erreiche einen Score von 5000.", MissionStat.BestScore, 5000),
		new MissionDefinition("level_2", "Level II", "Erreiche Level 2.", MissionStat.BestLevelReached, 2),
		new MissionDefinition("level_3", "Level III", "Erreiche Level 3.", MissionStat.BestLevelReached, 3),
		new MissionDefinition("level_4", "Level IV", "Erreiche Level 4.", MissionStat.BestLevelReached, 4),
		new MissionDefinition("level_5", "Level V", "Erreiche Level 5.", MissionStat.BestLevelReached, 5),
		new MissionDefinition("survive_30", "Atem holen", "Überlebe 30 Sekunden.", MissionStat.BestSurvivalSeconds, 30, true),
		new MissionDefinition("survive_60", "Ruhiger Schwimmer", "Überlebe 60 Sekunden.", MissionStat.BestSurvivalSeconds, 60, true),
		new MissionDefinition("survive_120", "Tiefseetour", "Überlebe 120 Sekunden.", MissionStat.BestSurvivalSeconds, 120, true),
		new MissionDefinition("survive_180", "Langer Lauf", "Überlebe 180 Sekunden.", MissionStat.BestSurvivalSeconds, 180, true),
		new MissionDefinition("green_3", "Perfektes Timing I", "Schaffe 3 perfekte Boosts.", MissionStat.LifetimeGreenBoosts, 3),
		new MissionDefinition("green_10", "Perfektes Timing II", "Schaffe 10 perfekte Boosts.", MissionStat.LifetimeGreenBoosts, 10),
		new MissionDefinition("panic_3", "Panikschub I", "Nutze 3 rote Panik-Boosts.", MissionStat.LifetimeOverBoosts, 3),
		new MissionDefinition("panic_10", "Panikschub II", "Nutze 10 rote Panik-Boosts.", MissionStat.LifetimeOverBoosts, 10),
		new MissionDefinition("alcohol_1", "Samba Start", "Nutze einmal Alkohol.", MissionStat.LifetimeAlcoholUses, 1),
		new MissionDefinition("alcohol_5", "Partymodus", "Nutze 5-mal Alkohol.", MissionStat.LifetimeAlcoholUses, 5),
		new MissionDefinition("chorus_1", "Weg hier", "Nutze einmal eine Chorusfrucht.", MissionStat.LifetimeChorusUses, 1),
		new MissionDefinition("trash_1", "Aua Tempo", "Sammle einmal Müll ein.", MissionStat.LifetimeTrashCollected, 1),
		new MissionDefinition("fish_bonus_1", "Fisch-Bonus", "Iss betrunken einen passiven Fisch.", MissionStat.LifetimePassiveFishBonus, 1),
		new MissionDefinition("runs_3", "Wieder rein", "Starte 3 Runden.", MissionStat.LifetimeRuns, 3),
		new MissionDefinition("skin_1", "Neues Outfit", "Besitze einen Skin aus den Skinpacks.", MissionStat.OwnedCustomSkins, 1),
		new MissionDefinition("start_item_1", "Vorbereitet", "Kaufe ein Start-Item.", MissionStat.LifetimeStartItemsPurchased, 1),
	};

	public static MissionDefinition GetMission(string id)
	{
		foreach (MissionDefinition mission in Missions)
		{
			if (mission.Id == id)
				return mission;
		}

		return null;
	}
}
