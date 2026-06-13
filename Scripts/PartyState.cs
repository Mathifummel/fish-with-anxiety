public static class PartyState
{
	public enum GameSelection
	{
		Party,
		Catch,
		Coins,
		Cops,
		DrunkRun
	}

	public static int Rounds = 3;
	public static NPCFish.EnemySkin OpponentSkin = NPCFish.EnemySkin.Gegnerfisch;
	public static GameSelection SelectedGame = GameSelection.Party;

	public static void Reset()
	{
		Rounds = 3;
		OpponentSkin = NPCFish.EnemySkin.Gegnerfisch;
		SelectedGame = GameSelection.Party;
	}
}
