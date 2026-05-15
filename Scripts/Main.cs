using Godot;
using System.Collections.Generic;

public partial class Main : Node2D
{
	[Export] public PlayerFish Player;

	// EXISTING NPCS (aggressive)
	[Export] public Node2D NPCContainer;
	[Export] public PackedScene NPCFishScene;

	// NEW PASSIVE FISH
	[Export] public Node2D PassiveFishContainer;
	[Export] public PackedScene PassiveFishScene;

	// NEW COINS
	[Export] public Node2D CoinContainer;
	[Export] public PackedScene CoinScene;

	// NEW OBSTACLES
	[Export] public Node2D ObstacleContainer;
	[Export] public PackedScene ObstacleScene;

	// STRESS
	[Export] public float DangerRadius = 400f;
	[Export] public float CalmRadius = 650f;

	[Export] public float StressGain = 140f;
	[Export] public float StressDecayFar = 20f;
	[Export] public float StressDecayMid = 8f;

	[Export] public float ContactRadius = 55f;
	[Export] public float ContactStressBonus = 220f;

	[Export] public float PassiveDangerRadius = 240f;
	[Export] public float PassiveContactRadius = 36f;
	[Export] public float PassiveStressWeight = 0.32f;
	[Export] public float StressTargetPerThreat = 58f;
	[Export] public float ContactStressTargetBonus = 28f;

	[Export] public float ActivationThreshold = 0.25f;

	// COUNTDOWN
	[Export] public float CountdownDuration = 3f;

	// WORLD STREAMING
	[Export] public float MinSpawnDistance = 760f;
	[Export] public float MaxSpawnDistance = 1450f;
	[Export] public float DespawnDistance = 1900f;
	[Export] public float MinSpawnSpacing = 170f;
	[Export] public int MaxSpawnAttempts = 80;
	[Export] public float SpawnCheckInterval = 0.35f;
	[Export] public float SpawnMovementStep = 260f;

	[Export] public int InitialNPCCount = 2;
	[Export] public int InitialPassiveFishCount = 5;
	[Export] public int InitialCoinCount = 6;
	[Export] public int InitialObstacleCount = 4;

	[Export] public int TargetNPCCount = 7;
	[Export] public int TargetPassiveFishCount = 14;
	[Export] public int TargetCoinCount = 18;
	[Export] public int TargetObstacleCount = 10;

	private ProgressBar StressBar;
	private Label ScoreLabel;
	private Label CountdownLabel;

	private float Stress = 0f;
	private float countdownTimer = 0f;
	private bool gameStarted = false;
	private float spawnCheckTimer = 0f;
	private Vector2 lastStreamPosition = Vector2.Zero;

	private StyleBoxFlat stressFillStyle;
	private StyleBoxFlat stressBackgroundStyle;

	public override void _Ready()
	{
		StressBar = GetNode<ProgressBar>("UI/StressBar");
		ScoreLabel = GetNode<Label>("UI/ScoreLabel");
		SetupUi();
		CreateCountdownLabel();

		var sm = GetNode<ScoreManager>("/root/ScoreManager");
		sm.Reset();

		countdownTimer = CountdownDuration;
		lastStreamPosition = Player.Position;
		Player.SetPhysicsProcess(false);

		// OBSTACLES
		for (int i = 0; i < InitialObstacleCount; i++)
			SpawnObstacle();

		// AGGRESSIVE NPCS
		for (int i = 0; i < InitialNPCCount; i++)
			SpawnNPC();

		// PASSIVE FISH
		for (int i = 0; i < InitialPassiveFishCount; i++)
			SpawnPassiveFish();

		// COINS
		for (int i = 0; i < InitialCoinCount; i++)
			SpawnCoin();
	}

	private void SetupUi()
	{
		StressBar.MinValue = 0;
		StressBar.MaxValue = 100;
		StressBar.Value = 0;
		StressBar.ShowPercentage = false;
		StressBar.CustomMinimumSize = new Vector2(340, 22);
		StressBar.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		StressBar.OffsetLeft = 24;
		StressBar.OffsetTop = 22;
		StressBar.OffsetRight = 364;
		StressBar.OffsetBottom = 44;

		stressBackgroundStyle = new StyleBoxFlat();
		stressBackgroundStyle.BgColor = new Color(0.04f, 0.09f, 0.13f, 0.82f);
		stressBackgroundStyle.CornerRadiusTopLeft = 7;
		stressBackgroundStyle.CornerRadiusTopRight = 7;
		stressBackgroundStyle.CornerRadiusBottomLeft = 7;
		stressBackgroundStyle.CornerRadiusBottomRight = 7;
		stressBackgroundStyle.BorderWidthLeft = 1;
		stressBackgroundStyle.BorderWidthTop = 1;
		stressBackgroundStyle.BorderWidthRight = 1;
		stressBackgroundStyle.BorderWidthBottom = 1;
		stressBackgroundStyle.BorderColor = new Color(0.55f, 0.78f, 0.86f, 0.35f);

		stressFillStyle = new StyleBoxFlat();
		stressFillStyle.CornerRadiusTopLeft = 6;
		stressFillStyle.CornerRadiusTopRight = 6;
		stressFillStyle.CornerRadiusBottomLeft = 6;
		stressFillStyle.CornerRadiusBottomRight = 6;

		StressBar.AddThemeStyleboxOverride("background", stressBackgroundStyle);
		StressBar.AddThemeStyleboxOverride("fill", stressFillStyle);

		ScoreLabel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		ScoreLabel.OffsetLeft = -210;
		ScoreLabel.OffsetTop = 18;
		ScoreLabel.OffsetRight = -24;
		ScoreLabel.OffsetBottom = 48;
		ScoreLabel.HorizontalAlignment = HorizontalAlignment.Right;
		ScoreLabel.AddThemeFontSizeOverride("font_size", 22);
		ScoreLabel.AddThemeColorOverride("font_color", new Color(0.93f, 0.98f, 1f));
		ScoreLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.65f));
		ScoreLabel.AddThemeConstantOverride("shadow_offset_x", 2);
		ScoreLabel.AddThemeConstantOverride("shadow_offset_y", 2);
	}

	private void CreateCountdownLabel()
	{
		CountdownLabel = new Label();
		CountdownLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		CountdownLabel.HorizontalAlignment = HorizontalAlignment.Center;
		CountdownLabel.VerticalAlignment = VerticalAlignment.Center;
		CountdownLabel.AddThemeFontSizeOverride("font_size", 96);
		CountdownLabel.Modulate = new Color(1f, 1f, 1f);
		CountdownLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.72f));
		CountdownLabel.AddThemeConstantOverride("shadow_offset_x", 4);
		CountdownLabel.AddThemeConstantOverride("shadow_offset_y", 4);

		GetNode<CanvasLayer>("UI").AddChild(CountdownLabel);
	}

	private void StartGame()
	{
		gameStarted = true;
		CountdownLabel.Hide();

		Player.SetPhysicsProcess(true);

		foreach (Node node in NPCContainer.GetChildren())
			node.SetPhysicsProcess(true);

		foreach (Node node in PassiveFishContainer.GetChildren())
			node.SetPhysicsProcess(true);

		GetNode<ScoreManager>("/root/ScoreManager").StartScoring();
	}

	// =====================================
	// AGGRESSIVE NPC
	// =====================================

	void SpawnNPC()
	{
		NPCFish npc = NPCFishScene.Instantiate<NPCFish>();

		npc.Position = GetSafeSpawnPosition(true);
		npc.Player = Player;
		npc.SetPhysicsProcess(gameStarted);

		NPCContainer.AddChild(npc);
	}

	// =====================================
	// PASSIVE FISH
	// =====================================

	void SpawnPassiveFish()
	{
		PassiveFish fish = PassiveFishScene.Instantiate<PassiveFish>();

		fish.Position = GetSafeSpawnPosition();
		fish.SetPhysicsProcess(gameStarted);

		PassiveFishContainer.AddChild(fish);
	}

	// =====================================
	// COINS
	// =====================================

	void SpawnCoin()
	{
		Coin coin = CoinScene.Instantiate<Coin>();

		coin.Position = GetSafeSpawnPosition();

		CoinContainer.AddChild(coin);
	}

	// =====================================
	// OBSTACLES
	// =====================================

	void SpawnObstacle()
	{
		Node2D obstacle = ObstacleScene.Instantiate<Node2D>();

		obstacle.Position = GetSafeSpawnPosition();

		ObstacleContainer.AddChild(obstacle);
	}

	private Vector2 GetSafeSpawnPosition(bool preferChaseAngle = false)
	{
		Vector2 fallback = Player.Position + Vector2.Right * MinSpawnDistance;

		for (int i = 0; i < MaxSpawnAttempts; i++)
		{
			float angle = GetSpawnAngle(preferChaseAngle, i);
			float distance = (float)GD.RandRange(MinSpawnDistance, MaxSpawnDistance);
			Vector2 candidate = Player.Position + Vector2.FromAngle(angle) * distance;

			if (IsSpawnPositionSafe(candidate))
				return candidate;

			fallback = candidate;
		}

		return fallback;
	}

	private float GetSpawnAngle(bool preferChaseAngle, int attempt)
	{
		if (!preferChaseAngle || Player.Velocity.Length() < 20f || attempt % 4 == 0)
			return (float)GD.RandRange(0f, Mathf.Tau);

		float playerMoveAngle = Player.Velocity.Angle();

		if (attempt % 4 == 1)
		{
			float side = GD.Randf() < 0.5f ? -1f : 1f;
			return playerMoveAngle + side * Mathf.Pi * 0.5f + (float)GD.RandRange(-0.55f, 0.55f);
		}

		return playerMoveAngle + Mathf.Pi + (float)GD.RandRange(-1.15f, 1.15f);
	}

	private bool IsSpawnPositionSafe(Vector2 candidate)
	{
		if (candidate.DistanceTo(Player.Position) < MinSpawnDistance)
			return false;

		foreach (Node2D node in GetAllStreamedNodes())
		{
			if (candidate.DistanceTo(node.Position) < MinSpawnSpacing)
				return false;
		}

		return true;
	}

	private List<Node2D> GetAllStreamedNodes()
	{
		List<Node2D> nodes = new List<Node2D>();

		AddNode2DChildren(nodes, NPCContainer);
		AddNode2DChildren(nodes, PassiveFishContainer);
		AddNode2DChildren(nodes, CoinContainer);
		AddNode2DChildren(nodes, ObstacleContainer);

		return nodes;
	}

	private void AddNode2DChildren(List<Node2D> nodes, Node container)
	{
		foreach (Node child in container.GetChildren())
		{
			if (child is Node2D node && !node.IsQueuedForDeletion())
				nodes.Add(node);
		}
	}

	private void UpdateWorldStreaming(float dt)
	{
		spawnCheckTimer -= dt;

		if (spawnCheckTimer > 0f)
			return;

		spawnCheckTimer = SpawnCheckInterval;

		DespawnFarChildren(NPCContainer);
		DespawnFarChildren(PassiveFishContainer);
		DespawnFarChildren(CoinContainer);
		DespawnFarChildren(ObstacleContainer);

		if (Player.Position.DistanceTo(lastStreamPosition) < SpawnMovementStep)
			return;

		lastStreamPosition = Player.Position;

		FillContainer(NPCContainer, TargetNPCCount, SpawnNPC);
		FillContainer(PassiveFishContainer, TargetPassiveFishCount, SpawnPassiveFish);
		FillContainer(CoinContainer, TargetCoinCount, SpawnCoin);
		FillContainer(ObstacleContainer, TargetObstacleCount, SpawnObstacle);
	}

	private void DespawnFarChildren(Node container)
	{
		foreach (Node child in container.GetChildren())
		{
			if (child is Node2D node &&
				node.Position.DistanceTo(Player.Position) > DespawnDistance)
			{
				node.QueueFree();
			}
		}
	}

	private void FillContainer(Node container, int targetCount, System.Action spawnAction)
	{
		int liveCount = 0;

		foreach (Node child in container.GetChildren())
		{
			if (!child.IsQueuedForDeletion())
				liveCount++;
		}

		while (liveCount < targetCount)
		{
			spawnAction();
			liveCount++;
		}
	}

	// =====================================
	// SCORE UI
	// =====================================

	public override void _Process(double delta)
	{
		if (!gameStarted)
		{
			countdownTimer -= (float)delta;
			int number = Mathf.CeilToInt(countdownTimer);

			CountdownLabel.Text = number > 0 ? number.ToString() : "GO!";
			ScoreLabel.Text = "Score: 0";

			if (countdownTimer <= 0f)
				StartGame();

			return;
		}

		var sm = GetNode<ScoreManager>("/root/ScoreManager");

		ScoreLabel.Text = $"Score: {sm.CurrentScore}";
		UpdateWorldStreaming((float)delta);
	}

	// =====================================
	// STRESS SYSTEM
	// =====================================

	private float CalculateProximityPressure(float realDist, float radius)
	{
		float t = 1f - (realDist / radius);

		if (t <= ActivationThreshold)
			return 0f;

		t = (t - ActivationThreshold) / (1f - ActivationThreshold);
		return t * t * (3f - 2f * t);
	}

	private float CalculateContactPressure(float realDist, float radius)
	{
		if (realDist >= radius)
			return 0f;

		float t = 1f - (realDist / radius);
		return Mathf.Clamp(t, 0f, 1f);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!gameStarted)
			return;

		float dt = (float)delta;

		float threatPressure = 0f;
		float contactPressure = 0f;

		foreach (Node node in NPCContainer.GetChildren())
		{
			if (node is NPCFish npc)
			{
				float dist = Player.Position.DistanceTo(npc.Position);

				float realDist =
					dist - (Player.CollisionRadius + npc.CollisionRadius);

				threatPressure += CalculateProximityPressure(realDist, DangerRadius);
				contactPressure += CalculateContactPressure(realDist, ContactRadius);
			}
		}

		foreach (Node node in PassiveFishContainer.GetChildren())
		{
			if (node is PassiveFish fish)
			{
				float dist = Player.Position.DistanceTo(fish.Position);

				float realDist =
					dist - (Player.CollisionRadius + fish.CollisionRadius);

				threatPressure +=
					CalculateProximityPressure(realDist, PassiveDangerRadius) *
					PassiveStressWeight;

				contactPressure +=
					CalculateContactPressure(realDist, PassiveContactRadius) *
					PassiveStressWeight;
			}
		}

		float stressMultiplier = Player.IsBoosting ? 0.5f : 1f;
		float targetStress = Mathf.Clamp(
			(threatPressure * StressTargetPerThreat * stressMultiplier) +
			(contactPressure * ContactStressTargetBonus),
			0f,
			100f
		);

		float stressSpeed = targetStress > Stress ? StressGain : StressDecayFar;
		Stress = Mathf.MoveToward(Stress, targetStress, stressSpeed * dt);

		if (Player.IsBoosting)
		{
			Stress -= Player.GetStressDrain() * dt;
		}

		Stress = Mathf.Clamp(Stress, 0, 100);

		Player.CurrentStress = Stress;

		StressBar.Value = Stress;

		// GAME OVER
		if (Stress >= 100f)
		{
			GetNode<ScoreManager>("/root/ScoreManager").StopScoring();

			GetTree().ChangeSceneToFile(
				"res://Scenes/NameInput.tscn"
			);

			return;
		}

		// UI COLORS
		if (Stress < 40)
		{
			stressFillStyle.BgColor =
				new Color(0.25f, 0.67f, 1f);
		}
		else if (Stress <= 60)
		{
			stressFillStyle.BgColor =
				new Color(0.35f, 0.95f, 0.48f);
		}
		else
		{
			stressFillStyle.BgColor =
				new Color(1f, 0.34f, 0.31f);
		}
	}
}
