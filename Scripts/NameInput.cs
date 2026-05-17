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
	private Sprite2D[] chasingFish = new Sprite2D[3];
	private Control resultPanelHost;
	private float effectTimer = 0f;
	private bool scoreSaved = false;
	private const FishSwimPath DeathSwimPath = FishSwimPath.LeftToRight;
	private float deathPathProgress = 0f;
	private Vector2 deathLastLeaderPos = Vector2.Zero;
	private const float DeathSwimSpeed = 58f;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		ClearSceneChildren();
		BuildLayout();
		nameField.GrabFocus();
		SceneTransition.FadeIn(GetTree(), 0.26f);
	}

	public override void _Process(double delta)
	{
		effectTimer += (float)delta;
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

		Label title = CreateLabel("Runde beendet", 40, new Color(0.94f, 1f, 0.98f));
		title.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(title);

		Label subtitle = CreateLabel("Kurz durchatmen. Das war viel auf einmal.", 18, new Color(0.78f, 0.92f, 0.95f));
		subtitle.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(subtitle);

		var sm = GetNode<ScoreManager>("/root/ScoreManager");

		Label scoreLabel = CreateLabel($"Score: {sm.CurrentScore}", 24, new Color(0.9f, 0.98f, 1f));
		scoreLabel.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(scoreLabel);

		Label coinLabel = CreateLabel(
			$"Muenzen diese Runde: {sm.CoinsThisRun}  |  Gesamt: {sm.TotalCoins}",
			20,
			new Color(0.98f, 0.9f, 0.34f)
		);
		coinLabel.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(coinLabel);

		Label prompt = CreateLabel("Name fuer die Bestenliste", 18, new Color(0.72f, 0.88f, 0.92f));
		prompt.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(prompt);

		nameField = new LineEdit();
		nameField.PlaceholderText = "Player";
		nameField.Alignment = HorizontalAlignment.Center;
		nameField.CustomMinimumSize = new Vector2(0, 48);
		nameField.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		nameField.AddThemeFontSizeOverride("font_size", 22);
		nameField.AddThemeColorOverride("font_color", new Color(0.94f, 1f, 0.98f));
		nameField.AddThemeColorOverride("font_placeholder_color", new Color(0.72f, 0.87f, 0.9f, 0.62f));
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

		Button menuButton = CreateButton("Hauptmenue");
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

		fallenPlayerFish = CreateBackdropFish("res://Assets/MainCharacter.png", new Vector2(0.3f, 0.3f), 0.42f);
		deathFishLayer.AddChild(fallenPlayerFish);

		for (int i = 0; i < chasingFish.Length; i++)
		{
			chasingFish[i] = CreateBackdropFish("res://Assets/EnemyCharacter.png", new Vector2(0.2f, 0.2f), 0.3f - i * 0.045f);
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

	private Sprite2D CreateBackdropFish(string texturePath, Vector2 scale, float alpha)
	{
		Sprite2D fish = new Sprite2D();
		fish.Texture = ResourceLoader.Load<Texture2D>(texturePath);
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
		float leaderRotation = BackdropFishSwim.GetLeaderRotation(
			deathLastLeaderPos,
			leaderPos,
			tangent
		);
		deathLastLeaderPos = leaderPos;

		float panic = Mathf.Max(0f, 1f - effectTimer / 1.6f);
		fallenPlayerFish.Position = leaderPos;
		fallenPlayerFish.Rotation = leaderRotation - 0.08f + Mathf.Sin(effectTimer * 0.9f) * 0.03f;
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
			chasingFish[i].Modulate = new Color(1f, 1f, 1f, 0.28f - i * 0.035f);
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
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.65f));
		label.AddThemeConstantOverride("shadow_offset_x", 2);
		label.AddThemeConstantOverride("shadow_offset_y", 2);
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		return label;
	}

	private Button CreateButton(string text)
	{
		Button button = new Button();
		button.Text = text;
		button.CustomMinimumSize = new Vector2(150, 42);
		button.AddThemeFontSizeOverride("font_size", 18);
		button.AddThemeStyleboxOverride("normal", CreateButtonStyle(new Color(0.03f, 0.16f, 0.22f, 0.88f)));
		button.AddThemeStyleboxOverride("hover", CreateButtonStyle(new Color(0.07f, 0.27f, 0.34f, 0.94f)));
		button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(new Color(0.02f, 0.11f, 0.16f, 0.96f)));
		button.AddThemeColorOverride("font_color", new Color(0.9f, 0.98f, 1f));
		button.AddThemeColorOverride("font_hover_color", new Color(1f, 1f, 1f));
		return button;
	}

	private StyleBoxFlat CreatePanelStyle()
	{
		StyleBoxFlat style = new StyleBoxFlat();
		style.BgColor = new Color(0.015f, 0.085f, 0.13f, 0.84f);
		style.BorderColor = new Color(0.49f, 0.86f, 0.93f, 0.56f);
		style.BorderWidthLeft = 2;
		style.BorderWidthTop = 2;
		style.BorderWidthRight = 2;
		style.BorderWidthBottom = 2;
		style.CornerRadiusTopLeft = 8;
		style.CornerRadiusTopRight = 8;
		style.CornerRadiusBottomLeft = 8;
		style.CornerRadiusBottomRight = 8;
		return style;
	}

	private StyleBoxFlat CreateInputStyle(bool focused)
	{
		StyleBoxFlat style = new StyleBoxFlat();
		style.BgColor = focused
			? new Color(0.04f, 0.18f, 0.24f, 0.9f)
			: new Color(0.015f, 0.09f, 0.13f, 0.78f);
		style.BorderColor = focused
			? new Color(0.66f, 0.96f, 1f, 0.78f)
			: new Color(0.42f, 0.74f, 0.82f, 0.45f);
		style.BorderWidthLeft = 2;
		style.BorderWidthTop = 2;
		style.BorderWidthRight = 2;
		style.BorderWidthBottom = 2;
		style.ContentMarginLeft = 14;
		style.ContentMarginRight = 14;
		style.CornerRadiusTopLeft = 7;
		style.CornerRadiusTopRight = 7;
		style.CornerRadiusBottomLeft = 7;
		style.CornerRadiusBottomRight = 7;
		return style;
	}

	private StyleBoxFlat CreateButtonStyle(Color color)
	{
		StyleBoxFlat style = new StyleBoxFlat();
		style.BgColor = color;
		style.BorderColor = new Color(0.54f, 0.86f, 0.94f, 0.48f);
		style.BorderWidthLeft = 1;
		style.BorderWidthTop = 1;
		style.BorderWidthRight = 1;
		style.BorderWidthBottom = 1;
		style.CornerRadiusTopLeft = 7;
		style.CornerRadiusTopRight = 7;
		style.CornerRadiusBottomLeft = 7;
		style.CornerRadiusBottomRight = 7;
		return style;
	}

	private void OnLineEditTextSubmitted(string text)
	{
		SaveAndExit();
	}

	private void OnButtonPressed()
	{
		SaveAndExit();
	}

	private void OnRetryButtonPressed()
	{
		SaveScore();
		SceneTransition.FadeToScene(GetTree(), "res://Scenes/main.tscn", 0.32f);
	}

	private void SaveAndExit()
	{
		SaveScore();
		SceneTransition.FadeToScene(GetTree(), "res://Scenes/MainMenu.tscn", 0.32f);
	}

	private void SaveScore()
	{
		if (scoreSaved)
			return;

		string playerName = nameField.Text.Trim();

		if (string.IsNullOrEmpty(playerName))
			playerName = "Player";

		GetNode<ScoreManager>("/root/ScoreManager").SaveScore(playerName);
		scoreSaved = true;
	}
}
