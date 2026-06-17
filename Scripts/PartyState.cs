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

	public enum OpponentSelection
	{
		EnemyOne,
		EnemyTwo,
		Jellyfish
	}

	public static int Rounds = 3;
	public static NPCFish.EnemySkin OpponentSkin = NPCFish.EnemySkin.Gegnerfisch;
	public static string PlayerSkinId = ShopCatalog.DefaultSkinId;
	public static OpponentSelection Opponent = OpponentSelection.EnemyOne;
	public static GameSelection SelectedGame = GameSelection.Party;

	public static void Reset()
	{
		Rounds = 3;
		OpponentSkin = NPCFish.EnemySkin.Gegnerfisch;
		PlayerSkinId = ShopCatalog.DefaultSkinId;
		Opponent = OpponentSelection.EnemyOne;
		SelectedGame = GameSelection.Party;
	}
}
