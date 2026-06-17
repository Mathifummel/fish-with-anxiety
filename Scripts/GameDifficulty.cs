public enum GameDifficulty
{
	Easy,
	Medium,
	Hard
}

public static class GameDifficultySettings
{
	public static GameDifficulty Current = GameDifficulty.Medium;

	public static int SelectedIndex => Current switch
	{
		GameDifficulty.Easy => 0,
		GameDifficulty.Hard => 2,
		_ => 1,
	};

	public static string DisplayName => Current switch
	{
		GameDifficulty.Easy => "Leicht",
		GameDifficulty.Hard => "Hart",
		_ => "Mittel",
	};

	public static float EnemySpeedMultiplier => Current switch
	{
		GameDifficulty.Easy => 0.86f,
		GameDifficulty.Hard => 1.13f,
		_ => 1f,
	};

	public static float PassiveSpeedMultiplier => Current switch
	{
		GameDifficulty.Easy => 0.93f,
		GameDifficulty.Hard => 1.07f,
		_ => 1f,
	};

	public static float JellyfishSpeedMultiplier => Current switch
	{
		GameDifficulty.Easy => 0.88f,
		GameDifficulty.Hard => 1.12f,
		_ => 1f,
	};

	public static float StressGainMultiplier => Current switch
	{
		GameDifficulty.Easy => 0.72f,
		GameDifficulty.Hard => 1.24f,
		_ => 1f,
	};

	public static float StressDecayMultiplier => Current switch
	{
		GameDifficulty.Easy => 1.34f,
		GameDifficulty.Hard => 0.82f,
		_ => 1f,
	};

	public static float StressPressureMultiplier => Current switch
	{
		GameDifficulty.Easy => 0.78f,
		GameDifficulty.Hard => 1.16f,
		_ => 1f,
	};

	public static float SpawnCheckIntervalMultiplier => Current switch
	{
		GameDifficulty.Easy => 1.24f,
		GameDifficulty.Hard => 0.86f,
		_ => 1f,
	};

	public static float SpawnMovementStepMultiplier => Current switch
	{
		GameDifficulty.Easy => 1.1f,
		GameDifficulty.Hard => 0.9f,
		_ => 1f,
	};

	public static float MinSpawnSpacingMultiplier => Current switch
	{
		GameDifficulty.Easy => 1.12f,
		GameDifficulty.Hard => 0.92f,
		_ => 1f,
	};

	public static int NpcTargetOffset => Current switch
	{
		GameDifficulty.Easy => -2,
		GameDifficulty.Hard => 2,
		_ => 0,
	};

	public static int JellyfishTargetOffset => Current switch
	{
		GameDifficulty.Easy => -1,
		GameDifficulty.Hard => 1,
		_ => 0,
	};

	public static int ObstacleTargetOffset => Current switch
	{
		GameDifficulty.Easy => -2,
		GameDifficulty.Hard => 2,
		_ => 0,
	};

	public static void SetFromIndex(int index)
	{
		Current = index switch
		{
			0 => GameDifficulty.Easy,
			2 => GameDifficulty.Hard,
			_ => GameDifficulty.Medium,
		};
	}
}
