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

	// RARE ITEMS (Level 3+)
	[Export] public Node2D ItemContainer;
	[Export] public PackedScene AlcoholItemScene;
	[Export] public PackedScene ChorusFruitItemScene;

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

	[Export] public float DirectionalEnemySpawnDistance = 980f;
	[Export] public float DirectionalEnemySpawnSpread = 380f;
	[Export] public float CrossingFishChance = 0.28f;
	[Export] public float SwarmSpawnDistance = 760f;
	[Export] public float SwarmSpacing = 76f;
	[Export] public float SwarmSpawnDelay = 2.1f;

	[Export] public int MinAlcoholLevel = 3;
	[Export] public int MinChorusFruitLevel = 2;
	[Export] public int MaxItemsInWorld = 1;
	[Export] public float ItemSpawnChance = 0.07f;
	[Export] public float MinItemSpacing = 920f;
	[Export] public float AlcoholDuration = 7f;
	[Export] public float NpcFleeSpeedMultiplier = 1.35f;
	[Export] public float ChorusTeleportDistance = 760f;
	[Export] public float ChorusMinEnemyDistance = 420f;

	public bool ShouldNpcsFlee => invincibilityTimer > 0f;

	private ProgressBar StressBar;
	private Label ScoreLabel;
	private Label CoinLabel;
	private Label CountdownLabel;
	private Label LevelNoticeLabel;
	private Label ItemEffectLabel;
	private float invincibilityTimer = 0f;

	private float Stress = 0f;
	private float countdownTimer = 0f;
	private bool gameStarted = false;
	private float spawnCheckTimer = 0f;
	private Vector2 lastStreamPosition = Vector2.Zero;
	private int currentLevel = 1;
	private float levelNoticeTimer = 0f;
	private float npcSpeedMultiplier = 1f;
	private float passiveSpeedMultiplier = 1f;
	private int pendingSwarmCount = 0;
	private float pendingSwarmTimer = 0f;
	private VideoStreamPlayer backgroundVideo;
	private float backgroundTime = 0f;
	private Vector2 backgroundScroll = Vector2.Zero;
	private Vector2 backgroundScrollVelocity = Vector2.Zero;
	private bool gameOverTriggered = false;

	private StyleBoxFlat stressFillStyle;
	private StyleBoxFlat stressBackgroundStyle;

	public override void _Ready()
	{
		AddToGroup("game_main");
		CreateBackgroundLayer();

		StressBar = GetNode<ProgressBar>("UI/StressBar");
		ScoreLabel = GetNode<Label>("UI/ScoreLabel");
		SetupUi();
		CreateCountdownLabel();
		CreateLevelNoticeLabel();
		CreateItemEffectLabel();

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

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		backgroundTime += dt;
		AnimateBackground(dt);

		if (!gameStarted)
		{
			countdownTimer -= dt;
			int number = Mathf.CeilToInt(countdownTimer);

			CountdownLabel.Text = number > 0 ? number.ToString() : "GO!";
			ScoreLabel.Text = "Score: 0";
			CoinLabel.Text = "Muenzen: 0";

			if (countdownTimer <= 0f)
				StartGame();

			return;
		}

		var sm = GetNode<ScoreManager>("/root/ScoreManager");

		ScoreLabel.Text = $"Score: {sm.CurrentScore}";
		CoinLabel.Text = $"Muenzen: {sm.CoinsThisRun}";
		UpdateDifficulty(sm.CurrentScore);
		UpdatePendingSwarm(dt);
		UpdateLevelNotice(dt);
		UpdateItemEffects(dt);
		UpdateWorldStreaming(dt);
	}

	private void CreateBackgroundLayer()
	{
		CanvasLayer backgroundLayer = new CanvasLayer();
		backgroundLayer.Layer = -20;
		AddChild(backgroundLayer);

		backgroundVideo = new VideoStreamPlayer();
		backgroundVideo.Stream = ResourceLoader.Load<VideoStream>("res://Assets/underwater.ogv");
		backgroundVideo.SpeedScale = 2.57f;
		backgroundVideo.Autoplay = true;
		backgroundVideo.Expand = true;
		backgroundVideo.Loop = true;
		backgroundVideo.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		backgroundVideo.PivotOffset = GetViewportRect().Size * 0.5f;
		backgroundLayer.AddChild(backgroundVideo);

		ColorRect overlay = new ColorRect();
		overlay.Color = new Color(0.01f, 0.06f, 0.09f, 0.18f);
		overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		backgroundLayer.AddChild(overlay);
	}

	private void AnimateBackground(float dt)
	{
		if (backgroundVideo == null)
			return;

		Vector2 playerVelocity = Player != null ? Player.Velocity : Vector2.Zero;
		float speed = playerVelocity.Length();
		float playerBaseSpeed = Player != null ? Player.Speed : 200f;
		float speedFactor = Mathf.Clamp(speed / Mathf.Max(playerBaseSpeed, 1f), 0f, 2.3f);
		Vector2 desiredScrollVelocity = Vector2.Zero;

		if (speed > 8f)
		{
			float parallaxStrength = Player.IsBoosting ? 0.105f : 0.062f;
			desiredScrollVelocity = -playerVelocity * parallaxStrength;
		}

		backgroundScrollVelocity = backgroundScrollVelocity.Lerp(
			desiredScrollVelocity,
			Mathf.Clamp(dt * 3.2f, 0f, 1f)
		);

		backgroundScroll += backgroundScrollVelocity * dt;
		backgroundScroll.X = WrapAroundCenter(backgroundScroll.X, 92f);
		backgroundScroll.Y = WrapAroundCenter(backgroundScroll.Y, 68f);

		float driftX = Mathf.Sin(backgroundTime * 0.12f) * 18f;
		float driftY = Mathf.Cos(backgroundTime * 0.1f) * 12f;
		float zoom = 1.085f + speedFactor * 0.018f + Mathf.Sin(backgroundTime * 0.07f) * 0.012f;

		backgroundVideo.OffsetLeft = -64f + driftX + backgroundScroll.X;
		backgroundVideo.OffsetTop = -48f + driftY + backgroundScroll.Y;
		backgroundVideo.OffsetRight = 64f + driftX + backgroundScroll.X;
		backgroundVideo.OffsetBottom = 48f + driftY + backgroundScroll.Y;
		backgroundVideo.Scale = new Vector2(zoom, zoom);
	}

	private float WrapAroundCenter(float value, float limit)
	{
		if (value > limit)
			return -limit;

		if (value < -limit)
			return limit;

		return value;
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
		ScoreLabel.OffsetLeft = -230;
		ScoreLabel.OffsetTop = 18;
		ScoreLabel.OffsetRight = -24;
		ScoreLabel.OffsetBottom = 48;
		ScoreLabel.HorizontalAlignment = HorizontalAlignment.Right;
		ScoreLabel.AddThemeFontSizeOverride("font_size", 22);
		ScoreLabel.AddThemeColorOverride("font_color", new Color(0.93f, 0.98f, 1f));
		ScoreLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.65f));
		ScoreLabel.AddThemeConstantOverride("shadow_offset_x", 2);
		ScoreLabel.AddThemeConstantOverride("shadow_offset_y", 2);

		CoinLabel = new Label();
		CoinLabel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		CoinLabel.OffsetLeft = -230;
		CoinLabel.OffsetTop = 48;
		CoinLabel.OffsetRight = -24;
		CoinLabel.OffsetBottom = 78;
		CoinLabel.HorizontalAlignment = HorizontalAlignment.Right;
		CoinLabel.AddThemeFontSizeOverride("font_size", 20);
		CoinLabel.AddThemeColorOverride("font_color", new Color(0.98f, 0.9f, 0.34f));
		CoinLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.65f));
		CoinLabel.AddThemeConstantOverride("shadow_offset_x", 2);
		CoinLabel.AddThemeConstantOverride("shadow_offset_y", 2);
		GetNode<CanvasLayer>("UI").AddChild(CoinLabel);
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

	private void CreateLevelNoticeLabel()
	{
		LevelNoticeLabel = new Label();
		LevelNoticeLabel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
		LevelNoticeLabel.OffsetLeft = 240;
		LevelNoticeLabel.OffsetTop = 72;
		LevelNoticeLabel.OffsetRight = -240;
		LevelNoticeLabel.OffsetBottom = 126;
		LevelNoticeLabel.HorizontalAlignment = HorizontalAlignment.Center;
		LevelNoticeLabel.VerticalAlignment = VerticalAlignment.Center;
		LevelNoticeLabel.AddThemeFontSizeOverride("font_size", 30);
		LevelNoticeLabel.AddThemeColorOverride("font_color", new Color(0.94f, 1f, 0.98f));
		LevelNoticeLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.72f));
		LevelNoticeLabel.AddThemeConstantOverride("shadow_offset_x", 3);
		LevelNoticeLabel.AddThemeConstantOverride("shadow_offset_y", 3);
		LevelNoticeLabel.Hide();

		GetNode<CanvasLayer>("UI").AddChild(LevelNoticeLabel);
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
		if (gameStarted && currentLevel >= 2 && GD.Randf() < CrossingFishChance)
		{
			SpawnCrossingNPC();
			return;
		}

		NPCFish npc = NPCFishScene.Instantiate<NPCFish>();

		npc.Position = GetDirectionalEnemySpawnPosition();
		npc.Player = Player;
		npc.Speed *= npcSpeedMultiplier * GetNewEnemySpeedBoost();
		npc.SetPhysicsProcess(gameStarted);

		NPCContainer.AddChild(npc);
	}

	private void SpawnCrossingNPC()
	{
		NPCFish npc = NPCFishScene.Instantiate<NPCFish>();
		Vector2 forward = GetPlayerMoveDirection();
		Vector2 side = new Vector2(-forward.Y, forward.X);
		float sideSign = GD.Randf() < 0.5f ? -1f : 1f;
		Vector2 laneCenter =
			Player.Position +
			forward * (float)GD.RandRange(260f, 540f);

		npc.Position =
			laneCenter +
			side * sideSign * (float)GD.RandRange(680f, 920f);

		npc.Player = Player;
		npc.Mode = NPCFish.MovementMode.Crossing;
		npc.CrossingDirection =
			(-side * sideSign + forward * (float)GD.RandRange(-0.15f, 0.35f)).Normalized();
		npc.CrossingLifetime = (float)GD.RandRange(4.2f, 6.2f);
		npc.Speed *= npcSpeedMultiplier * (1.75f + currentLevel * 0.12f);
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
		fish.Speed *= passiveSpeedMultiplier;
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

	// =====================================
	// RARE ITEMS
	// =====================================

	private void TrySpawnRareItem()
	{
		if (ItemContainer == null)
			return;

		ItemType? type = PickRandomItemType();

		if (type == null)
			return;

		if (CountLiveItems() >= MaxItemsInWorld)
			return;

		if (GD.Randf() > ItemSpawnChance)
			return;

		Vector2? spawnPos = GetItemSpawnPosition();

		if (spawnPos == null)
			return;

		PickupItem item = type.Value == ItemType.Alcohol
			? AlcoholItemScene.Instantiate<PickupItem>()
			: ChorusFruitItemScene.Instantiate<PickupItem>();

		item.Type = type.Value;
		item.Position = spawnPos.Value;
		ItemContainer.AddChild(item);
	}

	private ItemType? PickRandomItemType()
	{
		bool canAlcohol = currentLevel >= MinAlcoholLevel;
		bool canChorus = currentLevel >= MinChorusFruitLevel;

		if (!canAlcohol && !canChorus)
			return null;

		if (canAlcohol && canChorus)
			return GD.Randf() < 0.5f ? ItemType.Alcohol : ItemType.ChorusFruit;

		if (canChorus)
			return ItemType.ChorusFruit;

		return ItemType.Alcohol;
	}

	private int CountLiveItems()
	{
		if (ItemContainer == null)
			return 0;

		int count = 0;

		foreach (Node child in ItemContainer.GetChildren())
		{
			if (!child.IsQueuedForDeletion())
				count++;
		}

		return count;
	}

	private Vector2? GetItemSpawnPosition()
	{
		for (int i = 0; i < MaxSpawnAttempts; i++)
		{
			float angle = (float)GD.RandRange(0f, Mathf.Tau);
			float distance = (float)GD.RandRange(MinSpawnDistance, MaxSpawnDistance);
			Vector2 candidate = Player.Position + Vector2.FromAngle(angle) * distance;

			if (IsItemSpawnPositionSafe(candidate))
				return candidate;
		}

		return null;
	}

	private bool IsItemSpawnPositionSafe(Vector2 candidate)
	{
		if (!IsSpawnPositionSafe(candidate))
			return false;

		if (ItemContainer == null)
			return true;

		foreach (Node child in ItemContainer.GetChildren())
		{
			if (child is Node2D item && !item.IsQueuedForDeletion())
			{
				if (candidate.DistanceTo(item.Position) < MinItemSpacing)
					return false;
			}
		}

		return true;
	}

	public void ApplyItem(ItemType type)
	{
		switch (type)
		{
			case ItemType.Alcohol:
				ApplyAlcoholEffect();
				break;
			case ItemType.ChorusFruit:
				ApplyChorusFruitEffect();
				break;
		}
	}

	private void ApplyAlcoholEffect()
	{
		invincibilityTimer = AlcoholDuration;
		Player.SetInvincible(true);
		Stress = 0f;
		Player.CurrentStress = 0f;
		StressBar.Value = 0f;
		ShowLevelNotice("Alkohol: kurz unverwundbar!");
		UpdateItemEffectLabel();
	}

	private void ApplyChorusFruitEffect()
	{
		Player.GlobalPosition = FindChorusTeleportPosition();
		Stress = Mathf.Min(Stress, 55f);
		Player.CurrentStress = Stress;
		StressBar.Value = Stress;
		ShowLevelNotice("Chorusfrucht: wegteleportiert!");
		UpdateItemEffectLabel();
	}

	private Vector2 FindChorusTeleportPosition()
	{
		Vector2 awayDirection = Vector2.Zero;
		float nearestDist = float.MaxValue;

		foreach (Node node in NPCContainer.GetChildren())
		{
			if (node is not NPCFish npc)
				continue;

			float dist = Player.Position.DistanceTo(npc.Position);

			if (dist >= nearestDist)
				continue;

			nearestDist = dist;
			awayDirection = (Player.Position - npc.Position).Normalized();
		}

		if (awayDirection.LengthSquared() < 0.01f)
			awayDirection = GetPlayerMoveDirection();

		Vector2 forward = GetPlayerMoveDirection();
		Vector2[] directions =
		{
			awayDirection,
			awayDirection.Rotated(Mathf.Pi * 0.35f),
			awayDirection.Rotated(-Mathf.Pi * 0.35f),
			forward,
			-forward
		};

		foreach (Vector2 dir in directions)
		{
			if (dir.LengthSquared() < 0.01f)
				continue;

			Vector2 candidate =
				Player.Position + dir.Normalized() * ChorusTeleportDistance;

			if (IsChorusTeleportSafe(candidate))
				return candidate;
		}

		return Player.Position + awayDirection.Normalized() * ChorusTeleportDistance * 0.7f;
	}

	private bool IsChorusTeleportSafe(Vector2 position)
	{
		foreach (Node node in NPCContainer.GetChildren())
		{
			if (node is NPCFish npc &&
				position.DistanceTo(npc.Position) < ChorusMinEnemyDistance)
			{
				return false;
			}
		}

		return true;
	}

	private void UpdateItemEffects(float dt)
	{
		if (invincibilityTimer > 0f)
		{
			invincibilityTimer -= dt;

			if (invincibilityTimer <= 0f)
			{
				invincibilityTimer = 0f;
				Player.SetInvincible(false);
			}
		}

		UpdateItemEffectLabel();
	}

	private void CreateItemEffectLabel()
	{
		ItemEffectLabel = new Label();
		ItemEffectLabel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		ItemEffectLabel.OffsetLeft = 24f;
		ItemEffectLabel.OffsetTop = 50f;
		ItemEffectLabel.OffsetRight = 420f;
		ItemEffectLabel.OffsetBottom = 78f;
		ItemEffectLabel.AddThemeFontSizeOverride("font_size", 17);
		ItemEffectLabel.AddThemeColorOverride("font_color", new Color(0.88f, 1f, 0.95f));
		ItemEffectLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.65f));
		ItemEffectLabel.AddThemeConstantOverride("shadow_offset_x", 2);
		ItemEffectLabel.AddThemeConstantOverride("shadow_offset_y", 2);
		ItemEffectLabel.Hide();

		GetNode<CanvasLayer>("UI").AddChild(ItemEffectLabel);
	}

	private void UpdateItemEffectLabel()
	{
		if (ItemEffectLabel == null)
			return;

		if (invincibilityTimer > 0f)
		{
			ItemEffectLabel.Text =
				$"Unverwundbar: {invincibilityTimer:0.0}s";
			ItemEffectLabel.Show();
			return;
		}

		ItemEffectLabel.Hide();
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

	private Vector2 GetDirectionalEnemySpawnPosition()
	{
		if (!gameStarted || Player.Velocity.Length() < 20f)
			return GetSafeSpawnPosition(true);

		Vector2 forward = GetPlayerMoveDirection();
		Vector2 side = new Vector2(-forward.Y, forward.X);
		Vector2 fallback =
			Player.Position +
			forward * DirectionalEnemySpawnDistance;

		for (int i = 0; i < MaxSpawnAttempts; i++)
		{
			float distance = (float)GD.RandRange(
				MinSpawnDistance * 0.86f,
				DirectionalEnemySpawnDistance + 240f
			);
			float spread = (float)GD.RandRange(
				-DirectionalEnemySpawnSpread,
				DirectionalEnemySpawnSpread
			);
			Vector2 candidate =
				Player.Position +
				forward * distance +
				side * spread;

			if (IsSpawnPositionSafe(candidate))
				return candidate;

			fallback = candidate;
		}

		return fallback;
	}

	private Vector2 GetPlayerMoveDirection()
	{
		if (Player.Velocity.Length() > 20f)
			return Player.Velocity.Normalized();

		return Vector2.Right;
	}

	private float GetNewEnemySpeedBoost()
	{
		if (currentLevel >= 3)
			return 1.2f;

		if (currentLevel >= 2)
			return 1.12f;

		return 1f;
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
		if (ItemContainer != null)
			AddNode2DChildren(nodes, ItemContainer);

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
		DespawnFarChildren(ItemContainer);

		if (Player.Position.DistanceTo(lastStreamPosition) < SpawnMovementStep)
			return;

		lastStreamPosition = Player.Position;

		FillContainer(NPCContainer, TargetNPCCount, SpawnNPC);
		FillContainer(PassiveFishContainer, TargetPassiveFishCount, SpawnPassiveFish);
		FillContainer(CoinContainer, TargetCoinCount, SpawnCoin);
		FillContainer(ObstacleContainer, TargetObstacleCount, SpawnObstacle);
		TrySpawnRareItem();
	}

	private void DespawnFarChildren(Node container)
	{
		if (container == null)
			return;

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

	private void UpdateDifficulty(int score)
	{
		if (currentLevel < 2 && score >= 2000)
			SetDifficultyLevel(2);

		if (currentLevel < 3 && score >= 4500)
			SetDifficultyLevel(3);
	}

	private void SetDifficultyLevel(int level)
	{
		currentLevel = level;

		if (level == 2)
		{
			TargetNPCCount += 3;
			TargetPassiveFishCount += 3;
			TargetObstacleCount += 2;
			SpawnCheckInterval = Mathf.Max(0.22f, SpawnCheckInterval - 0.08f);
			ApplySpeedMultiplier(1.18f, 1.1f);
			ScheduleLevelSwarm(3);
			ShowLevelNotice("LvL 2 erreicht");
		}
		else if (level == 3)
		{
			TargetNPCCount += 5;
			TargetPassiveFishCount += 4;
			TargetObstacleCount += 3;
			SpawnCheckInterval = Mathf.Max(0.18f, SpawnCheckInterval - 0.06f);
			ApplySpeedMultiplier(1.38f, 1.22f);
			ScheduleLevelSwarm(5);
			ShowLevelNotice("LvL 3 erreicht");
		}
	}

	private void ScheduleLevelSwarm(int count)
	{
		pendingSwarmCount = count;
		pendingSwarmTimer = SwarmSpawnDelay;
	}

	private void UpdatePendingSwarm(float dt)
	{
		if (pendingSwarmCount <= 0)
			return;

		pendingSwarmTimer -= dt;

		if (pendingSwarmTimer > 0f)
			return;

		SpawnLevelSwarm(pendingSwarmCount);
		pendingSwarmCount = 0;
	}

	private void SpawnLevelSwarm(int count)
	{
		Vector2 forward = GetPlayerMoveDirection();
		Vector2 side = new Vector2(-forward.Y, forward.X);
		float sideSign = GD.Randf() < 0.5f ? -1f : 1f;
		Vector2 center =
			Player.Position +
			forward * SwarmSpawnDistance +
			side * sideSign * 320f;

		for (int i = 0; i < count; i++)
		{
			NPCFish npc = NPCFishScene.Instantiate<NPCFish>();
			float row = i - (count - 1) * 0.5f;
			Vector2 offset =
				side * row * SwarmSpacing +
				forward * (float)GD.RandRange(-48f, 48f);

			npc.Position = center + offset;
			npc.Player = Player;
			npc.Speed *= npcSpeedMultiplier * 1.18f;
			npc.ApproachOffsetStrength *= 1.35f;
			npc.SeparationRadius *= 0.82f;
			npc.SetPhysicsProcess(gameStarted);

			NPCContainer.AddChild(npc);
		}
	}

	private void ApplySpeedMultiplier(float newNpcMultiplier, float newPassiveMultiplier)
	{
		float npcRatio = newNpcMultiplier / npcSpeedMultiplier;
		float passiveRatio = newPassiveMultiplier / passiveSpeedMultiplier;

		npcSpeedMultiplier = newNpcMultiplier;
		passiveSpeedMultiplier = newPassiveMultiplier;

		foreach (Node node in NPCContainer.GetChildren())
		{
			if (node is NPCFish npc)
				npc.Speed *= npcRatio;
		}

		foreach (Node node in PassiveFishContainer.GetChildren())
		{
			if (node is PassiveFish fish)
				fish.Speed *= passiveRatio;
		}
	}

	private void ShowLevelNotice(string text)
	{
		LevelNoticeLabel.Text = text;
		LevelNoticeLabel.Modulate = new Color(1f, 1f, 1f, 1f);
		LevelNoticeLabel.Show();
		levelNoticeTimer = 2.6f;
	}

	private void UpdateLevelNotice(float dt)
	{
		if (levelNoticeTimer <= 0f)
			return;

		levelNoticeTimer -= dt;

		if (levelNoticeTimer <= 0f)
		{
			LevelNoticeLabel.Hide();
			return;
		}

		float alpha = Mathf.Clamp(levelNoticeTimer, 0f, 1f);
		LevelNoticeLabel.Modulate = new Color(1f, 1f, 1f, alpha);
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
		bool invincible = Player.IsInvincible || invincibilityTimer > 0f;

		if (invincible)
		{
			Stress = Mathf.MoveToward(Stress, 0f, 95f * dt);
			Player.CurrentStress = Stress;
			StressBar.Value = Stress;
			UpdateStressBarColor();
			return;
		}

		float threatPressure = 0f;
		float contactPressure = 0f;

		foreach (Node node in NPCContainer.GetChildren())
		{
			if (node is NPCFish npc)
			{
				float dist = Player.Position.DistanceTo(npc.Position);

				float realDist =
					dist - (Player.CollisionRadius + npc.CollisionRadius);

				if (realDist <= 0f)
				{
					GameOver();
					return;
				}

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

				if (realDist <= 0f)
				{
					GameOver();
					return;
				}

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
			GameOver();
			return;
		}

		UpdateStressBarColor();
	}

	private void UpdateStressBarColor()
	{
		if (Stress < 40)
			stressFillStyle.BgColor = new Color(0.25f, 0.67f, 1f);
		else if (Stress <= 60)
			stressFillStyle.BgColor = new Color(0.35f, 0.95f, 0.48f);
		else
			stressFillStyle.BgColor = new Color(1f, 0.34f, 0.31f);
	}

	private void GameOver()
	{
		if (gameOverTriggered)
			return;

		gameOverTriggered = true;
		gameStarted = false;
		GetNode<ScoreManager>("/root/ScoreManager").StopScoring();

		Player.SetPhysicsProcess(false);

		foreach (Node node in NPCContainer.GetChildren())
			node.SetPhysicsProcess(false);

		foreach (Node node in PassiveFishContainer.GetChildren())
			node.SetPhysicsProcess(false);

		SceneTransition.FadeToScene(
			GetTree(),
			"res://Scenes/nameinput.tscn",
			0.38f
		);
	}
}
