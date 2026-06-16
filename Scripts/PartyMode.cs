using Godot;
using System.Collections.Generic;

public partial class PartyMode : Control
{
	private enum MiniGame
	{
		Catch,
		Coins,
		Cops,
		DrunkRun
	}

	private enum RoundState
	{
		Intro,
		Playing,
		RoundOver,
		MatchOver
	}

	private readonly MiniGame[] gameOrder = new MiniGame[]
	{
		MiniGame.Catch,
		MiniGame.Coins,
		MiniGame.Cops,
		MiniGame.DrunkRun
	};

	private SubViewport leftViewport;
	private SubViewport rightViewport;
	private Node2D world;
	private Camera2D leftCamera;
	private Camera2D rightCamera;
	private Label roundLabel;
	private Label timerLabel;
	private Label scoreLabel;
	private ProgressBar catchStressBar;
	private ProgressBar p1BoostBar;
	private ProgressBar p2BoostBar;
	private StyleBoxFlat catchStressFillStyle;
	private Label statusLabel;
	private Label leftHudLabel;
	private Label rightHudLabel;
	private HBoxContainer matchButtons;
	private ControllerHintBar matchHintBar;

	private Rect2 arenaBounds = new Rect2(new Vector2(-1500f, -900f), new Vector2(3000f, 1800f));
	private RandomNumberGenerator rng = new RandomNumberGenerator();
	private RoundState state = RoundState.Intro;
	private MiniGame currentGame = MiniGame.Catch;
	private bool singleGameMode = false;
	private int totalRounds = 3;
	private int roundIndex = 0;
	private int p1MatchScore = 0;
	private int p2MatchScore = 0;
	private int p1RoundScore = 0;
	private int p2RoundScore = 0;
	private float roundTimer = 45f;
	private float introTimer = 2.2f;
	private float roundOverTimer = 3.2f;
	private float catchStress = 0f;
	private string statusText = "";

	private PartyFish playerFish;
	private PartyFish enemyFish;
	private PartyFish activeRunner;
	private PartyFish activeCop;
	private readonly List<PartyFish> aiEnemies = new List<PartyFish>();
	private readonly List<PartyFish> passiveFish = new List<PartyFish>();
	private readonly List<PartyFish> runners = new List<PartyFish>();
	private readonly List<PartyFish> cops = new List<PartyFish>();
	private readonly List<PartyHazard> hazards = new List<PartyHazard>();
	private readonly List<Node2D> coins = new List<Node2D>();
	private readonly Dictionary<PartyFish, float> aiDirectionTimers = new Dictionary<PartyFish, float>();
	private readonly Dictionary<PartyFish, Vector2> aiDirections = new Dictionary<PartyFish, Vector2>();
	private readonly Dictionary<PartyFish, float> hazardHitCooldowns = new Dictionary<PartyFish, float>();

	public override void _Ready()
	{
		rng.Randomize();
		singleGameMode = PartyState.SelectedGame != PartyState.GameSelection.Party;
		totalRounds = singleGameMode ? 1 : Mathf.Clamp(PartyState.Rounds, 3, 10);
		SetAnchorsPreset(LayoutPreset.FullRect);
		BuildSplitScreen();
		BuildOverlay();
		StartNextRound();
		SceneTransition.FadeIn(GetTree(), 0.22f);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		UpdateCameras(dt);
		UpdateUi();

		if (state == RoundState.Intro)
		{
			introTimer -= dt;
			if (introTimer <= 0f)
			{
				state = RoundState.Playing;
				statusText = GetPlayingHint();
			}
			return;
		}

		if (state == RoundState.Playing)
		{
			roundTimer -= dt;
			if (roundTimer <= 0f)
				EndRoundByTimer();
			return;
		}

		if (state == RoundState.RoundOver)
		{
			roundOverTimer -= dt;
			if (roundOverTimer <= 0f)
				StartNextRound();
		}
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (matchButtons != null &&
			matchButtons.Visible &&
			GameUi.IsCancelPressed(inputEvent))
		{
			GetViewport().SetInputAsHandled();
			SceneTransition.FadeToScene(GetTree(), "res://Scenes/MainMenu.tscn", 0.3f);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (state != RoundState.Playing)
			return;

		float dt = (float)delta;
		UpdateCooldowns(dt);
		UpdateAi(dt);
		UpdateCatchStress(dt);
		if (state != RoundState.Playing)
			return;
		CheckRoundCollisions();
	}

	private void BuildSplitScreen()
	{
		HBoxContainer split = new HBoxContainer();
		split.SetAnchorsPreset(LayoutPreset.FullRect);
		split.AddThemeConstantOverride("separation", 4);
		AddChild(split);

		SubViewportContainer leftContainer = CreateViewportContainer();
		SubViewportContainer rightContainer = CreateViewportContainer();
		split.AddChild(leftContainer);
		split.AddChild(rightContainer);

		leftViewport = CreateViewport();
		rightViewport = CreateViewport();
		leftContainer.AddChild(leftViewport);
		rightContainer.AddChild(rightViewport);
		BuildViewportBackground(leftViewport);
		BuildViewportBackground(rightViewport);

		world = new Node2D();
		leftViewport.AddChild(world);
		rightViewport.World2D = leftViewport.World2D;

		leftCamera = CreateCamera();
		rightCamera = CreateCamera();
		leftViewport.AddChild(leftCamera);
		rightViewport.AddChild(rightCamera);
		leftCamera.MakeCurrent();
		rightCamera.MakeCurrent();

		ColorRect divider = new ColorRect();
		divider.Color = new Color(1f, 1f, 1f, 0.68f);
		divider.AnchorLeft = 0.5f;
		divider.AnchorRight = 0.5f;
		divider.AnchorTop = 0f;
		divider.AnchorBottom = 1f;
		divider.OffsetLeft = -2f;
		divider.OffsetRight = 2f;
		divider.MouseFilter = MouseFilterEnum.Ignore;
		AddChild(divider);
	}

	private SubViewportContainer CreateViewportContainer()
	{
		SubViewportContainer container = new SubViewportContainer();
		container.Stretch = true;
		container.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		container.SizeFlagsVertical = SizeFlags.ExpandFill;
		return container;
	}

	private SubViewport CreateViewport()
	{
		SubViewport viewport = new SubViewport();
		viewport.Size = new Vector2I(640, 720);
		viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
		viewport.TransparentBg = false;
		return viewport;
	}

	private void BuildViewportBackground(SubViewport viewport)
	{
		CanvasLayer layer = new CanvasLayer();
		layer.Layer = -20;
		viewport.AddChild(layer);

		OceanMapBackground background = new OceanMapBackground();
		background.ConfigureForScreen();
		layer.AddChild(background);

		ColorRect tint = new ColorRect();
		tint.Color = new Color(0.01f, 0.06f, 0.09f, 0.2f);
		tint.MouseFilter = MouseFilterEnum.Ignore;
		tint.SetAnchorsPreset(LayoutPreset.FullRect);
		layer.AddChild(tint);
	}

	private Camera2D CreateCamera()
	{
		Camera2D camera = new Camera2D();
		camera.PositionSmoothingEnabled = true;
		camera.PositionSmoothingSpeed = 8f;
		camera.Zoom = new Vector2(0.72f, 0.72f);
		camera.Enabled = true;
		return camera;
	}

	private void BuildOverlay()
	{
		PanelContainer topPanel = new PanelContainer();
		topPanel.SetAnchorsPreset(LayoutPreset.TopWide);
		topPanel.OffsetLeft = 130f;
		topPanel.OffsetTop = 16f;
		topPanel.OffsetRight = -130f;
		topPanel.OffsetBottom = 178f;
		topPanel.MouseFilter = MouseFilterEnum.Ignore;
		topPanel.AddThemeStyleboxOverride("panel", GameUi.CreatePanelStyle());
		AddChild(topPanel);

		VBoxContainer topContent = new VBoxContainer();
		topContent.Alignment = BoxContainer.AlignmentMode.Center;
		topContent.AddThemeConstantOverride("separation", 4);
		topPanel.AddChild(topContent);

		roundLabel = CreateLabel("", 24, GameUi.DarkText);
		roundLabel.HorizontalAlignment = HorizontalAlignment.Center;
		topContent.AddChild(roundLabel);

		timerLabel = CreateLabel("", 18, new Color(0.05f, 0.22f, 0.34f, 0.82f));
		timerLabel.HorizontalAlignment = HorizontalAlignment.Center;
		topContent.AddChild(timerLabel);

		scoreLabel = CreateLabel("", 18, GameUi.DarkText);
		scoreLabel.HorizontalAlignment = HorizontalAlignment.Center;
		topContent.AddChild(scoreLabel);

		catchStressBar = CreateStressBar();
		topContent.AddChild(catchStressBar);

		statusLabel = CreateLabel("", 16, GameUi.DarkText);
		statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
		statusLabel.SetAnchorsPreset(LayoutPreset.BottomWide);
		statusLabel.OffsetLeft = 190f;
		statusLabel.OffsetRight = -190f;
		statusLabel.OffsetTop = -68f;
		statusLabel.OffsetBottom = -22f;
		AddChild(statusLabel);

		leftHudLabel = CreateCornerLabel("Spieler 1  WASD + Leertaste", true);
		rightHudLabel = CreateCornerLabel("Spieler 2  Pfeile + Enter", false);
		p1BoostBar = CreateBoostBar(new Color(0.32f, 0.76f, 1f));
		p1BoostBar.SetAnchorsPreset(LayoutPreset.BottomLeft);
		p1BoostBar.OffsetLeft = 18f;
		p1BoostBar.OffsetRight = 278f;
		p1BoostBar.OffsetTop = -64f;
		p1BoostBar.OffsetBottom = -46f;
		p2BoostBar = CreateBoostBar(new Color(1f, 0.54f, 0.38f));
		p2BoostBar.SetAnchorsPreset(LayoutPreset.BottomRight);
		p2BoostBar.OffsetLeft = -278f;
		p2BoostBar.OffsetRight = -18f;
		p2BoostBar.OffsetTop = -64f;
		p2BoostBar.OffsetBottom = -46f;
		AddChild(p1BoostBar);
		AddChild(p2BoostBar);
		AddChild(leftHudLabel);
		AddChild(rightHudLabel);

		matchButtons = new HBoxContainer();
		matchButtons.Visible = false;
		matchButtons.Alignment = BoxContainer.AlignmentMode.Center;
		matchButtons.AddThemeConstantOverride("separation", 16);
		matchButtons.SetAnchorsPreset(LayoutPreset.CenterBottom);
		matchButtons.OffsetLeft = -230f;
		matchButtons.OffsetRight = 230f;
		matchButtons.OffsetTop = -118f;
		matchButtons.OffsetBottom = -62f;
		AddChild(matchButtons);

		Button retry = CreateButton("Nochmal");
		retry.Pressed += RestartMatch;
		matchButtons.AddChild(retry);

		Button menu = CreateButton("Hauptmenü");
		menu.Pressed += () => SceneTransition.FadeToScene(GetTree(), "res://Scenes/MainMenu.tscn", 0.3f);
		matchButtons.AddChild(menu);
	}

	private Label CreateCornerLabel(string text, bool left)
	{
		Label label = CreateLabel(text, 14, GameUi.LightText);
		label.SetAnchorsPreset(left ? LayoutPreset.BottomLeft : LayoutPreset.BottomRight);
		label.OffsetLeft = left ? 18f : -330f;
		label.OffsetRight = left ? 330f : -18f;
		label.OffsetTop = -38f;
		label.OffsetBottom = -12f;
		label.HorizontalAlignment = left ? HorizontalAlignment.Left : HorizontalAlignment.Right;
		return label;
	}

	private Label CreateLabel(string text, int size, Color color)
	{
		Label label = new Label();
		label.Text = text;
		GameUi.ApplyLabel(label, size, color);
		return label;
	}

	private Button CreateButton(string text)
	{
		Button button = new Button();
		button.Text = text;
		button.CustomMinimumSize = new Vector2(190f, 48f);
		button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		GameUi.ApplyButton(button, 16);
		return button;
	}

	private ProgressBar CreateStressBar()
	{
		ProgressBar bar = new ProgressBar();
		bar.MinValue = 0f;
		bar.MaxValue = 100f;
		bar.Value = 0f;
		bar.ShowPercentage = false;
		bar.CustomMinimumSize = new Vector2(360f, 16f);
		bar.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;

		StyleBoxFlat background = new StyleBoxFlat();
		background.BgColor = new Color(0.04f, 0.09f, 0.13f, 0.78f);
		background.BorderColor = new Color(1f, 1f, 1f, 0.45f);
		background.BorderWidthLeft = 1;
		background.BorderWidthTop = 1;
		background.BorderWidthRight = 1;
		background.BorderWidthBottom = 1;
		background.CornerRadiusTopLeft = 6;
		background.CornerRadiusTopRight = 6;
		background.CornerRadiusBottomLeft = 6;
		background.CornerRadiusBottomRight = 6;

		catchStressFillStyle = new StyleBoxFlat();
		catchStressFillStyle.BgColor = new Color(0.25f, 0.67f, 1f);
		catchStressFillStyle.CornerRadiusTopLeft = 5;
		catchStressFillStyle.CornerRadiusTopRight = 5;
		catchStressFillStyle.CornerRadiusBottomLeft = 5;
		catchStressFillStyle.CornerRadiusBottomRight = 5;

		bar.AddThemeStyleboxOverride("background", background);
		bar.AddThemeStyleboxOverride("fill", catchStressFillStyle);
		return bar;
	}

	private ProgressBar CreateBoostBar(Color fillColor)
	{
		ProgressBar bar = new ProgressBar();
		bar.MinValue = 0f;
		bar.MaxValue = 100f;
		bar.Value = 100f;
		bar.ShowPercentage = false;
		bar.CustomMinimumSize = new Vector2(260f, 16f);

		StyleBoxFlat background = new StyleBoxFlat();
		background.BgColor = new Color(0.01f, 0.05f, 0.08f, 0.82f);
		background.BorderColor = new Color(1f, 1f, 1f, 0.5f);
		background.BorderWidthLeft = 1;
		background.BorderWidthTop = 1;
		background.BorderWidthRight = 1;
		background.BorderWidthBottom = 1;
		background.CornerRadiusTopLeft = 6;
		background.CornerRadiusTopRight = 6;
		background.CornerRadiusBottomLeft = 6;
		background.CornerRadiusBottomRight = 6;

		StyleBoxFlat fill = new StyleBoxFlat();
		fill.BgColor = fillColor;
		fill.CornerRadiusTopLeft = 5;
		fill.CornerRadiusTopRight = 5;
		fill.CornerRadiusBottomLeft = 5;
		fill.CornerRadiusBottomRight = 5;

		bar.AddThemeStyleboxOverride("background", background);
		bar.AddThemeStyleboxOverride("fill", fill);
		return bar;
	}

	private void RestartMatch()
	{
		p1MatchScore = 0;
		p2MatchScore = 0;
		roundIndex = 0;
		matchButtons.Hide();
		matchHintBar?.Hide();
		StartNextRound();
	}

	private void StartNextRound()
	{
		if (roundIndex >= totalRounds)
		{
			ShowMatchOver();
			return;
		}

		roundIndex++;
		currentGame = singleGameMode
			? GetSelectedMiniGame()
			: gameOrder[(roundIndex - 1) % gameOrder.Length];
		p1RoundScore = 0;
		p2RoundScore = 0;
		introTimer = 2.2f;
		state = RoundState.Intro;
		matchButtons.Hide();
		matchHintBar?.Hide();

		ClearWorld();

		switch (currentGame)
		{
			case MiniGame.Coins:
				SetupCoins();
				break;

			case MiniGame.Cops:
				SetupCops();
				break;

			case MiniGame.DrunkRun:
				SetupDrunkRun();
				break;

			default:
				SetupCatch();
				break;
		}

		statusText = $"{GetGameName(currentGame)} startet gleich";
	}

	private void SetupCatch()
	{
		roundTimer = 45f;
		catchStress = 0f;
		arenaBounds = new Rect2(new Vector2(-2100f, -1300f), new Vector2(4200f, 2600f));
		DrawArena(false);

		playerFish = CreatePartyFish(PartyFish.VisualKind.Player, PartyFish.ControlMode.Wasd, new Vector2(-280f, 0f), 230f);
		playerFish.UsesStressBoost = true;
		playerFish.CurrentStress = 0f;
		enemyFish = CreatePartyFish(GetEnemyVisual(), PartyFish.ControlMode.Arrows, new Vector2(320f, 0f), 238f);

		for (int i = 0; i < 8; i++)
			aiEnemies.Add(CreateAiEnemy(RandomPoint(160f), i % 2 == 0));

		for (int i = 0; i < 14; i++)
			passiveFish.Add(CreatePassiveFish(RandomPoint(160f)));

		for (int i = 0; i < 5; i++)
			SpawnHazard(RandomPoint(180f), playerFish, enemyFish);
	}

	private void SetupCoins()
	{
		roundTimer = 60f;
		arenaBounds = new Rect2(new Vector2(-1250f, -760f), new Vector2(2500f, 1520f));
		DrawArena(true);

		playerFish = CreatePartyFish(PartyFish.VisualKind.Player, PartyFish.ControlMode.Wasd, new Vector2(-360f, 0f), 232f);
		enemyFish = CreatePartyFish(GetEnemyVisual(), PartyFish.ControlMode.Arrows, new Vector2(360f, 0f), 232f);

		for (int i = 0; i < 10; i++)
			SpawnHazard(RandomPoint(140f), playerFish, enemyFish);

		for (int i = 0; i < 24; i++)
			SpawnCoin(RandomPoint(90f));
	}

	private void SetupCops()
	{
		roundTimer = 180f;
		arenaBounds = new Rect2(new Vector2(-1400f, -860f), new Vector2(2800f, 1720f));
		DrawArena(true);

		for (int i = 0; i < 5; i++)
		{
			PartyFish runner = CreatePartyFish(
				PartyFish.VisualKind.Player,
				i == 0 ? PartyFish.ControlMode.Wasd : PartyFish.ControlMode.Ai,
				new Vector2(-720f, -360f + i * 180f),
				226f
			);
			runners.Add(runner);
			if (i == 0)
			{
				playerFish = runner;
				activeRunner = runner;
			}
		}

		for (int i = 0; i < 5; i++)
		{
			PartyFish cop = CreatePartyFish(
				GetEnemyVisual(),
				i == 0 ? PartyFish.ControlMode.Arrows : PartyFish.ControlMode.Ai,
				new Vector2(720f, -360f + i * 180f),
				232f
			);
			cops.Add(cop);
			if (i == 0)
			{
				enemyFish = cop;
				activeCop = cop;
			}
		}
	}

	private void SetupDrunkRun()
	{
		roundTimer = 30f;
		arenaBounds = new Rect2(new Vector2(-1250f, -760f), new Vector2(2500f, 1520f));
		DrawArena(true);

		playerFish = CreatePartyFish(PartyFish.VisualKind.Player, PartyFish.ControlMode.Wasd, new Vector2(-320f, 0f), 232f);
		enemyFish = CreatePartyFish(GetEnemyVisual(), PartyFish.ControlMode.Arrows, new Vector2(320f, 0f), 232f);
		playerFish.AlwaysPerfectBoost = true;
		enemyFish.AlwaysPerfectBoost = true;

		for (int i = 0; i < 34; i++)
			passiveFish.Add(CreatePassiveFish(RandomPoint(120f)));
	}

	private PartyFish CreatePartyFish(PartyFish.VisualKind visual, PartyFish.ControlMode controls, Vector2 position, float speed)
	{
		PartyFish fish = new PartyFish();
		fish.Configure(visual, controls);
		fish.Position = position;
		fish.BaseSpeed = speed;
		fish.Bounds = arenaBounds.Grow(-48f);
		fish.GamepadDevice = GetGamepadDeviceForControls(controls);
		world.AddChild(fish);
		return fish;
	}

	private int GetGamepadDeviceForControls(PartyFish.ControlMode controls)
	{
		return controls switch
		{
			PartyFish.ControlMode.Wasd => 0,
			PartyFish.ControlMode.Arrows => 1,
			_ => -2
		};
	}

	private PartyFish CreateAiEnemy(Vector2 position, bool variantTwo)
	{
		PartyFish fish = CreatePartyFish(
			variantTwo ? PartyFish.VisualKind.EnemyTwo : PartyFish.VisualKind.EnemyOne,
			PartyFish.ControlMode.Ai,
			position,
			variantTwo ? 128f : 138f
		);
		RegisterAi(fish);
		return fish;
	}

	private PartyFish CreatePassiveFish(Vector2 position)
	{
		PartyFish fish = CreatePartyFish(PartyFish.VisualKind.Passive, PartyFish.ControlMode.Ai, position, rng.RandfRange(72f, 108f));
		RegisterAi(fish);
		return fish;
	}

	private void RegisterAi(PartyFish fish)
	{
		aiDirectionTimers[fish] = 0f;
		aiDirections[fish] = Vector2.Right;
	}

	private PartyFish.VisualKind GetEnemyVisual()
	{
		return PartyState.OpponentSkin == NPCFish.EnemySkin.Gegnerfisch2
			? PartyFish.VisualKind.EnemyTwo
			: PartyFish.VisualKind.EnemyOne;
	}

	private void SpawnHazard(Vector2 position, params Node2D[] targets)
	{
		PartyHazard hazard = new PartyHazard();
		hazard.Position = position;
		hazard.Bounds = arenaBounds.Grow(-42f);
		world.AddChild(hazard);
		hazards.Add(hazard);
	}

	private void SpawnCoin(Vector2 position)
	{
		Node2D coin = new Node2D();
		coin.Position = position;

		Sprite2D sprite = new Sprite2D();
		sprite.Texture = ResourceLoader.Load<Texture2D>("res://Assets/münze.png");
		sprite.Scale = new Vector2(0.9f, 0.9f);
		coin.AddChild(sprite);

		world.AddChild(coin);
		coins.Add(coin);
	}

	private void DrawArena(bool bounded)
	{
		Polygon2D water = new Polygon2D();
		water.Polygon = new Vector2[]
		{
			arenaBounds.Position,
			new Vector2(arenaBounds.End.X, arenaBounds.Position.Y),
			arenaBounds.End,
			new Vector2(arenaBounds.Position.X, arenaBounds.End.Y)
		};
		water.Color = bounded
			? new Color(0.03f, 0.18f, 0.25f, 0.58f)
			: new Color(0.02f, 0.13f, 0.2f, 0.46f);
		world.AddChild(water);

		Line2D border = new Line2D();
		border.Width = bounded ? 9f : 4f;
		border.DefaultColor = bounded
			? new Color(0.86f, 0.98f, 1f, 0.82f)
			: new Color(0.86f, 0.98f, 1f, 0.32f);
		border.Closed = true;
		border.AddPoint(arenaBounds.Position);
		border.AddPoint(new Vector2(arenaBounds.End.X, arenaBounds.Position.Y));
		border.AddPoint(arenaBounds.End);
		border.AddPoint(new Vector2(arenaBounds.Position.X, arenaBounds.End.Y));
		world.AddChild(border);

		for (int i = 0; i < 36; i++)
			AddBubble(RandomPoint(40f));
	}

	private void AddBubble(Vector2 position)
	{
		Line2D bubble = new Line2D();
		bubble.Width = rng.RandfRange(1.5f, 3.2f);
		bubble.DefaultColor = new Color(0.8f, 0.96f, 1f, rng.RandfRange(0.16f, 0.34f));
		bubble.Closed = true;
		float radius = rng.RandfRange(8f, 28f);
		for (int i = 0; i < 18; i++)
			bubble.AddPoint(position + Vector2.FromAngle(i / 18f * Mathf.Tau) * radius);
		world.AddChild(bubble);
	}

	private void ClearWorld()
	{
		foreach (Node child in world.GetChildren())
			child.QueueFree();

		playerFish = null;
		enemyFish = null;
		activeRunner = null;
		activeCop = null;
		aiEnemies.Clear();
		passiveFish.Clear();
		runners.Clear();
		cops.Clear();
		hazards.Clear();
		coins.Clear();
		aiDirectionTimers.Clear();
		aiDirections.Clear();
		hazardHitCooldowns.Clear();
	}

	private Vector2 RandomPoint(float margin)
	{
		return new Vector2(
			rng.RandfRange(arenaBounds.Position.X + margin, arenaBounds.End.X - margin),
			rng.RandfRange(arenaBounds.Position.Y + margin, arenaBounds.End.Y - margin)
		);
	}

	private void UpdateCameras(float dt)
	{
		Node2D leftTarget = GetLeftCameraTarget();
		Node2D rightTarget = GetRightCameraTarget();

		if (leftCamera != null && leftTarget != null)
			leftCamera.GlobalPosition = leftTarget.GlobalPosition;

		if (rightCamera != null && rightTarget != null)
			rightCamera.GlobalPosition = rightTarget.GlobalPosition;
	}

	private Node2D GetLeftCameraTarget()
	{
		if (currentGame == MiniGame.Cops)
			return activeRunner ?? playerFish;

		return playerFish;
	}

	private Node2D GetRightCameraTarget()
	{
		if (currentGame == MiniGame.Cops)
			return activeCop ?? enemyFish;

		return enemyFish;
	}

	private void UpdateUi()
	{
		string phaseText = state == RoundState.Intro
			? $"Start in {Mathf.CeilToInt(Mathf.Max(0f, introTimer))}"
			: state == RoundState.RoundOver
				? $"Nächste Runde in {Mathf.CeilToInt(Mathf.Max(0f, roundOverTimer))}"
				: state == RoundState.MatchOver
					? "Match beendet"
					: $"{Mathf.CeilToInt(Mathf.Max(0f, roundTimer))}s";

		roundLabel.Text = $"Runde {Mathf.Min(roundIndex, totalRounds)} / {totalRounds}  -  {GetGameName(currentGame)}";
		timerLabel.Text = phaseText;
		scoreLabel.Text = $"Party-Score  P1 {p1MatchScore} : {p2MatchScore} P2    Runde  {p1RoundScore} : {p2RoundScore}";
		if (currentGame == MiniGame.Catch && state != RoundState.MatchOver)
			scoreLabel.Text += $"    Stress {Mathf.CeilToInt(catchStress)}%";
		statusLabel.Text = statusText;
		catchStressBar.Visible = currentGame == MiniGame.Catch && state != RoundState.MatchOver;
		catchStressBar.Value = catchStress;
		UpdateCatchStressBarColor();
		p1BoostBar.Value = GetBoostMeter(playerFish);
		p2BoostBar.Value = GetBoostMeter(enemyFish);

		leftHudLabel.Text = currentGame == MiniGame.Cops
			? "Spieler 1 Boost  |  kleines Team"
			: "Spieler 1 Boost  |  kleiner Fisch";
		rightHudLabel.Text = currentGame == MiniGame.Cops
			? "Spieler 2 Boost  |  Gegnerfisch-Team"
			: "Spieler 2 Boost  |  Gegnerfisch";
	}

	private float GetBoostMeter(PartyFish fish)
	{
		if (fish == null || fish.IsEliminated || state == RoundState.MatchOver)
			return 0f;

		return fish.BoostMeterValue;
	}

	private void UpdateCatchStressBarColor()
	{
		if (catchStressFillStyle == null)
			return;

		if (catchStress < 40f)
			catchStressFillStyle.BgColor = new Color(0.25f, 0.67f, 1f);
		else if (catchStress <= 60f)
			catchStressFillStyle.BgColor = new Color(0.35f, 0.95f, 0.48f);
		else
			catchStressFillStyle.BgColor = new Color(1f, 0.34f, 0.31f);
	}

	private string GetGameName(MiniGame game)
	{
		return game switch
		{
			MiniGame.Coins => "Münzen sammeln",
			MiniGame.Cops => "Räuber und Gendarm",
			MiniGame.DrunkRun => "Betrunkener Run",
			_ => "Fangen"
		};
	}

	private MiniGame GetSelectedMiniGame()
	{
		return PartyState.SelectedGame switch
		{
			PartyState.GameSelection.Coins => MiniGame.Coins,
			PartyState.GameSelection.Cops => MiniGame.Cops,
			PartyState.GameSelection.DrunkRun => MiniGame.DrunkRun,
			_ => MiniGame.Catch
		};
	}

	private string GetPlayingHint()
	{
		return currentGame switch
		{
			MiniGame.Coins => "Sammelt Münzen. Wer in eine Qualle schwimmt, lässt welche fallen.",
			MiniGame.Cops => "Das Gegnerteam muss alle kleinen Fische erwischen, bevor die Zeit abläuft.",
			MiniGame.DrunkRun => "Dauer-Boost: Berührt so viele Fische wie möglich.",
			_ => "Der Gegnerfisch hat 45 Sekunden, um den kleinen Fisch zu fangen."
		};
	}

	private void UpdateCooldowns(float dt)
	{
		List<PartyFish> keys = new List<PartyFish>(hazardHitCooldowns.Keys);
		foreach (PartyFish fish in keys)
			hazardHitCooldowns[fish] -= dt;
	}

	private void UpdateAi(float dt)
	{
		foreach (PartyFish fish in passiveFish)
			fish.AiDirection = GetWanderDirection(fish, dt);

		if (currentGame == MiniGame.Catch)
		{
			foreach (PartyFish fish in aiEnemies)
			{
				Vector2 chase = DirectionTo(fish, playerFish);
				fish.AiDirection = (chase + GetWanderDirection(fish, dt) * 0.25f).Normalized();
			}
		}
		else if (currentGame == MiniGame.Cops)
		{
			foreach (PartyFish runner in runners)
			{
				if (runner.IsEliminated || runner.Controls != PartyFish.ControlMode.Ai)
					continue;

				Node2D nearestCop = FindNearestAlive(runner, cops);
				Vector2 flee = nearestCop != null
					? (runner.GlobalPosition - nearestCop.GlobalPosition).Normalized()
					: GetWanderDirection(runner, dt);
				runner.AiDirection = (flee + GetWanderDirection(runner, dt) * 0.55f).Normalized();
			}

			foreach (PartyFish cop in cops)
			{
				if (cop.Controls != PartyFish.ControlMode.Ai || cop.IsEliminated)
					continue;

				Node2D nearestRunner = FindNearestAlive(cop, runners);
				cop.AiDirection = (DirectionTo(cop, nearestRunner) + GetWanderDirection(cop, dt) * 0.18f).Normalized();
			}
		}
	}

	private Vector2 GetWanderDirection(PartyFish fish, float dt)
	{
		if (!aiDirectionTimers.ContainsKey(fish))
			RegisterAi(fish);

		aiDirectionTimers[fish] -= dt;
		if (aiDirectionTimers[fish] <= 0f)
		{
			aiDirectionTimers[fish] = rng.RandfRange(0.7f, 2.0f);
			aiDirections[fish] = Vector2.FromAngle(rng.RandfRange(0f, Mathf.Tau));
		}

		Vector2 direction = aiDirections[fish];
		Vector2 position = fish.GlobalPosition;
		float edgeMargin = 180f;
		if (position.X < arenaBounds.Position.X + edgeMargin)
			direction.X = Mathf.Abs(direction.X);
		else if (position.X > arenaBounds.End.X - edgeMargin)
			direction.X = -Mathf.Abs(direction.X);

		if (position.Y < arenaBounds.Position.Y + edgeMargin)
			direction.Y = Mathf.Abs(direction.Y);
		else if (position.Y > arenaBounds.End.Y - edgeMargin)
			direction.Y = -Mathf.Abs(direction.Y);

		aiDirections[fish] = direction.Normalized();
		return aiDirections[fish];
	}

	private Vector2 DirectionTo(Node2D from, Node2D to)
	{
		if (from == null || to == null)
			return Vector2.Zero;

		Vector2 direction = to.GlobalPosition - from.GlobalPosition;
		return direction.LengthSquared() > 1f ? direction.Normalized() : Vector2.Zero;
	}

	private Node2D FindNearestAlive(Node2D from, List<PartyFish> candidates)
	{
		Node2D nearest = null;
		float nearestDistance = float.MaxValue;

		foreach (PartyFish candidate in candidates)
		{
			if (candidate == null || candidate.IsEliminated)
				continue;

			float distance = from.GlobalPosition.DistanceSquaredTo(candidate.GlobalPosition);
			if (distance >= nearestDistance)
				continue;

			nearestDistance = distance;
			nearest = candidate;
		}

		return nearest;
	}

	private void UpdateCatchStress(float dt)
	{
		if (currentGame != MiniGame.Catch || playerFish == null || playerFish.IsEliminated)
			return;

		float threatPressure = 0f;
		float contactPressure = 0f;

		AddStressPressure(enemyFish, 410f, 62f, 1f, ref threatPressure, ref contactPressure);

		foreach (PartyFish enemy in aiEnemies)
			AddStressPressure(enemy, 390f, 58f, 0.82f, ref threatPressure, ref contactPressure);

		foreach (PartyFish fish in passiveFish)
			AddStressPressure(fish, 235f, 34f, 0.25f, ref threatPressure, ref contactPressure);

		foreach (PartyHazard hazard in hazards)
			AddStressPressure(hazard, 330f, 48f, 0.72f, ref threatPressure, ref contactPressure);

		float targetStress = Mathf.Clamp(threatPressure * 58f + contactPressure * 28f, 0f, 100f);
		float stressSpeed = targetStress > catchStress ? 140f : 22f;
		catchStress = Mathf.MoveToward(catchStress, targetStress, stressSpeed * dt);

		if (playerFish.IsBoosting)
			catchStress = Mathf.MoveToward(catchStress, 0f, 32f * dt);

		catchStress = Mathf.Clamp(catchStress, 0f, 100f);
		playerFish.CurrentStress = catchStress;

		if (catchStress >= 100f)
		{
			playerFish.Kill();
			EndRound(2, "Der Stress war zu viel.");
		}
	}

	private void AddStressPressure(Node2D source, float dangerRadius, float contactRadius, float weight, ref float threatPressure, ref float contactPressure)
	{
		if (source == null || playerFish == null)
			return;

		if (source is PartyFish fish && fish.IsEliminated)
			return;

		float distance = playerFish.GlobalPosition.DistanceTo(source.GlobalPosition);
		float sourceRadius = source switch
		{
			PartyFish partyFish => partyFish.CollisionRadius,
			PartyHazard hazard => hazard.CollisionRadius,
			_ => 24f
		};
		float realDistance = distance - playerFish.CollisionRadius - sourceRadius;

		if (realDistance < dangerRadius)
		{
			float t = 1f - Mathf.Clamp(realDistance / dangerRadius, 0f, 1f);
			if (t > 0.25f)
			{
				t = (t - 0.25f) / 0.75f;
				threatPressure += t * t * (3f - 2f * t) * weight;
			}
		}

		if (realDistance < contactRadius)
		{
			float t = 1f - Mathf.Clamp(realDistance / contactRadius, 0f, 1f);
			contactPressure += t * weight;
		}
	}

	private void CheckRoundCollisions()
	{
		switch (currentGame)
		{
			case MiniGame.Coins:
				CheckCoinRoundCollisions();
				break;

			case MiniGame.Cops:
				CheckCopsRoundCollisions();
				break;

			case MiniGame.DrunkRun:
				CheckDrunkRoundCollisions();
				break;

			default:
				CheckCatchRoundCollisions();
				break;
		}
	}

	private void CheckCatchRoundCollisions()
	{
		if (Touches(playerFish, enemyFish))
		{
			playerFish.Kill();
			EndRound(2, "Der Gegnerfisch hat den kleinen Fisch gefangen.");
			return;
		}

		foreach (PartyFish enemy in aiEnemies)
		{
			if (Touches(playerFish, enemy))
			{
				playerFish.Kill();
				EndRound(2, "Ein Gegnerfisch hat den kleinen Fisch erwischt.");
				return;
			}
		}

		foreach (PartyHazard hazard in hazards)
		{
			if (Touches(playerFish, hazard))
			{
				playerFish.Kill();
				EndRound(2, "Der kleine Fisch ist in eine Qualle geraten.");
				return;
			}

			if (Touches(enemyFish, hazard))
			{
				enemyFish.Kill();
				EndRound(1, "Gegnerfisch wurde von einer Qualle gestoppt.");
				return;
			}
		}
	}

	private void CheckCoinRoundCollisions()
	{
		for (int i = coins.Count - 1; i >= 0; i--)
		{
			Node2D coin = coins[i];
			if (coin == null || !IsInstanceValid(coin))
			{
				coins.RemoveAt(i);
				continue;
			}

			if (Touches(playerFish, coin, 34f))
			{
				p1RoundScore++;
				coin.QueueFree();
				coins.RemoveAt(i);
				SpawnCoin(RandomPoint(90f));
			}
			else if (Touches(enemyFish, coin, 34f))
			{
				p2RoundScore++;
				coin.QueueFree();
				coins.RemoveAt(i);
				SpawnCoin(RandomPoint(90f));
			}
		}

		foreach (PartyHazard hazard in hazards)
		{
			if (Touches(playerFish, hazard))
				ApplyHazardPenalty(playerFish, true);

			if (Touches(enemyFish, hazard))
				ApplyHazardPenalty(enemyFish, false);
		}
	}

	private void ApplyHazardPenalty(PartyFish fish, bool playerOne)
	{
		if (fish == null)
			return;

		if (hazardHitCooldowns.TryGetValue(fish, out float cooldown) && cooldown > 0f)
			return;

		hazardHitCooldowns[fish] = 1.2f;
		fish.ApplyStun(1.1f);

		if (playerOne)
		{
			int dropped = Mathf.Min(3, p1RoundScore);
			p1RoundScore = Mathf.Max(0, p1RoundScore - dropped);
			DropCoinsFromFish(fish, dropped);
			statusText = dropped > 0 ? "Spieler 1 verliert Münzen durch eine Qualle." : "Spieler 1 wurde von einer Qualle gebremst.";
		}
		else
		{
			int dropped = Mathf.Min(3, p2RoundScore);
			p2RoundScore = Mathf.Max(0, p2RoundScore - dropped);
			DropCoinsFromFish(fish, dropped);
			statusText = dropped > 0 ? "Spieler 2 verliert Münzen durch eine Qualle." : "Spieler 2 wurde von einer Qualle gebremst.";
		}
	}

	private void DropCoinsFromFish(PartyFish fish, int count)
	{
		if (fish == null || count <= 0)
			return;

		for (int i = 0; i < count; i++)
		{
			float angle = rng.RandfRange(0f, Mathf.Tau);
			float distance = rng.RandfRange(96f, 172f);
			Vector2 position = fish.GlobalPosition + Vector2.FromAngle(angle) * distance;
			position.X = Mathf.Clamp(position.X, arenaBounds.Position.X + 70f, arenaBounds.End.X - 70f);
			position.Y = Mathf.Clamp(position.Y, arenaBounds.Position.Y + 70f, arenaBounds.End.Y - 70f);
			SpawnCoin(position);
		}
	}

	private void CheckCopsRoundCollisions()
	{
		foreach (PartyFish cop in cops)
		{
			if (cop.IsEliminated)
				continue;

			foreach (PartyFish runner in runners)
			{
				if (runner.IsEliminated || !Touches(cop, runner))
					continue;

				bool wasActiveRunner = runner == activeRunner;
				runner.Kill();
				p2RoundScore++;

				if (wasActiveRunner)
					AssignNextRunnerControl();

				if (AllRunnersCaught())
				{
					EndRound(2, "Alle kleinen Fische wurden gefangen.");
					return;
				}
			}
		}

		p1RoundScore = CountAlive(runners);
	}

	private void AssignNextRunnerControl()
	{
		activeRunner = null;
		foreach (PartyFish runner in runners)
		{
			if (runner.IsEliminated)
				continue;

			runner.Controls = PartyFish.ControlMode.Wasd;
			playerFish = runner;
			activeRunner = runner;
			statusText = "Spieler 1 übernimmt den nächsten kleinen Fisch.";
			return;
		}
	}

	private bool AllRunnersCaught()
	{
		foreach (PartyFish runner in runners)
		{
			if (!runner.IsEliminated)
				return false;
		}

		return true;
	}

	private int CountAlive(List<PartyFish> fishList)
	{
		int count = 0;
		foreach (PartyFish fish in fishList)
		{
			if (!fish.IsEliminated)
				count++;
		}
		return count;
	}

	private void CheckDrunkRoundCollisions()
	{
		for (int i = passiveFish.Count - 1; i >= 0; i--)
		{
			PartyFish fish = passiveFish[i];
			if (fish == null || fish.IsEliminated)
			{
				passiveFish.RemoveAt(i);
				continue;
			}

			bool p1Touch = Touches(playerFish, fish);
			bool p2Touch = Touches(enemyFish, fish);

			if (!p1Touch && !p2Touch)
				continue;

			if (p1Touch && p2Touch)
			{
				float p1Distance = playerFish.GlobalPosition.DistanceSquaredTo(fish.GlobalPosition);
				float p2Distance = enemyFish.GlobalPosition.DistanceSquaredTo(fish.GlobalPosition);
				if (p1Distance <= p2Distance)
					p1RoundScore++;
				else
					p2RoundScore++;
			}
			else if (p1Touch)
			{
				p1RoundScore++;
			}
			else
			{
				p2RoundScore++;
			}

			fish.QueueFree();
			passiveFish.RemoveAt(i);
			passiveFish.Add(CreatePassiveFish(RandomPoint(120f)));
		}
	}

	private bool Touches(PartyFish fish, Node2D other)
	{
		if (fish == null || other == null || fish.IsEliminated)
			return false;

		float otherRadius = other switch
		{
			PartyFish partyFish => partyFish.CollisionRadius,
			PartyHazard hazard => hazard.CollisionRadius,
			_ => 24f
		};

		return Touches(fish, other, fish.CollisionRadius + otherRadius);
	}

	private bool Touches(PartyFish fish, Node2D other, float radius)
	{
		if (fish == null || other == null || fish.IsEliminated)
			return false;

		return fish.GlobalPosition.DistanceSquaredTo(other.GlobalPosition) <= radius * radius;
	}

	private void EndRoundByTimer()
	{
		switch (currentGame)
		{
			case MiniGame.Catch:
				EndRound(1, "45 Sekunden überlebt.");
				break;

			case MiniGame.Coins:
				EndRound(CompareRoundScore(), BuildScoreResult("Münzen"));
				break;

			case MiniGame.Cops:
				EndRound(1, "Mindestens ein kleiner Fisch ist entkommen.");
				break;

			case MiniGame.DrunkRun:
				EndRound(CompareRoundScore(), BuildScoreResult("Treffer"));
				break;
		}
	}

	private int CompareRoundScore()
	{
		if (p1RoundScore > p2RoundScore)
			return 1;

		if (p2RoundScore > p1RoundScore)
			return 2;

		return 0;
	}

	private string BuildScoreResult(string label)
	{
		if (p1RoundScore == p2RoundScore)
			return $"Unentschieden: {p1RoundScore} {label}.";

		return $"P1 {p1RoundScore} : {p2RoundScore} P2 {label}.";
	}

	private void EndRound(int winner, string reason)
	{
		if (state != RoundState.Playing)
			return;

		state = RoundState.RoundOver;
		roundOverTimer = 3.2f;

		if (winner == 1)
		{
			p1MatchScore++;
			statusText = $"Runde an Spieler 1: {reason}";
		}
		else if (winner == 2)
		{
			p2MatchScore++;
			statusText = $"Runde an Spieler 2: {reason}";
		}
		else
		{
			statusText = $"Unentschieden: {reason}";
		}

		FreezeAllFish();
	}

	private void FreezeAllFish()
	{
		foreach (PartyFish fish in GetAllPartyFish())
			fish.Controls = PartyFish.ControlMode.None;
	}

	private List<PartyFish> GetAllPartyFish()
	{
		List<PartyFish> fish = new List<PartyFish>();
		if (playerFish != null)
			fish.Add(playerFish);
		if (enemyFish != null && enemyFish != playerFish)
			fish.Add(enemyFish);
		fish.AddRange(aiEnemies);
		fish.AddRange(passiveFish);
		fish.AddRange(runners);
		fish.AddRange(cops);
		return fish;
	}

	private void ShowMatchOver()
	{
		state = RoundState.MatchOver;
		ClearWorld();
		DrawArena(true);
		matchButtons.Show();
		ShowMatchHints();
		GameUi.FocusFirstButton(matchButtons);

		if (p1MatchScore > p2MatchScore)
			statusText = $"Spieler 1 gewinnt die Party {p1MatchScore}:{p2MatchScore}.";
		else if (p2MatchScore > p1MatchScore)
			statusText = $"Spieler 2 gewinnt die Party {p2MatchScore}:{p1MatchScore}.";
		else
			statusText = $"Party endet unentschieden {p1MatchScore}:{p2MatchScore}.";

		roundTimer = 0f;
	}

	private void ShowMatchHints()
	{
		if (matchHintBar == null)
		{
			matchHintBar = GameUi.CreateControllerHintBar(GameUi.ControllerHintMode.BackOnly);
			GameUi.PlaceControllerHintOverlay(matchHintBar, 12f);
			AddChild(matchHintBar);
			return;
		}

		matchHintBar.Show();
	}
}
