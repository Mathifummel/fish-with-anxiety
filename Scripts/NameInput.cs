using Godot;

public partial class NameInput : Control
{
	private LineEdit nameField;
	private ColorRect stressOverlay;
	private ColorRect blushOverlay;
	private VideoStreamPlayer backgroundVideo;
	private PanelContainer resultPanel;
	private CanvasLayer backdropLayer;
	private Node2D deathFishLayer;
	private Sprite2D fallenPlayerFish;
	private Sprite2D[] chasingFish = new Sprite2D[5];
	private Texture2D[] fallenFishFrames;
	private Texture2D[] chasingFishFrames;
	private Texture2D[] chasingFishAltFrames;
	private Control resultPanelHost;
	private Label timerLabel;
	private Label coinLabel;
	private Button reviveButton;
	private float effectTimer = 0f;
	private float decisionTimer = 10f;
	private bool scoreSaved = false;
	private bool decisionExpired = false;
	private const float DecisionDuration = 10f;
	private const FishSwimPath DeathSwimPath = FishSwimPath.LeftToRight;
	private float deathPathProgress = 0f;
	private Vector2 deathLastLeaderPos = Vector2.Zero;
	private const float DeathSwimSpeed = 58f;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		ClearSceneChildren();
		BuildLayout();
		CallDeferred(nameof(RumbleGameOver));
		GameUi.FocusFirstButton(this);
		SceneTransition.FadeIn(GetTree(), 0.26f);
	}

	private void RumbleGameOver()
	{
		GameUi.RumbleConnectedJoypads(0.55f, 1f, 0.55f);
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		GameUi.EnsureInputDefaults();

		if (GameUi.IsCancelPressed(inputEvent))
		{
			GetViewport().SetInputAsHandled();
			GameUi.RumbleConnectedJoypads(0.15f, 0.42f, 0.1f);
			SaveAndExit();
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		effectTimer += dt;
		UpdateDecisionTimer(dt);
		AnimateBackground();
		AnimateDeathBackdrop();
		AnimateResultPanel();

		if (stressOverlay == null || blushOverlay == null)
			return;

		float calm = Mathf.Clamp(effectTimer / 2.2f, 0f, 1f);
		float stressPulse =
			(Mathf.Sin(effectTimer * 16f) + 1f) * 0.5f;
		float blushPulse =
			(Mathf.Sin(effectTimer * 9f + 1.4f) + 1f) * 0.5f;
		float effectStrength = Mathf.Lerp(1f, 0.35f, calm);

		stressOverlay.Color = new Color(
			1f,
			0.08f,
			0.08f,
			(0.08f + stressPulse * 0.12f) * effectStrength
		);

		blushOverlay.Color = new Color(
			1f,
			0.2f,
			0.42f,
			(0.04f + blushPulse * 0.08f) * effectStrength
		);
	}

	private void ClearSceneChildren()
	{
		foreach (Node child in GetChildren())
			child.QueueFree();
	}

	private void BuildLayout()
	{
		backdropLayer = new CanvasLayer();
		backdropLayer.Layer = -2;
		AddChild(backdropLayer);

		AddVideoBackground();
		AddDeathBackdropFish();
		AddTintOverlay();
		AddDeathEffectOverlay();

		MarginContainer pageMargin = new MarginContainer();
		pageMargin.SetAnchorsPreset(LayoutPreset.FullRect);
		pageMargin.AddThemeConstantOverride("margin_left", 28);
		pageMargin.AddThemeConstantOverride("margin_top", 24);
		pageMargin.AddThemeConstantOverride("margin_right", 28);
		pageMargin.AddThemeConstantOverride("margin_bottom", 24);
		AddChild(pageMargin);

		VBoxContainer page = new VBoxContainer();
		page.Alignment = BoxContainer.AlignmentMode.Center;
		page.AddThemeConstantOverride("separation", 14);
		page.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		page.SizeFlagsVertical = SizeFlags.ExpandFill;
		pageMargin.AddChild(page);

		TextureRect logo = new TextureRect();
		logo.Texture = ResourceLoader.Load<Texture2D>("res://Assets/Logo.png");
		logo.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
		logo.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		logo.CustomMinimumSize = new Vector2(500, 96);
		logo.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		page.AddChild(logo);

		resultPanelHost = new Control();
		resultPanelHost.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		resultPanelHost.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		resultPanelHost.CustomMinimumSize = new Vector2(620, 410);
		page.AddChild(resultPanelHost);

		resultPanel = new PanelContainer();
		resultPanel.SetAnchorsPreset(LayoutPreset.FullRect);
		resultPanel.AddThemeStyleboxOverride("panel", CreatePanelStyle());
		resultPanelHost.AddChild(resultPanel);

		MarginContainer panelMargin = new MarginContainer();
		panelMargin.AddThemeConstantOverride("margin_left", 34);
		panelMargin.AddThemeConstantOverride("margin_top", 28);
		panelMargin.AddThemeConstantOverride("margin_right", 34);
		panelMargin.AddThemeConstantOverride("margin_bottom", 28);
		resultPanel.AddChild(panelMargin);

		VBoxContainer content = new VBoxContainer();
		content.AddThemeConstantOverride("separation", 13);
		panelMargin.AddChild(content);

		Label title = CreateLabel("Spiel vorbei", 40, GameUi.DarkText);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(title);

		Label subtitle = CreateLabel("Kurz durchatmen. Dein Fisch hat alles gegeben.", 18, new Color(0.05f, 0.22f, 0.34f, 0.82f));
		subtitle.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(subtitle);

		var sm = GetNode<ScoreManager>("/root/ScoreManager");

		Label scoreLabel = CreateLabel($"Score: {sm.CurrentScore}", 24, GameUi.DarkText);
		scoreLabel.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(scoreLabel);

		timerLabel = CreateLabel(
			$"Entscheidung: {DecisionDuration:0}s",
			22,
			new Color(0.64f, 0.1f, 0.14f)
		);
		timerLabel.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(timerLabel);

		coinLabel = CreateLabel(
			$"Münzen diese Runde: {sm.CoinsThisRun}  |  Gesamt: {sm.TotalCoins}",
			20,
			new Color(0.48f, 0.36f, 0.02f)
		);
		coinLabel.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(coinLabel);

		reviveButton = CreateButton($"Wiederbeleben ({ScoreManager.RevivalCost} Münzen)");
		reviveButton.CustomMinimumSize = new Vector2(340, 48);
		Texture2D heartTexture = ResourceLoader.Load<Texture2D>("res://Assets/Herz.png");
		if (heartTexture != null)
			reviveButton.Icon = heartTexture;
		reviveButton.Pressed += OnRevivePressed;
		content.AddChild(reviveButton);
		UpdateReviveButton();

		Label prompt = CreateLabel("Name für die Bestenliste", 18, new Color(0.05f, 0.22f, 0.34f, 0.82f));
		prompt.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(prompt);

		nameField = new LineEdit();
		nameField.PlaceholderText = "Dein Name";
		nameField.Alignment = HorizontalAlignment.Center;
		nameField.CustomMinimumSize = new Vector2(0, 48);
		nameField.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		GameUi.ApplyFont(nameField, 22);
		nameField.AddThemeColorOverride("font_color", GameUi.DarkText);
		nameField.AddThemeColorOverride("font_placeholder_color", new Color(0.05f, 0.22f, 0.34f, 0.62f));
		nameField.AddThemeStyleboxOverride("normal", CreateInputStyle(false));
		nameField.AddThemeStyleboxOverride("focus", CreateInputStyle(true));
		nameField.TextSubmitted += OnLineEditTextSubmitted;
		content.AddChild(nameField);

		HBoxContainer buttons = new HBoxContainer();
		buttons.Alignment = BoxContainer.AlignmentMode.Center;
		buttons.AddThemeConstantOverride("separation", 12);
		content.AddChild(buttons);

		Button saveButton = CreateButton("Speichern");
		saveButton.Pressed += OnButtonPressed;
		buttons.AddChild(saveButton);

		Button menuButton = CreateButton("Hauptmenü");
		menuButton.Pressed += SaveAndExit;
		buttons.AddChild(menuButton);

		Button retryButton = CreateButton("Nochmal");
		retryButton.Pressed += OnRetryButtonPressed;
		buttons.AddChild(retryButton);
	}

	private void AddVideoBackground()
	{
		backgroundVideo = new VideoStreamPlayer();
		backgroundVideo.Stream = ResourceLoader.Load<VideoStream>("res://Assets/underwater.ogv");
		backgroundVideo.SpeedScale = 2.57f;
		backgroundVideo.Autoplay = true;
		backgroundVideo.Expand = true;
		backgroundVideo.Loop = true;
		backgroundVideo.SetAnchorsPreset(LayoutPreset.FullRect);
		backgroundVideo.PivotOffset = GetViewportRect().Size * 0.5f;
		backdropLayer.AddChild(backgroundVideo);
	}

	private void AddDeathBackdropFish()
	{
		deathFishLayer = new Node2D();
		deathFishLayer.Name = "DeathBackdropFish";
		backdropLayer.AddChild(deathFishLayer);

		fallenFishFrames = new Texture2D[]
		{
			ResourceLoader.Load<Texture2D>("res://Assets/Fisch_1 1.png"),
			ResourceLoader.Load<Texture2D>("res://Assets/Fisch_2 1.png")
		};
		chasingFishFrames = new Texture2D[]
		{
			ResourceLoader.Load<Texture2D>("res://Assets/Gegnerfischframe1.png"),
			ResourceLoader.Load<Texture2D>("res://Assets/Gegnerfischframe2.png"),
			ResourceLoader.Load<Texture2D>("res://Assets/Gegnerfischframe3.png")
		};
		chasingFishAltFrames = new Texture2D[]
		{
			ResourceLoader.Load<Texture2D>("res://Assets/Gegnerfisch2frame1.png"),
			ResourceLoader.Load<Texture2D>("res://Assets/Gegnerfisch2frame2.png")
		};

		fallenPlayerFish = CreateBackdropFish(fallenFishFrames[0], new Vector2(0.88f, 0.88f), 0.42f);
		deathFishLayer.AddChild(fallenPlayerFish);

		for (int i = 0; i < chasingFish.Length; i++)
		{
			Texture2D texture = i % 2 == 0 ? chasingFishFrames[0] : chasingFishAltFrames[0];
			chasingFish[i] = CreateBackdropFish(texture, new Vector2(0.9f - i * 0.035f, 0.9f - i * 0.035f), 0.32f - i * 0.035f);
			chasingFish[i].FlipH = true;
			deathFishLayer.AddChild(chasingFish[i]);
		}

		Vector2 viewport = GetViewportRect().Size;
		deathLastLeaderPos = BackdropFishSwim.SamplePosition(
			DeathSwimPath,
			viewport,
			0f,
			0f,
			10f,
			out _
		);
	}

	private Sprite2D CreateBackdropFish(Texture2D texture, Vector2 scale, float alpha)
	{
		Sprite2D fish = new Sprite2D();
		fish.Texture = texture;
		fish.Scale = scale;
		fish.Modulate = new Color(1f, 1f, 1f, alpha);
		return fish;
	}

	private void AnimateBackground()
	{
		if (backgroundVideo == null)
			return;

		float calm = Mathf.Clamp(effectTimer / 2.4f, 0f, 1f);
		float driftX = Mathf.Sin(effectTimer * 0.1f) * Mathf.Lerp(22f, 8f, calm);
		float driftY = Mathf.Cos(effectTimer * 0.085f) * Mathf.Lerp(16f, 6f, calm);
		float zoom = 1.04f + Mathf.Sin(effectTimer * 0.06f) * Mathf.Lerp(0.016f, 0.006f, calm);

		backgroundVideo.OffsetLeft = -44f + driftX;
		backgroundVideo.OffsetTop = -34f + driftY;
		backgroundVideo.OffsetRight = 44f + driftX;
		backgroundVideo.OffsetBottom = 34f + driftY;
		backgroundVideo.Scale = new Vector2(zoom, zoom);
	}

	private void AnimateDeathBackdrop()
	{
		if (fallenPlayerFish == null)
			return;

		Vector2 viewport = GetViewportRect().Size;
		float pathLength = Mathf.Max(BackdropFishSwim.GetPathLength(DeathSwimPath, viewport), 1f);
		float delta = (float)GetProcessDeltaTime();
		float exhaustion = Mathf.Clamp(effectTimer / 3.5f, 0f, 1f);
		float currentSpeed = Mathf.Lerp(DeathSwimSpeed, DeathSwimSpeed * 0.72f, exhaustion);

		deathPathProgress += delta * currentSpeed / pathLength;

		if (deathPathProgress >= 1f)
		{
			deathPathProgress = 0f;
			deathLastLeaderPos = BackdropFishSwim.SamplePosition(
				DeathSwimPath,
				viewport,
				0f,
				effectTimer,
				10f,
				out _
			);
		}

		float wobbleStrength = Mathf.Lerp(14f, 8f, exhaustion);
		Vector2 leaderPos = BackdropFishSwim.SamplePosition(
			DeathSwimPath,
			viewport,
			deathPathProgress,
			effectTimer * 0.85f,
			wobbleStrength,
			out Vector2 tangent
		);
		deathLastLeaderPos = leaderPos;

		float panic = Mathf.Max(0f, 1f - effectTimer / 1.6f);
		if (fallenFishFrames != null && fallenFishFrames.Length > 0)
			fallenPlayerFish.Texture = fallenFishFrames[Mathf.PosMod((int)(effectTimer * 5.8f), fallenFishFrames.Length)];

		fallenPlayerFish.Position = leaderPos;
		BackdropFishSwim.ApplyUprightRotation(
			fallenPlayerFish,
			tangent,
			-0.08f + Mathf.Sin(effectTimer * 0.9f) * 0.03f
		);
		fallenPlayerFish.Modulate = new Color(0.94f, 0.99f, 0.98f, 0.36f + panic * 0.1f);

		BackdropFishSwim.PlaceFollowersOnPath(
			DeathSwimPath,
			viewport,
			deathPathProgress,
			effectTimer * 0.95f,
			wobbleStrength,
			chasingFish,
			0.07f,
			14f
		);

		for (int i = 0; i < chasingFish.Length; i++)
		{
			Texture2D[] frames = i % 2 == 0 ? chasingFishFrames : chasingFishAltFrames;
			if (frames != null && frames.Length > 0)
				chasingFish[i].Texture = frames[Mathf.PosMod((int)(effectTimer * (7.2f + i * 0.3f)), frames.Length)];

			chasingFish[i].FlipH = true;
			chasingFish[i].Modulate = new Color(1f, 1f, 1f, 0.3f - i * 0.035f);
		}
	}

	private void AnimateResultPanel()
	{
		if (resultPanelHost == null || resultPanel == null)
			return;

		float panic = Mathf.Max(0f, 1f - effectTimer / 1.15f);
		float shake = Mathf.Sin(effectTimer * 34f) * 2.5f * panic;
		float appear = Mathf.Clamp(effectTimer / 0.42f, 0f, 1f);

		resultPanelHost.Position = Vector2.Zero;
		resultPanelHost.Scale = Vector2.One;
		resultPanelHost.PivotOffset = resultPanelHost.CustomMinimumSize * 0.5f;

		Vector2 panelPivot = resultPanelHost.CustomMinimumSize * 0.5f;
		resultPanel.Position = new Vector2(shake, Mathf.Sin(effectTimer * 1.1f) * panic * 0.6f);
		resultPanel.PivotOffset = panelPivot;
		resultPanel.Scale = new Vector2(
			0.97f + appear * 0.03f,
			0.97f + appear * 0.03f
		);
		resultPanel.Modulate = new Color(1f, 1f, 1f, appear);
	}

	private void AddTintOverlay()
	{
		ColorRect overlay = new ColorRect();
		overlay.Color = new Color(0.01f, 0.06f, 0.09f, 0.34f);
		overlay.MouseFilter = MouseFilterEnum.Ignore;
		overlay.SetAnchorsPreset(LayoutPreset.FullRect);
		backdropLayer.AddChild(overlay);
	}

	private void AddDeathEffectOverlay()
	{
		stressOverlay = new ColorRect();
		stressOverlay.Color = new Color(1f, 0.08f, 0.08f, 0f);
		stressOverlay.MouseFilter = MouseFilterEnum.Ignore;
		stressOverlay.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(stressOverlay);

		blushOverlay = new ColorRect();
		blushOverlay.Color = new Color(1f, 0.2f, 0.42f, 0f);
		blushOverlay.MouseFilter = MouseFilterEnum.Ignore;
		blushOverlay.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(blushOverlay);
	}

	private Label CreateLabel(string text, int fontSize, Color color)
	{
		Label label = new Label();
		label.Text = text;
		GameUi.ApplyLabel(label, fontSize, color);
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		return label;
	}

	private Button CreateButton(string text)
	{
		Button button = new Button();
		button.Text = text;
		button.CustomMinimumSize = new Vector2(150, 42);
		GameUi.ApplyButton(button);
		return button;
	}

	private StyleBoxFlat CreatePanelStyle()
	{
		return GameUi.CreatePanelStyle();
	}

	private StyleBoxFlat CreateInputStyle(bool focused)
	{
		return GameUi.CreateInputStyle(focused);
	}

	private StyleBoxFlat CreateButtonStyle(Color color)
	{
		return GameUi.CreateButtonStyle(color, GameUi.ButtonBorder);
	}

	private void UpdateDecisionTimer(float dt)
	{
		if (decisionExpired)
			return;

		decisionTimer -= dt;

		if (timerLabel != null)
		{
			timerLabel.Text = decisionTimer > 0f
				? $"Entscheidung: {decisionTimer:0.0}s"
				: "Zeit abgelaufen";
		}

		UpdateReviveButton();

		if (decisionTimer <= 0f)
			ForceExitToMenu();
	}

	private void UpdateReviveButton()
	{
		if (reviveButton == null)
			return;

		var sm = GetNode<ScoreManager>("/root/ScoreManager");
		bool canAfford = sm.CanAffordRevival();
		reviveButton.Disabled = decisionExpired || !canAfford;

		if (decisionExpired)
			reviveButton.Text = "Zeit abgelaufen";
		else if (!canAfford)
			reviveButton.Text = $"Wiederbeleben ({ScoreManager.RevivalCost} Münzen) - noch zu wenig";
		else
			reviveButton.Text = $"Wiederbeleben ({ScoreManager.RevivalCost} Münzen)";
	}

	private void OnRevivePressed()
	{
		if (decisionExpired)
			return;

		var sm = GetNode<ScoreManager>("/root/ScoreManager");

		if (!sm.TryPurchaseRevival())
		{
			UpdateReviveButton();
			return;
		}

		GameUi.RumbleConnectedJoypads();
		decisionExpired = true;
		decisionTimer = 0f;

		if (coinLabel != null)
			coinLabel.Text = $"Münzen: {sm.TotalCoins} (Wiederbelebung gekauft)";

		SceneTransition.FadeToScene(GetTree(), "res://Scenes/main.tscn", 0.32f);
	}

	private void ForceExitToMenu()
	{
		if (decisionExpired)
			return;

		decisionExpired = true;
		decisionTimer = 0f;
		UpdateReviveButton();
		SaveAndExit();
	}

	private void OnLineEditTextSubmitted(string text)
	{
		if (!decisionExpired)
			decisionExpired = true;

		GameUi.RumbleConnectedJoypads();
		SaveAndExit();
	}

	private void OnButtonPressed()
	{
		if (!decisionExpired)
			decisionExpired = true;

		GameUi.RumbleConnectedJoypads();
		SaveAndExit();
	}

	private void OnRetryButtonPressed()
	{
		if (!decisionExpired)
			decisionExpired = true;

		GameUi.RumbleConnectedJoypads();
		SaveScore();
		GetNode<ScoreManager>("/root/ScoreManager").ClearRevivalState();
		SceneTransition.FadeToScene(GetTree(), "res://Scenes/main.tscn", 0.32f);
	}

	private void SaveAndExit()
	{
		GetNode<ScoreManager>("/root/ScoreManager").ClearRevivalState();
		SaveScore();
		SceneTransition.FadeToScene(GetTree(), "res://Scenes/MainMenu.tscn", 0.32f);
	}

	private void SaveScore()
	{
		if (scoreSaved)
			return;

		string playerName = nameField.Text.Trim();

		if (string.IsNullOrEmpty(playerName))
			playerName = "Spieler";

		GetNode<ScoreManager>("/root/ScoreManager").SaveScore(playerName);
		scoreSaved = true;
	}
}
