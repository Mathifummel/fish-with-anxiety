using Godot;
using System.Collections.Generic;

public partial class Main : Node2D
{
	[Export] public PlayerFish Player;
	[Export] public bool MenuPreviewMode = false;

	// EXISTING NPCS (aggressive)
	[Export] public Node2D NPCContainer;
	[Export] public PackedScene NPCFishScene;

	// NEW PASSIVE FISH
	[Export] public Node2D PassiveFishContainer;
	[Export] public PackedScene PassiveFishScene;

	// JELLYFISH (threat, but not a direct chaser)
	[Export] public Node2D JellyfishContainer;
	[Export] public PackedScene JellyfishScene;

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
	[Export] public PackedScene TrashItemScene;

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
	[Export] public float JellyfishDangerRadius = 330f;
	[Export] public float JellyfishContactRadius = 48f;
	[Export] public float JellyfishStressWeight = 0.72f;
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
	[Export] public int InitialJellyfishCount = 0;
	[Export] public int InitialCoinCount = 6;
	[Export] public int InitialObstacleCount = 4;

	[Export] public int TargetNPCCount = 7;
	[Export] public int TargetPassiveFishCount = 14;
	[Export] public int TargetJellyfishCount = 0;
	[Export] public int TargetCoinCount = 18;
	[Export] public int TargetObstacleCount = 10;

	[Export] public float DirectionalEnemySpawnDistance = 980f;
	[Export] public float DirectionalEnemySpawnSpread = 380f;
	[Export] public float CrossingFishChance = 0.28f;
	[Export] public float SwarmSpawnDistance = 920f;
	[Export] public float SwarmSpacing = 126f;
	[Export] public float SwarmSpawnDelay = 2.8f;
	[Export] public int LevelTwoScore = 1200;
	[Export] public int LevelThreeScore = 2500;
	[Export] public int LevelFourScore = 3750;
	[Export] public int LevelFiveScore = 5000;

	[Export] public int MinJellyfishLevel = 2;
	[Export] public int MinAlcoholLevel = 4;
	[Export] public int MinChorusFruitLevel = 2;
	[Export] public int MinTrashLevel = 3;
	[Export] public float TrashSlowMultiplier = 0.52f;
	[Export] public float TrashSlowDuration = 5.5f;
	[Export] public int MaxItemsInWorld = 1;
	[Export] public float ItemSpawnChance = 0.07f;
	[Export] public float MinItemSpacing = 920f;
	[Export] public float AlcoholDuration = 7f;
	[Export] public float NpcFleeSpeedMultiplier = 1.35f;
	[Export] public float ChorusTeleportDistance = 760f;
	[Export] public float ChorusMinEnemyDistance = 420f;
	[Export] public float ItemHintMinDelay = 8f;
	[Export] public float ItemHintMaxDelay = 16f;
	[Export] public float ItemHintDuration = 1.65f;
	[Export] public float ItemHintChance = 0.62f;
	[Export] public float ItemHintScreenRadius = 145f;

	public bool ShouldNpcsFlee => invincibilityTimer > 0f;

	private ProgressBar StressBar;
	private Label ScoreLabel;
	private Label CoinLabel;
	private Label CountdownLabel;
	private Label LevelNoticeLabel;
	private Label ItemEffectLabel;
	private TextureRect ItemHintArrow;
	private Label ItemHintText;
	private Button PauseButton;
	private ColorRect PauseBackdrop;
	private Panel PausePanel;
	private VBoxContainer PauseCustomBindings;
	private Label PauseCaptureLabel;
	private Label PauseStatusLabel;
	private Button PauseConfirmButton;
	private float invincibilityTimer = 0f;

	private float Stress = 0f;
	private float countdownTimer = 0f;
	private bool gameStarted = false;
	private bool gamePaused = false;
	private float spawnCheckTimer = 0f;
	private Vector2 lastStreamPosition = Vector2.Zero;
	private int currentLevel = 1;
	private float levelNoticeTimer = 0f;
	private float itemHintCooldown = 0f;
	private float itemHintTimer = 0f;
	private float npcSpeedMultiplier = 1f;
	private float passiveSpeedMultiplier = 1f;
	private float jellyfishSpeedMultiplier = 1f;
	private int pendingSwarmCount = 0;
	private float pendingSwarmTimer = 0f;
	private VideoStreamPlayer backgroundVideo;
	private float backgroundTime = 0f;
	private Vector2 backgroundScroll = Vector2.Zero;
	private Vector2 backgroundScrollVelocity = Vector2.Zero;
	private bool gameOverTriggered = false;
	private string pendingPauseCustomAction = "";
	private float pauseConfirmationTimer = 0f;

	private StyleBoxFlat stressFillStyle;
	private StyleBoxFlat stressBackgroundStyle;
	private readonly Dictionary<PlayerFish.ControlScheme, Button> pauseModeButtons =
		new Dictionary<PlayerFish.ControlScheme, Button>();
	private readonly Dictionary<string, Button> pauseCustomButtons = new Dictionary<string, Button>();

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
		CreateItemDirectionHint();
		CreatePauseMenu();

		if (MenuPreviewMode)
		{
			SetupMenuPreviewRun();
			return;
		}

		var sm = GetNode<ScoreManager>("/root/ScoreManager");

		if (sm.PendingRevival)
			SetupRevivedRun(sm);
		else
			SetupNewRun(sm);
	}

	private void SetupNewRun(ScoreManager sm)
	{
		sm.Reset();
		sm.ClearRevivalState();

		countdownTimer = CountdownDuration;
		lastStreamPosition = Player.Position;
		Player.SetPhysicsProcess(false);
		gameOverTriggered = false;
		gamePaused = false;
		currentLevel = 1;
		Stress = 0f;
		itemHintTimer = 0f;
		ResetItemHintCooldown();
		HideItemDirectionHint();
		HidePauseMenu();
		PauseButton?.Hide();

		SpawnInitialWorld();
	}

	private void SetupRevivedRun(ScoreManager sm)
	{
		sm.PendingRevival = false;
		sm.StartScoring();

		gameOverTriggered = false;
		gamePaused = false;
		gameStarted = true;
		currentLevel = sm.SavedLevel;
		Stress = Mathf.Clamp(sm.SavedStress, 0f, 85f);
		Player.CurrentStress = Stress;
		StressBar.Value = Stress;
		Player.GlobalPosition = sm.SavedPlayerPosition;
		lastStreamPosition = Player.Position;
		Player.SetInvincible(false);
		Player.SpeedMultiplier = 1f;
		Player.SetPhysicsProcess(true);
		CountdownLabel?.Hide();
		itemHintTimer = 0f;
		ResetItemHintCooldown();
		HideItemDirectionHint();
		HidePauseMenu();
		PauseButton?.Show();

		ApplyLevelSettings(currentLevel, false);
		SpawnInitialWorld();
		ShowLevelNotice("Wiederbelebt!");
		UpdateItemEffectLabel();
	}

	private void SpawnInitialWorld()
	{
		for (int i = 0; i < InitialObstacleCount; i++)
			SpawnObstacle();

		for (int i = 0; i < InitialNPCCount; i++)
			SpawnNPC();

		for (int i = 0; i < InitialPassiveFishCount; i++)
			SpawnPassiveFish();

		for (int i = 0; i < InitialJellyfishCount; i++)
			SpawnJellyfish();

		for (int i = 0; i < InitialCoinCount; i++)
			SpawnCoin();
	}

	private void SetupMenuPreviewRun()
	{
		gameOverTriggered = false;
		gamePaused = false;
		gameStarted = true;
		currentLevel = 3;
		Stress = 0f;
		countdownTimer = 0f;
		lastStreamPosition = Player.Position;

		CanvasLayer ui = GetNodeOrNull<CanvasLayer>("UI");
		if (ui != null)
			ui.Visible = false;

		Player.SetPhysicsProcess(false);
		Player.CurrentStress = 0f;
		CountdownLabel?.Hide();
		HideItemDirectionHint();
		HidePauseMenu();
		PauseButton?.Hide();

		ApplyLevelSettings(currentLevel, false);
		SpawnInitialWorld();
		FillContainer(NPCContainer, Mathf.Min(TargetNPCCount, 6), SpawnNPC);
		FillContainer(PassiveFishContainer, Mathf.Min(TargetPassiveFishCount, 10), SpawnPassiveFish);
		FillContainer(JellyfishContainer, Mathf.Min(TargetJellyfishCount, 3), SpawnJellyfish);
		FillContainer(CoinContainer, Mathf.Min(TargetCoinCount, 8), SpawnCoin);
		FillContainer(ObstacleContainer, Mathf.Min(TargetObstacleCount, 6), SpawnObstacle);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		backgroundTime += dt;
		AnimateBackground(dt);

		if (gamePaused)
		{
			UpdatePauseConfirmation(dt);
			return;
		}

		if (MenuPreviewMode)
		{
			UpdatePendingSwarm(dt);
			UpdateWorldStreaming(dt);
			return;
		}

		if (!gameStarted)
		{
			countdownTimer -= dt;
			int number = Mathf.CeilToInt(countdownTimer);

			CountdownLabel.Text = number > 0 ? number.ToString() : "GO!";
			ScoreLabel.Text = "Score: 0";
			CoinLabel.Text = "Münzen: 0";

			if (countdownTimer <= 0f)
				StartGame();

			return;
		}

		var sm = GetNode<ScoreManager>("/root/ScoreManager");

		ScoreLabel.Text = $"Score: {sm.CurrentScore}";
		CoinLabel.Text = $"Münzen: {sm.CoinsThisRun}";
		UpdateDifficulty(sm.CurrentScore);
		UpdatePendingSwarm(dt);
		UpdateLevelNotice(dt);
		UpdateItemEffects(dt);
		UpdateItemDirectionHint(dt);
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
		GameUi.ApplyLabel(ScoreLabel, 22, GameUi.LightText);

		CoinLabel = new Label();
		CoinLabel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		CoinLabel.OffsetLeft = -230;
		CoinLabel.OffsetTop = 48;
		CoinLabel.OffsetRight = -24;
		CoinLabel.OffsetBottom = 78;
		CoinLabel.HorizontalAlignment = HorizontalAlignment.Right;
		GameUi.ApplyLabel(CoinLabel, 20, new Color(0.98f, 0.9f, 0.34f));
		GetNode<CanvasLayer>("UI").AddChild(CoinLabel);
	}

	private void CreatePauseMenu()
	{
		CanvasLayer ui = GetNode<CanvasLayer>("UI");

		PauseButton = new Button();
		PauseButton.Text = "Pause";
		PauseButton.CustomMinimumSize = new Vector2(96, 38);
		PauseButton.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		PauseButton.OffsetLeft = -344;
		PauseButton.OffsetTop = 18;
		PauseButton.OffsetRight = -242;
		PauseButton.OffsetBottom = 58;
		PauseButton.FocusMode = Control.FocusModeEnum.None;
		ApplyPauseButtonStyle(PauseButton, false);
		PauseButton.Pressed += () => SetGameplayPaused(true);
		PauseButton.Hide();
		ui.AddChild(PauseButton);

		PauseBackdrop = new ColorRect();
		PauseBackdrop.Visible = false;
		PauseBackdrop.Color = new Color(0f, 0.03f, 0.05f, 0.58f);
		PauseBackdrop.MouseFilter = Control.MouseFilterEnum.Stop;
		PauseBackdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		ui.AddChild(PauseBackdrop);

		PausePanel = new Panel();
		PausePanel.Visible = false;
		PausePanel.AnchorLeft = 0.5f;
		PausePanel.AnchorTop = 0.5f;
		PausePanel.AnchorRight = 0.5f;
		PausePanel.AnchorBottom = 0.5f;
		PausePanel.OffsetLeft = -260;
		PausePanel.OffsetTop = -305;
		PausePanel.OffsetRight = 260;
		PausePanel.OffsetBottom = 305;
		PausePanel.AddThemeStyleboxOverride(
			"panel",
			CreatePausePanelStyle(new Color(0.02f, 0.12f, 0.17f, 0.97f))
		);
		ui.AddChild(PausePanel);

		VBoxContainer layout = new VBoxContainer();
		layout.AnchorRight = 1f;
		layout.AnchorBottom = 1f;
		layout.OffsetLeft = 30;
		layout.OffsetTop = 24;
		layout.OffsetRight = -30;
		layout.OffsetBottom = -24;
		layout.AddThemeConstantOverride("separation", 9);
		PausePanel.AddChild(layout);

		Label title = new Label();
		title.Text = "Pausiert";
		title.HorizontalAlignment = HorizontalAlignment.Center;
		GameUi.ApplyLabel(title, 30, GameUi.DarkText);
		layout.AddChild(title);

		Label sectionLabel = new Label();
		sectionLabel.Text = "Steuerung";
		GameUi.ApplyLabel(sectionLabel, 17, new Color(0.05f, 0.22f, 0.34f, 0.82f));
		layout.AddChild(sectionLabel);

		AddPauseModeButton(layout, "Pfeiltasten", PlayerFish.ControlScheme.ArrowKeys);
		AddPauseModeButton(layout, "WASD", PlayerFish.ControlScheme.WASD);
		AddPauseModeButton(layout, "Maussteuerung", PlayerFish.ControlScheme.Mouse);
		AddPauseModeButton(layout, "Eigene Tasten", PlayerFish.ControlScheme.Custom);

		PauseCustomBindings = new VBoxContainer();
		PauseCustomBindings.Visible = PlayerFish.CurrentControlScheme == PlayerFish.ControlScheme.Custom;
		PauseCustomBindings.AddThemeConstantOverride("separation", 6);
		layout.AddChild(PauseCustomBindings);

		AddPauseCustomBindingButton("Hoch", PlayerFish.CustomMoveUp);
		AddPauseCustomBindingButton("Runter", PlayerFish.CustomMoveDown);
		AddPauseCustomBindingButton("Links", PlayerFish.CustomMoveLeft);
		AddPauseCustomBindingButton("Rechts", PlayerFish.CustomMoveRight);
		AddPauseCustomBindingButton("Boost", PlayerFish.CustomBoost);

		PauseCaptureLabel = new Label();
		PauseCaptureLabel.Text = "";
		PauseCaptureLabel.HorizontalAlignment = HorizontalAlignment.Center;
		GameUi.ApplyLabel(PauseCaptureLabel, 14, new Color(0.48f, 0.36f, 0.02f));
		layout.AddChild(PauseCaptureLabel);

		PauseStatusLabel = new Label();
		PauseStatusLabel.HorizontalAlignment = HorizontalAlignment.Center;
		GameUi.ApplyLabel(PauseStatusLabel, 16, new Color(0.02f, 0.34f, 0.18f));
		layout.AddChild(PauseStatusLabel);

		PauseConfirmButton = CreatePauseMenuButton("Bestätigen");
		PauseConfirmButton.CustomMinimumSize = new Vector2(0, 42);
		PauseConfirmButton.Pressed += () =>
		{
			PlayerFish.SaveControlSettings();
			ShowPauseConfirmation("Bestätigt");
		};
		layout.AddChild(PauseConfirmButton);

		HBoxContainer bottomRow = new HBoxContainer();
		bottomRow.AddThemeConstantOverride("separation", 10);
		layout.AddChild(bottomRow);

		Button resumeButton = CreatePauseMenuButton("Weiter");
		resumeButton.CustomMinimumSize = new Vector2(0, 40);
		resumeButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		resumeButton.Pressed += () => SetGameplayPaused(false);
		bottomRow.AddChild(resumeButton);

		Button endRunButton = CreatePauseMenuButton("Runde beenden");
		endRunButton.CustomMinimumSize = new Vector2(0, 40);
		endRunButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		endRunButton.Pressed += EndRunFromPause;
		bottomRow.AddChild(endRunButton);

		UpdatePauseModeButtonLabels();
		UpdatePauseCustomButtonLabels();
		UpdatePauseSettingsStatus();
	}

	private void AddPauseModeButton(VBoxContainer parent, string text, PlayerFish.ControlScheme scheme)
	{
		Button button = CreatePauseMenuButton(text);
		button.CustomMinimumSize = new Vector2(0, 36);
		button.Pressed += () => SelectPauseControlScheme(scheme);
		pauseModeButtons[scheme] = button;
		parent.AddChild(button);
	}

	private void AddPauseCustomBindingButton(string label, string action)
	{
		Button button = CreatePauseMenuButton("");
		button.CustomMinimumSize = new Vector2(0, 32);
		button.Pressed += () =>
		{
			pendingPauseCustomAction = action;
			PauseCaptureLabel.Text = $"{label}: Taste oder Mausklick drücken";
		};

		pauseCustomButtons[action] = button;
		PauseCustomBindings.AddChild(button);
	}

	private Button CreatePauseMenuButton(string text)
	{
		Button button = new Button();
		button.Text = text;
		button.FocusMode = Control.FocusModeEnum.None;
		ApplyPauseButtonStyle(button, false);
		return button;
	}

	private void SelectPauseControlScheme(PlayerFish.ControlScheme scheme)
	{
		PlayerFish.SetControlScheme(scheme);
		PauseCustomBindings.Visible = scheme == PlayerFish.ControlScheme.Custom;
		UpdatePauseSettingsStatus();
		UpdatePauseModeButtonLabels();
		UpdatePauseCustomButtonLabels();
		ShowPauseConfirmation("Gespeichert");
	}

	private void UpdatePauseModeButtonLabels()
	{
		foreach (var pair in pauseModeButtons)
		{
			bool selected = pair.Key == PlayerFish.CurrentControlScheme;
			pair.Value.Text = $"{(selected ? "> " : "")}{GetControlSchemeName(pair.Key)}";
			ApplyPauseButtonStyle(pair.Value, selected);
		}
	}

	private void UpdatePauseCustomButtonLabels()
	{
		if (pauseCustomButtons.Count == 0)
			return;

		pauseCustomButtons[PlayerFish.CustomMoveUp].Text =
			$"Hoch: {PlayerFish.GetCustomInputLabel(PlayerFish.CustomMoveUp)}";
		pauseCustomButtons[PlayerFish.CustomMoveDown].Text =
			$"Runter: {PlayerFish.GetCustomInputLabel(PlayerFish.CustomMoveDown)}";
		pauseCustomButtons[PlayerFish.CustomMoveLeft].Text =
			$"Links: {PlayerFish.GetCustomInputLabel(PlayerFish.CustomMoveLeft)}";
		pauseCustomButtons[PlayerFish.CustomMoveRight].Text =
			$"Rechts: {PlayerFish.GetCustomInputLabel(PlayerFish.CustomMoveRight)}";
		pauseCustomButtons[PlayerFish.CustomBoost].Text =
			$"Boost: {PlayerFish.GetCustomInputLabel(PlayerFish.CustomBoost)}";
	}

	private string GetControlSchemeName(PlayerFish.ControlScheme scheme)
	{
		return scheme switch
		{
			PlayerFish.ControlScheme.ArrowKeys => "Pfeiltasten",
			PlayerFish.ControlScheme.WASD => "WASD",
			PlayerFish.ControlScheme.Mouse => "Maussteuerung",
			PlayerFish.ControlScheme.Custom => "Eigene Tasten",
			_ => "Steuerung",
		};
	}

	private void ShowPauseConfirmation(string text)
	{
		if (PauseStatusLabel != null)
			PauseStatusLabel.Text = $"> {text}";

		if (PauseConfirmButton != null)
			PauseConfirmButton.Text = "> Bestätigt";

		pauseConfirmationTimer = 1.8f;
	}

	private void UpdatePauseConfirmation(float dt)
	{
		if (pauseConfirmationTimer <= 0f)
			return;

		pauseConfirmationTimer -= dt;

		if (pauseConfirmationTimer <= 0f)
			UpdatePauseSettingsStatus();
	}

	private void UpdatePauseSettingsStatus()
	{
		if (PauseStatusLabel != null)
		{
			PauseStatusLabel.Text =
				$"Aktiv: {GetControlSchemeName(PlayerFish.CurrentControlScheme)}";
		}

		if (PauseConfirmButton != null)
			PauseConfirmButton.Text = "Bestätigen";
	}

	private void SetGameplayPaused(bool paused)
	{
		if (!gameStarted || gameOverTriggered || gamePaused == paused)
			return;

		gamePaused = paused;

		if (paused)
		{
			PauseBackdrop.Show();
			PausePanel.Show();
			PauseButton.Hide();
			GetNode<ScoreManager>("/root/ScoreManager").StopScoring();
			SetWorldPhysics(false);
			UpdatePauseModeButtonLabels();
			UpdatePauseCustomButtonLabels();
			UpdatePauseSettingsStatus();
			return;
		}

		pendingPauseCustomAction = "";
		PauseCaptureLabel.Text = "";
		HidePauseMenu();
		PauseButton.Show();
		SetWorldPhysics(true);
		GetNode<ScoreManager>("/root/ScoreManager").StartScoring();
	}

	private void HidePauseMenu()
	{
		pendingPauseCustomAction = "";
		if (PauseCaptureLabel != null)
			PauseCaptureLabel.Text = "";

		PauseBackdrop?.Hide();
		PausePanel?.Hide();
	}

	private void SetWorldPhysics(bool active)
	{
		Player.SetPhysicsProcess(active);
		SetContainerPhysics(NPCContainer, active);
		SetContainerPhysics(PassiveFishContainer, active);
		SetContainerPhysics(JellyfishContainer, active);
	}

	private void SetContainerPhysics(Node container, bool active)
	{
		if (container == null)
			return;

		foreach (Node node in container.GetChildren())
			node.SetPhysicsProcess(active);
	}

	private void EndRunFromPause()
	{
		gamePaused = false;
		HidePauseMenu();
		PauseButton?.Hide();
		GameOver();
	}

	private void FinishPauseInputCapture()
	{
		pendingPauseCustomAction = "";
		PauseCaptureLabel.Text = "";
		UpdatePauseCustomButtonLabels();
		ShowPauseConfirmation("Taste gespeichert");
	}

	private StyleBoxFlat CreatePausePanelStyle(Color color)
	{
		return GameUi.CreatePanelStyle();
	}

	private void ApplyPauseButtonStyle(Button button, bool selected)
	{
		GameUi.ApplyButton(button, 15, selected);
	}

	private StyleBoxFlat CreatePauseButtonStyle(Color color)
	{
		StyleBoxFlat style = new StyleBoxFlat();
		style.BgColor = color;
		style.BorderColor = new Color(0.68f, 0.95f, 1f, 0.24f);
		style.BorderWidthLeft = 1;
		style.BorderWidthTop = 1;
		style.BorderWidthRight = 1;
		style.BorderWidthBottom = 1;
		style.CornerRadiusTopLeft = 6;
		style.CornerRadiusTopRight = 6;
		style.CornerRadiusBottomLeft = 6;
		style.CornerRadiusBottomRight = 6;
		style.ContentMarginLeft = 12;
		style.ContentMarginRight = 12;
		return style;
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!string.IsNullOrEmpty(pendingPauseCustomAction))
		{
			if (inputEvent is InputEventKey captureKey &&
				captureKey.Pressed &&
				!captureKey.Echo)
			{
				if (IsEscapeKey(captureKey))
				{
					pendingPauseCustomAction = "";
					PauseCaptureLabel.Text = "";
					MarkInputHandled();
					return;
				}

				PlayerFish.SetCustomInput(pendingPauseCustomAction, captureKey);
				MarkInputHandled();
				FinishPauseInputCapture();
				return;
			}

			if (inputEvent is InputEventMouseButton captureMouse &&
				captureMouse.Pressed)
			{
				PlayerFish.SetCustomInput(pendingPauseCustomAction, captureMouse);
				MarkInputHandled();
				FinishPauseInputCapture();
				return;
			}
		}

		if (inputEvent is InputEventKey keyEvent &&
			keyEvent.Pressed &&
			!keyEvent.Echo &&
			IsEscapeKey(keyEvent) &&
			gameStarted &&
			!gameOverTriggered)
		{
			SetGameplayPaused(!gamePaused);
			MarkInputHandled();
		}
	}

	private void MarkInputHandled()
	{
		GetViewport().SetInputAsHandled();
	}

	private bool IsEscapeKey(InputEventKey keyEvent)
	{
		return keyEvent.PhysicalKeycode == Key.Escape ||
			keyEvent.Keycode == Key.Escape;
	}

	private void CreateCountdownLabel()
	{
		CountdownLabel = new Label();
		CountdownLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		CountdownLabel.HorizontalAlignment = HorizontalAlignment.Center;
		CountdownLabel.VerticalAlignment = VerticalAlignment.Center;
		GameUi.ApplyFont(CountdownLabel, 96);
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
		GameUi.ApplyFont(LevelNoticeLabel, 30);
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

		SetContainerPhysics(JellyfishContainer, true);

		PauseButton?.Show();
		GetNode<ScoreManager>("/root/ScoreManager").StartScoring();
	}

	// =====================================
	// AGGRESSIVE NPC
	// =====================================

	void SpawnNPC()
	{
		if (gameStarted && currentLevel >= 2 && GD.Randf() < GetCrossingFishChance())
		{
			SpawnCrossingNPC();
			return;
		}

		NPCFish npc = NPCFishScene.Instantiate<NPCFish>();

		ConfigureNPCFish(npc);
		npc.Position = GetDirectionalEnemySpawnPosition();
		npc.Player = Player;
		npc.Speed *= npcSpeedMultiplier * GetNewEnemySpeedBoost();
		npc.SetPhysicsProcess(gameStarted);

		NPCContainer.AddChild(npc);
	}

	private void SpawnCrossingNPC()
	{
		NPCFish npc = NPCFishScene.Instantiate<NPCFish>();
		ConfigureNPCFish(npc);
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
		npc.Speed *= npcSpeedMultiplier * GetCrossingEnemySpeedBoost();
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
	// JELLYFISH
	// =====================================

	void SpawnJellyfish()
	{
		if (JellyfishScene == null ||
			JellyfishContainer == null ||
			currentLevel < MinJellyfishLevel)
		{
			return;
		}

		Jellyfish jellyfish = JellyfishScene.Instantiate<Jellyfish>();

		jellyfish.Position = GetSafeSpawnPosition();
		jellyfish.Player = Player;
		jellyfish.Speed *= jellyfishSpeedMultiplier;
		jellyfish.SetPhysicsProcess(gameStarted && !gamePaused);

		JellyfishContainer.AddChild(jellyfish);
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

		if (CountLiveItems() >= GetMaxItemsInWorld())
			return;

		if (GD.Randf() > GetItemSpawnChance())
			return;

		Vector2? spawnPos = GetItemSpawnPosition();

		if (spawnPos == null)
			return;

		PickupItem item = InstantiateItem(type.Value);
		item.Type = type.Value;
		item.Position = spawnPos.Value;
		ItemContainer.AddChild(item);

		if (IsUsefulHintItem(type.Value) && itemHintTimer <= 0f && GD.Randf() < 0.35f)
		{
			itemHintTimer = ItemHintDuration;
			PositionItemDirectionHint(item);
			ShowItemDirectionHint();
		}
	}

	private PickupItem InstantiateItem(ItemType type)
	{
		return type switch
		{
			ItemType.Alcohol => AlcoholItemScene.Instantiate<PickupItem>(),
			ItemType.ChorusFruit => ChorusFruitItemScene.Instantiate<PickupItem>(),
			ItemType.Trash => TrashItemScene.Instantiate<PickupItem>(),
			_ => TrashItemScene.Instantiate<PickupItem>(),
		};
	}

	private ItemType? PickRandomItemType()
	{
		var pool = new List<ItemType>();

		if (currentLevel >= MinChorusFruitLevel)
		{
			pool.Add(ItemType.ChorusFruit);
			if (currentLevel <= 3)
				pool.Add(ItemType.ChorusFruit);
		}

		if (currentLevel >= MinTrashLevel)
		{
			pool.Add(ItemType.Trash);
			if (currentLevel >= 5)
				pool.Add(ItemType.Trash);
		}

		if (currentLevel >= MinAlcoholLevel)
			pool.Add(ItemType.Alcohol);

		if (pool.Count == 0)
			return null;

		return pool[(int)(GD.Randi() % pool.Count)];
	}

	private int GetMaxItemsInWorld()
	{
		if (currentLevel >= 5)
			return Mathf.Max(MaxItemsInWorld, 2);

		return MaxItemsInWorld;
	}

	private float GetItemSpawnChance()
	{
		if (currentLevel < MinChorusFruitLevel)
			return 0f;

		return currentLevel switch
		{
			2 => ItemSpawnChance * 0.72f,
			3 => ItemSpawnChance,
			4 => ItemSpawnChance * 1.22f,
			_ => ItemSpawnChance * 1.42f,
		};
	}

	private bool IsUsefulHintItem(ItemType type)
	{
		return type == ItemType.Alcohol ||
			type == ItemType.ChorusFruit;
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
			case ItemType.Trash:
				ApplyTrashEffect();
				break;
		}
	}

	private void ApplyTrashEffect()
	{
		Player.ApplySlow(TrashSlowMultiplier, TrashSlowDuration);
		ShowLevelNotice("Müll: du schwimmst langsamer!");
		UpdateItemEffectLabel();
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
		GameUi.ApplyLabel(ItemEffectLabel, 17, new Color(0.88f, 1f, 0.95f));
		ItemEffectLabel.Hide();

		GetNode<CanvasLayer>("UI").AddChild(ItemEffectLabel);
	}

	private void CreateItemDirectionHint()
	{
		ItemHintArrow = new TextureRect();
		ItemHintArrow.Texture = ResourceLoader.Load<Texture2D>("res://Assets/Richtungszeiger.png");
		ItemHintArrow.CustomMinimumSize = new Vector2(96f, 58f);
		ItemHintArrow.PivotOffset = new Vector2(48f, 29f);
		ItemHintArrow.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		ItemHintArrow.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		ItemHintArrow.MouseFilter = Control.MouseFilterEnum.Ignore;
		ItemHintArrow.Modulate = new Color(1f, 1f, 1f, 0.92f);
		ItemHintArrow.Hide();

		ItemHintText = new Label();
		ItemHintText.Text = "Item";
		ItemHintText.CustomMinimumSize = new Vector2(76f, 22f);
		ItemHintText.HorizontalAlignment = HorizontalAlignment.Center;
		ItemHintText.MouseFilter = Control.MouseFilterEnum.Ignore;
		GameUi.ApplyLabel(ItemHintText, 15, new Color(0.95f, 1f, 0.86f, 0.9f));
		ItemHintText.Hide();

		CanvasLayer ui = GetNode<CanvasLayer>("UI");
		ui.AddChild(ItemHintArrow);
		ui.AddChild(ItemHintText);
		ResetItemHintCooldown();
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

		if (Player.SpeedMultiplier < 0.99f)
		{
			ItemEffectLabel.Text =
				$"Verlangsamt: {Player.SpeedMultiplier * 100f:0}% Tempo";
			ItemEffectLabel.Show();
			return;
		}

		ItemEffectLabel.Hide();
	}

	private void UpdateItemDirectionHint(float dt)
	{
		if (ItemHintArrow == null)
			return;

		if (!gameStarted)
		{
			HideItemDirectionHint();
			return;
		}

		if (itemHintTimer > 0f)
		{
			itemHintTimer -= dt;
			Node2D nearest = FindNearestUsefulItem();

			if (nearest == null || itemHintTimer <= 0f)
			{
				itemHintTimer = 0f;
				HideItemDirectionHint();
				return;
			}

			PositionItemDirectionHint(nearest);
			ShowItemDirectionHint();
			return;
		}

		itemHintCooldown -= dt;

		if (itemHintCooldown > 0f)
			return;

		ResetItemHintCooldown();

		if (GD.Randf() > ItemHintChance)
			return;

		Node2D item = FindNearestUsefulItem();

		if (item == null)
			return;

		itemHintTimer = ItemHintDuration;
		PositionItemDirectionHint(item);
		ShowItemDirectionHint();
	}

	private void PositionItemDirectionHint(Node2D item)
	{
		Vector2 direction = item.GlobalPosition - Player.GlobalPosition;

		if (direction.LengthSquared() < 0.01f)
			direction = Vector2.Right;

		direction = direction.Normalized();
		Vector2 viewport = GetViewportRect().Size;
		float radius = Mathf.Min(ItemHintScreenRadius, Mathf.Min(viewport.X, viewport.Y) * 0.31f);
		Vector2 arrowCenter = viewport * 0.5f + direction * radius;

		ItemHintArrow.Position = arrowCenter - new Vector2(48f, 29f);
		ItemHintArrow.Rotation = direction.Angle();
		ItemHintArrow.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(itemHintTimer, 0f, 1f));

		ItemHintText.Position = arrowCenter + new Vector2(-38f, 30f);
		ItemHintText.Modulate = ItemHintArrow.Modulate;
	}

	private Node2D FindNearestUsefulItem()
	{
		if (ItemContainer == null)
			return null;

		Node2D nearest = null;
		float nearestDistance = float.MaxValue;

		foreach (Node child in ItemContainer.GetChildren())
		{
			if (child is not Node2D item || item.IsQueuedForDeletion())
				continue;

			if (item is PickupItem pickup && !IsUsefulHintItem(pickup.Type))
				continue;

			float distance = Player.GlobalPosition.DistanceSquaredTo(item.GlobalPosition);

			if (distance >= nearestDistance)
				continue;

			nearestDistance = distance;
			nearest = item;
		}

		return nearest;
	}

	private void ShowItemDirectionHint()
	{
		ItemHintArrow?.Show();
		ItemHintText?.Show();
	}

	private void HideItemDirectionHint()
	{
		ItemHintArrow?.Hide();
		ItemHintText?.Hide();
	}

	private void ResetItemHintCooldown()
	{
		itemHintCooldown = (float)GD.RandRange(ItemHintMinDelay, ItemHintMaxDelay);
	}

	private Vector2 GetSafeSpawnPosition(bool preferChaseAngle = false)
	{
		Vector2 fallback = Player.Position + Vector2.Right * MaxSpawnDistance;
		float bestClearance = -1f;

		for (int i = 0; i < MaxSpawnAttempts; i++)
		{
			float angle = GetSpawnAngle(preferChaseAngle, i);
			float distance = (float)GD.RandRange(MinSpawnDistance, MaxSpawnDistance);
			Vector2 candidate = Player.Position + Vector2.FromAngle(angle) * distance;

			if (IsSpawnPositionSafe(candidate))
				return candidate;

			float clearance = GetSpawnClearance(candidate);
			if (clearance > bestClearance)
			{
				bestClearance = clearance;
				fallback = candidate;
			}
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
		float bestClearance = -1f;

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

			float clearance = GetSpawnClearance(candidate);
			if (candidate.DistanceTo(Player.Position) >= MinSpawnDistance &&
				clearance > bestClearance)
			{
				bestClearance = clearance;
				fallback = candidate;
			}
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
		return currentLevel switch
		{
			>= 5 => 1.2f,
			4 => 1.14f,
			3 => 1.09f,
			2 => 1.04f,
			_ => 1f,
		};
	}

	private float GetCrossingFishChance()
	{
		return currentLevel switch
		{
			2 => CrossingFishChance * 0.36f,
			3 => CrossingFishChance * 0.58f,
			4 => CrossingFishChance * 0.86f,
			>= 5 => CrossingFishChance * 1.12f,
			_ => 0f,
		};
	}

	private float GetCrossingEnemySpeedBoost()
	{
		return currentLevel switch
		{
			2 => 1.24f,
			3 => 1.38f,
			4 => 1.55f,
			>= 5 => 1.7f,
			_ => 1f,
		};
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

	private float GetSpawnClearance(Vector2 candidate)
	{
		float clearance = float.MaxValue;

		foreach (Node2D node in GetAllStreamedNodes())
			clearance = Mathf.Min(clearance, candidate.DistanceTo(node.Position));

		return clearance == float.MaxValue ? MaxSpawnDistance : clearance;
	}

	private List<Node2D> GetAllStreamedNodes()
	{
		List<Node2D> nodes = new List<Node2D>();

		AddNode2DChildren(nodes, NPCContainer);
		AddNode2DChildren(nodes, PassiveFishContainer);
		AddNode2DChildren(nodes, JellyfishContainer);
		AddNode2DChildren(nodes, CoinContainer);
		AddNode2DChildren(nodes, ObstacleContainer);
		if (ItemContainer != null)
			AddNode2DChildren(nodes, ItemContainer);

		return nodes;
	}

	private void AddNode2DChildren(List<Node2D> nodes, Node container)
	{
		if (container == null)
			return;

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
		DespawnFarChildren(JellyfishContainer);
		DespawnFarChildren(CoinContainer);
		DespawnFarChildren(ObstacleContainer);
		DespawnFarChildren(ItemContainer);

		if (Player.Position.DistanceTo(lastStreamPosition) < SpawnMovementStep)
			return;

		lastStreamPosition = Player.Position;

		FillContainer(NPCContainer, TargetNPCCount, SpawnNPC);
		FillContainer(PassiveFishContainer, TargetPassiveFishCount, SpawnPassiveFish);
		FillContainer(JellyfishContainer, TargetJellyfishCount, SpawnJellyfish);
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
		if (container == null)
			return;

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
		int targetLevel = 1;

		if (score >= LevelFiveScore)
			targetLevel = 5;
		else if (score >= LevelFourScore)
			targetLevel = 4;
		else if (score >= LevelThreeScore)
			targetLevel = 3;
		else if (score >= LevelTwoScore)
			targetLevel = 2;

		if (targetLevel > currentLevel)
			SetDifficultyLevel(targetLevel);
	}

	private void SetDifficultyLevel(int level)
	{
		currentLevel = level;
		ApplyLevelSettings(level, true);
	}

	private void ApplyLevelSettings(int level, bool showNotice)
	{
		switch (level)
		{
			case 1:
				TargetNPCCount = 7;
				TargetPassiveFishCount = 14;
				TargetJellyfishCount = 0;
				TargetObstacleCount = 10;
				MinSpawnSpacing = 170f;
				SpawnCheckInterval = 0.35f;
				ApplySpeedMultiplier(1f, 1f, 1f);
				break;

			case 2:
				TargetNPCCount = 8;
				TargetPassiveFishCount = 15;
				TargetJellyfishCount = 2;
				TargetObstacleCount = 11;
				MinSpawnSpacing = 185f;
				SpawnCheckInterval = 0.31f;
				ApplySpeedMultiplier(1.08f, 1.03f, 1.04f);
				break;

			case 3:
				TargetNPCCount = 9;
				TargetPassiveFishCount = 16;
				TargetJellyfishCount = 3;
				TargetObstacleCount = 12;
				MinSpawnSpacing = 205f;
				SpawnCheckInterval = 0.27f;
				ApplySpeedMultiplier(1.18f, 1.08f, 1.1f);
				break;

			case 4:
				TargetNPCCount = 11;
				TargetPassiveFishCount = 18;
				TargetJellyfishCount = 5;
				TargetObstacleCount = 13;
				MinSpawnSpacing = 220f;
				SpawnCheckInterval = 0.24f;
				ApplySpeedMultiplier(1.29f, 1.13f, 1.18f);
				break;

			default:
				TargetNPCCount = 13;
				TargetPassiveFishCount = 19;
				TargetJellyfishCount = 7;
				TargetObstacleCount = 14;
				MinSpawnSpacing = 210f;
				SpawnCheckInterval = 0.21f;
				ApplySpeedMultiplier(1.4f, 1.18f, 1.28f);
				break;
		}

		if (!showNotice)
			return;

		ShowLevelNotice($"Level {level} erreicht");

		if (level >= 3)
			ScheduleLevelSwarm(level - 1);
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
			ConfigureNPCFish(npc);
			float row = i - (count - 1) * 0.5f;
			Vector2 offset =
				side * row * SwarmSpacing +
				forward * (float)GD.RandRange(-48f, 48f);

			npc.Position = center + offset;
			npc.Player = Player;
			npc.Speed *= npcSpeedMultiplier * 1.14f;
			npc.ApproachOffsetStrength *= 1.15f;
			npc.SeparationRadius *= 1.18f;
			npc.SetPhysicsProcess(gameStarted);

			NPCContainer.AddChild(npc);
		}
	}

	private void ConfigureNPCFish(NPCFish npc)
	{
		float variantTwoChance = currentLevel switch
		{
			>= 5 => 0.58f,
			4 => 0.46f,
			3 => 0.3f,
			_ => 0f,
		};

		NPCFish.EnemySkin skin = GD.Randf() < variantTwoChance
			? NPCFish.EnemySkin.Gegnerfisch2
			: NPCFish.EnemySkin.Gegnerfisch;

		npc.ConfigureSkin(skin);
	}

	private void ApplySpeedMultiplier(
		float newNpcMultiplier,
		float newPassiveMultiplier,
		float newJellyfishMultiplier
	)
	{
		float npcRatio = newNpcMultiplier / npcSpeedMultiplier;
		float passiveRatio = newPassiveMultiplier / passiveSpeedMultiplier;
		float jellyfishRatio = newJellyfishMultiplier / jellyfishSpeedMultiplier;

		npcSpeedMultiplier = newNpcMultiplier;
		passiveSpeedMultiplier = newPassiveMultiplier;
		jellyfishSpeedMultiplier = newJellyfishMultiplier;

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

		if (JellyfishContainer == null)
			return;

		foreach (Node node in JellyfishContainer.GetChildren())
		{
			if (node is Jellyfish jellyfish)
				jellyfish.Speed *= jellyfishRatio;
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
		if (MenuPreviewMode)
			return;

		if (!gameStarted || gamePaused)
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

		if (JellyfishContainer != null)
		{
			foreach (Node node in JellyfishContainer.GetChildren())
			{
				if (node is Jellyfish jellyfish)
				{
					float dist = Player.Position.DistanceTo(jellyfish.Position);

					float realDist =
						dist - (Player.CollisionRadius + jellyfish.CollisionRadius);

					if (realDist <= 0f)
					{
						GameOver();
						return;
					}

					threatPressure +=
						CalculateProximityPressure(realDist, JellyfishDangerRadius) *
						JellyfishStressWeight;

					contactPressure +=
						CalculateContactPressure(realDist, JellyfishContactRadius) *
						JellyfishStressWeight;
				}
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
		gamePaused = false;

		var sm = GetNode<ScoreManager>("/root/ScoreManager");
		sm.StopScoring();
		sm.SaveDeathState(Player.GlobalPosition, currentLevel, Stress);

		Player.SetPhysicsProcess(false);

		foreach (Node node in NPCContainer.GetChildren())
			node.SetPhysicsProcess(false);

		foreach (Node node in PassiveFishContainer.GetChildren())
			node.SetPhysicsProcess(false);

		SetContainerPhysics(JellyfishContainer, false);

		invincibilityTimer = 0f;
		HideItemDirectionHint();
		HidePauseMenu();
		PauseButton?.Hide();

		SceneTransition.FadeToScene(
			GetTree(),
			"res://Scenes/nameinput.tscn",
			0.38f
		);
	}
}
