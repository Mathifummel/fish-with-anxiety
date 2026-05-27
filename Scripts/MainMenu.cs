using Godot;
using System.Collections.Generic;

public partial class MainMenu : Control
{
	private ColorRect settingsBackdrop;
	private Panel settingsPanel;
	private ColorRect tutorialBackdrop;
	private Panel tutorialPanel;
	private VBoxContainer customBindings;
	private Label captureLabel;
	private Label settingsStatusLabel;
	private TextureRect confirmationIcon;
	private Button confirmSettingsButton;
	private Button resetProgressButton;
	private string pendingCustomAction = "";
	private VideoStreamPlayer backgroundVideo;
	private Node2D menuFishLayer;
	private Sprite2D menuPlayerFish;
	private Sprite2D[] menuNpcFish = new Sprite2D[4];
	private Texture2D[] menuPlayerLeftFrames;
	private Texture2D[] menuPlayerRightFrames;
	private float visualTime = 0f;
	private float settingsConfirmationTimer = 0f;
	private float resetConfirmationTimer = 0f;
	private FishSwimPath menuSwimPath = FishSwimPath.LeftToRight;
	private float menuPathProgress = 0f;
	private Vector2 menuLastLeaderPos = Vector2.Zero;
	private const float MenuSwimSpeed = 520f;
	private const float MenuFollowerProgressLag = 0.055f;
	private const float MenuPathEndPadding = 0.32f;
	private const float MenuPlayerFrameRate = 10f;
	private const float ResetConfirmationDuration = 4f;
	private const string TutorialSeenPath = "user://tutorial_seen.save";
	private readonly Dictionary<PlayerFish.ControlScheme, Button> modeButtons =
		new Dictionary<PlayerFish.ControlScheme, Button>();
	private readonly Dictionary<string, Button> customButtons = new Dictionary<string, Button>();

	public override void _Ready()
	{
		GD.Print("Main Menu geladen");
		PlayerFish.LoadControlSettings();
		SetupMovingBackground();
		CreateMenuFishShowcase();
		CreateCoinCounter();
		CreateTutorialButton();
		CreateSettingsPanel();
		CreateTutorialPanel();
		SceneTransition.FadeIn(GetTree(), 0.32f);

		if (!HasSeenTutorial())
			CallDeferred(nameof(OpenTutorialFirstTime));
	}

	public override void _Process(double delta)
	{
		visualTime += (float)delta;
		AnimateBackground();
		AnimateMenuFish();
		UpdateSettingsConfirmation((float)delta);
		UpdateResetConfirmation((float)delta);
	}

	private void SetupMovingBackground()
	{
		backgroundVideo = GetNodeOrNull<VideoStreamPlayer>("CanvasLayer/VideoStreamPlayer");

		if (backgroundVideo == null)
			return;

		backgroundVideo.SetAnchorsPreset(LayoutPreset.FullRect);
		backgroundVideo.PivotOffset = GetViewportRect().Size * 0.5f;
		backgroundVideo.Scale = new Vector2(1.06f, 1.06f);

		CanvasLayer canvas = GetNode<CanvasLayer>("CanvasLayer");
		ColorRect depthTint = new ColorRect();
		depthTint.Name = "MenuDepthTint";
		depthTint.Color = new Color(0f, 0.05f, 0.09f, 0.28f);
		depthTint.MouseFilter = MouseFilterEnum.Ignore;
		depthTint.SetAnchorsPreset(LayoutPreset.FullRect);
		canvas.AddChild(depthTint);
		canvas.MoveChild(depthTint, 1);
	}

	private void AnimateBackground()
	{
		if (backgroundVideo == null)
			return;

		float driftX = Mathf.Sin(visualTime * 0.13f) * 24f;
		float driftY = Mathf.Cos(visualTime * 0.11f) * 16f;
		float zoom = 1.055f + Mathf.Sin(visualTime * 0.08f) * 0.018f;

		backgroundVideo.OffsetLeft = -42f + driftX;
		backgroundVideo.OffsetTop = -32f + driftY;
		backgroundVideo.OffsetRight = 42f + driftX;
		backgroundVideo.OffsetBottom = 32f + driftY;
		backgroundVideo.Scale = new Vector2(zoom, zoom);
	}

	private void CreateMenuFishShowcase()
	{
		CanvasLayer canvas = GetNode<CanvasLayer>("CanvasLayer");
		menuFishLayer = new Node2D();
		menuFishLayer.Name = "MenuFishShowcase";
		menuFishLayer.ZIndex = 1;
		canvas.AddChild(menuFishLayer);

		int logoIndex = canvas.GetNode("TextureRect").GetIndex();
		canvas.MoveChild(menuFishLayer, logoIndex);

		menuPlayerLeftFrames = new Texture2D[]
		{
			ResourceLoader.Load<Texture2D>("res://Assets/Fisch_1.png"),
			ResourceLoader.Load<Texture2D>("res://Assets/Fisch_2.png")
		};
		menuPlayerRightFrames = new Texture2D[]
		{
			ResourceLoader.Load<Texture2D>("res://Assets/Fisch_1 1.png"),
			ResourceLoader.Load<Texture2D>("res://Assets/Fisch_2 1.png")
		};

		menuPlayerFish = CreateMenuFish("res://Assets/Fisch_1 1.png", new Vector2(1.12f, 1.12f), 5);
		menuFishLayer.AddChild(menuPlayerFish);

		for (int i = 0; i < menuNpcFish.Length; i++)
		{
			menuNpcFish[i] = CreateMenuFish("res://Assets/EnemyCharacter.png", new Vector2(0.24f, 0.24f), 4 - i);
			menuFishLayer.AddChild(menuNpcFish[i]);
		}

		Vector2 viewport = GetViewportRect().Size;
		menuLastLeaderPos = BackdropFishSwim.SamplePosition(
			menuSwimPath,
			viewport,
			0f,
			0f,
			26f,
			out _
		);
	}

	private Sprite2D CreateMenuFish(string texturePath, Vector2 scale, int zIndex)
	{
		Sprite2D fish = new Sprite2D();
		fish.Texture = ResourceLoader.Load<Texture2D>(texturePath);
		fish.Scale = scale;
		fish.ZIndex = zIndex;
		fish.Modulate = new Color(1f, 1f, 1f, 0.58f);
		return fish;
	}

	private void AnimateMenuFish()
	{
		if (menuPlayerFish == null)
			return;

		Vector2 viewport = GetViewportRect().Size;
		float pathLength = Mathf.Max(BackdropFishSwim.GetPathLength(menuSwimPath, viewport), 1f);
		float delta = (float)GetProcessDeltaTime();
		menuPathProgress += delta * MenuSwimSpeed / pathLength;

		float lastFollowerLag = MenuFollowerProgressLag * menuNpcFish.Length;

		if (menuPathProgress >= 1f + lastFollowerLag + MenuPathEndPadding)
		{
			menuPathProgress = -lastFollowerLag - 0.08f;
			menuSwimPath = BackdropFishSwim.PickNextPath(menuSwimPath);
			menuLastLeaderPos = BackdropFishSwim.SamplePosition(
				menuSwimPath,
				viewport,
				0f,
				visualTime,
				30f,
				out _
			);
		}

		Vector2 leaderPos = BackdropFishSwim.SamplePosition(
			menuSwimPath,
			viewport,
			menuPathProgress,
			visualTime * 1.65f,
			30f,
			out Vector2 tangent
		);
		menuLastLeaderPos = leaderPos;

		float panicPulse = 0.5f + Mathf.Sin(visualTime * 3.4f) * 0.5f;
		menuPlayerFish.Position = leaderPos;
		UpdateMenuPlayerFrame(tangent);
		menuPlayerFish.FlipH = false;
		menuPlayerFish.Rotation =
			BackdropFishSwim.GetUprightRotation(tangent) +
			Mathf.Sin(visualTime * 2.2f) * 0.05f;
		menuPlayerFish.Modulate = new Color(0.98f, 1f, 0.99f, 0.56f + panicPulse * 0.08f);

		BackdropFishSwim.PlaceFollowersOnPath(
			menuSwimPath,
			viewport,
			menuPathProgress,
			visualTime * 1.75f,
			28f,
			menuNpcFish,
			MenuFollowerProgressLag,
			22f
		);

		for (int i = 0; i < menuNpcFish.Length; i++)
			menuNpcFish[i].Modulate = new Color(1f, 1f, 1f, 0.62f - i * 0.05f);
	}

	private void UpdateMenuPlayerFrame(Vector2 tangent)
	{
		Texture2D[] frames = tangent.X < 0f ? menuPlayerLeftFrames : menuPlayerRightFrames;

		if (frames == null || frames.Length == 0)
			return;

		int frameIndex = Mathf.PosMod((int)(visualTime * MenuPlayerFrameRate), frames.Length);
		Texture2D texture = frames[frameIndex];

		if (texture != null && menuPlayerFish.Texture != texture)
			menuPlayerFish.Texture = texture;
	}

	private void CreateCoinCounter()
	{
		var sm = GetNode<ScoreManager>("/root/ScoreManager");

		Label coinLabel = new Label();
		coinLabel.Text = $"Münzen: {sm.TotalCoins}";
		coinLabel.SetAnchorsPreset(LayoutPreset.TopRight);
		coinLabel.OffsetLeft = -230;
		coinLabel.OffsetTop = 18;
		coinLabel.OffsetRight = -24;
		coinLabel.OffsetBottom = 52;
		coinLabel.HorizontalAlignment = HorizontalAlignment.Right;
		coinLabel.AddThemeFontSizeOverride("font_size", 22);
		coinLabel.AddThemeColorOverride("font_color", new Color(0.98f, 0.9f, 0.34f));
		coinLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.72f));
		coinLabel.AddThemeConstantOverride("shadow_offset_x", 2);
		coinLabel.AddThemeConstantOverride("shadow_offset_y", 2);

		GetNode<CanvasLayer>("CanvasLayer").AddChild(coinLabel);
	}

	private void CreateTutorialButton()
	{
		Button tutorialButton = new Button();
		tutorialButton.Text = "Tutorial";
		tutorialButton.CustomMinimumSize = new Vector2(160, 42);
		tutorialButton.SetAnchorsPreset(LayoutPreset.BottomLeft);
		tutorialButton.OffsetLeft = 24;
		tutorialButton.OffsetTop = -76;
		tutorialButton.OffsetRight = 184;
		tutorialButton.OffsetBottom = -28;
		ApplyButtonStyle(tutorialButton, false);
		tutorialButton.Pressed += OnTutorialButtonPressed;

		GetNode<CanvasLayer>("CanvasLayer").AddChild(tutorialButton);
	}

	private void CreateTutorialPanel()
	{
		tutorialBackdrop = new ColorRect();
		tutorialBackdrop.Visible = false;
		tutorialBackdrop.Color = new Color(0f, 0.03f, 0.05f, 0.62f);
		tutorialBackdrop.MouseFilter = MouseFilterEnum.Stop;
		tutorialBackdrop.SetAnchorsPreset(LayoutPreset.FullRect);

		tutorialPanel = new Panel();
		tutorialPanel.Visible = false;
		tutorialPanel.AnchorLeft = 0.5f;
		tutorialPanel.AnchorTop = 0.5f;
		tutorialPanel.AnchorRight = 0.5f;
		tutorialPanel.AnchorBottom = 0.5f;
		tutorialPanel.OffsetLeft = -320;
		tutorialPanel.OffsetTop = -250;
		tutorialPanel.OffsetRight = 320;
		tutorialPanel.OffsetBottom = 250;
		tutorialPanel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle(new Color(0.015f, 0.1f, 0.15f, 0.97f))
		);

		VBoxContainer layout = new VBoxContainer();
		layout.AnchorRight = 1f;
		layout.AnchorBottom = 1f;
		layout.OffsetLeft = 32;
		layout.OffsetTop = 26;
		layout.OffsetRight = -32;
		layout.OffsetBottom = -26;
		layout.AddThemeConstantOverride("separation", 12);
		tutorialPanel.AddChild(layout);

		Label title = new Label();
		title.Text = "Tutorial";
		title.HorizontalAlignment = HorizontalAlignment.Center;
		title.AddThemeFontSizeOverride("font_size", 32);
		title.AddThemeColorOverride("font_color", new Color(0.92f, 1f, 0.98f));
		title.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.6f));
		title.AddThemeConstantOverride("shadow_offset_y", 2);
		layout.AddChild(title);

		Label text = new Label();
		text.Text =
			"Bleib so lange wie möglich am Leben und sammle Münzen für Bonuspunkte.\n\n" +
			"Halte Abstand zu anderen Fischen. Je näher sie kommen, desto stärker steigt dein Stress. Bei vollem Stress oder Kontakt ist die Runde vorbei.\n\n" +
			"Boost gibt dir Tempo und kann Stress abbauen, wenn du ihn gezielt einsetzt.\n\n" +
			"Items: Alkohol macht dich kurz unverwundbar, die Chorusfrucht teleportiert dich weg. Müll ist ein schlechtes Item und verlangsamt dich.\n\n" +
			"Der Richtungspfeil zeigt nur auf nützliche Items.";
		text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		text.AddThemeFontSizeOverride("font_size", 18);
		text.AddThemeColorOverride("font_color", new Color(0.82f, 0.96f, 0.98f));
		text.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.55f));
		text.AddThemeConstantOverride("shadow_offset_y", 1);
		text.SizeFlagsVertical = SizeFlags.ExpandFill;
		layout.AddChild(text);

		Button closeButton = CreateSettingsButton("Verstanden");
		closeButton.CustomMinimumSize = new Vector2(0, 44);
		closeButton.Pressed += CloseTutorialPanel;
		layout.AddChild(closeButton);

		CanvasLayer canvas = GetNode<CanvasLayer>("CanvasLayer");
		canvas.AddChild(tutorialBackdrop);
		canvas.AddChild(tutorialPanel);
	}

	// START BUTTON
	private void _on_button_pressed()
	{
		SceneTransition.FadeToScene(GetTree(), "res://Scenes/main.tscn");
	}

	private void OnTutorialButtonPressed()
	{
		ShowTutorialPanel();
	}

	private void OpenTutorialFirstTime()
	{
		ShowTutorialPanel();
	}

	private void ShowTutorialPanel()
	{
		tutorialBackdrop.Visible = true;
		tutorialPanel.Visible = true;
		SaveTutorialSeen();
	}

	private void CloseTutorialPanel()
	{
		tutorialBackdrop.Visible = false;
		tutorialPanel.Visible = false;
	}

	private bool HasSeenTutorial()
	{
		return FileAccess.FileExists(TutorialSeenPath);
	}

	private void SaveTutorialSeen()
	{
		var file = FileAccess.Open(TutorialSeenPath, FileAccess.ModeFlags.Write);
		file.StoreString("1");
		file.Close();
	}

	private void DeleteTutorialSeenFlag()
	{
		if (FileAccess.FileExists(TutorialSeenPath))
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(TutorialSeenPath));
	}

	private void _on_settings_button_pressed()
	{
		settingsBackdrop.Visible = true;
		settingsPanel.Visible = true;
		UpdateSettingsStatus();
		UpdateModeButtonLabels();
	}

	private void SelectControlScheme(PlayerFish.ControlScheme scheme)
	{
		PlayerFish.SetControlScheme(scheme);
		customBindings.Visible = scheme == PlayerFish.ControlScheme.Custom;
		UpdateSettingsStatus();
		UpdateModeButtonLabels();
		UpdateCustomButtonLabels();
		ShowSettingsConfirmation("Gespeichert");
	}

	private void CreateSettingsPanel()
	{
		settingsBackdrop = new ColorRect();
		settingsBackdrop.Visible = false;
		settingsBackdrop.Color = new Color(0f, 0.03f, 0.05f, 0.52f);
		settingsBackdrop.MouseFilter = MouseFilterEnum.Stop;
		settingsBackdrop.SetAnchorsPreset(LayoutPreset.FullRect);

		settingsPanel = new Panel();
		settingsPanel.Visible = false;
		settingsPanel.AnchorLeft = 0.5f;
		settingsPanel.AnchorTop = 0.5f;
		settingsPanel.AnchorRight = 0.5f;
		settingsPanel.AnchorBottom = 0.5f;
		settingsPanel.OffsetLeft = -235;
		settingsPanel.OffsetTop = -285;
		settingsPanel.OffsetRight = 235;
		settingsPanel.OffsetBottom = 285;
		settingsPanel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle(new Color(0.02f, 0.12f, 0.17f, 0.96f))
		);

		VBoxContainer layout = new VBoxContainer();
		layout.AnchorRight = 1f;
		layout.AnchorBottom = 1f;
		layout.OffsetLeft = 28;
		layout.OffsetTop = 24;
		layout.OffsetRight = -28;
		layout.OffsetBottom = -24;
		layout.AddThemeConstantOverride("separation", 9);
		settingsPanel.AddChild(layout);

		Label title = new Label();
		title.Text = "Einstellungen";
		title.HorizontalAlignment = HorizontalAlignment.Center;
		title.AddThemeFontSizeOverride("font_size", 28);
		title.AddThemeColorOverride("font_color", new Color(0.92f, 1f, 0.98f));
		title.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.55f));
		title.AddThemeConstantOverride("shadow_offset_y", 2);
		layout.AddChild(title);

		Label sectionLabel = new Label();
		sectionLabel.Text = "Steuerung";
		sectionLabel.AddThemeFontSizeOverride("font_size", 17);
		sectionLabel.AddThemeColorOverride("font_color", new Color(0.67f, 0.9f, 0.96f));
		layout.AddChild(sectionLabel);

		AddModeButton(layout, "Pfeiltasten", PlayerFish.ControlScheme.ArrowKeys);
		AddModeButton(layout, "WASD", PlayerFish.ControlScheme.WASD);
		AddModeButton(layout, "Maussteuerung", PlayerFish.ControlScheme.Mouse);
		AddModeButton(layout, "Eigene Tasten", PlayerFish.ControlScheme.Custom);

		customBindings = new VBoxContainer();
		customBindings.Visible = PlayerFish.CurrentControlScheme == PlayerFish.ControlScheme.Custom;
		customBindings.AddThemeConstantOverride("separation", 6);
		layout.AddChild(customBindings);

		AddCustomBindingButton("Hoch", PlayerFish.CustomMoveUp);
		AddCustomBindingButton("Runter", PlayerFish.CustomMoveDown);
		AddCustomBindingButton("Links", PlayerFish.CustomMoveLeft);
		AddCustomBindingButton("Rechts", PlayerFish.CustomMoveRight);
		AddCustomBindingButton("Boost", PlayerFish.CustomBoost);

		captureLabel = new Label();
		captureLabel.Text = "";
		captureLabel.HorizontalAlignment = HorizontalAlignment.Center;
		captureLabel.AddThemeFontSizeOverride("font_size", 14);
		captureLabel.AddThemeColorOverride("font_color", new Color(0.96f, 0.88f, 0.52f));
		layout.AddChild(captureLabel);

		settingsStatusLabel = new Label();
		settingsStatusLabel.HorizontalAlignment = HorizontalAlignment.Center;
		settingsStatusLabel.AddThemeFontSizeOverride("font_size", 16);
		settingsStatusLabel.AddThemeColorOverride("font_color", new Color(0.57f, 1f, 0.74f));
		layout.AddChild(settingsStatusLabel);

		HBoxContainer confirmRow = new HBoxContainer();
		confirmRow.AddThemeConstantOverride("separation", 10);
		layout.AddChild(confirmRow);

		confirmationIcon = new TextureRect();
		confirmationIcon.Texture = ResourceLoader.Load<Texture2D>("res://Assets/Bestätigung.png");
		confirmationIcon.CustomMinimumSize = new Vector2(36, 36);
		confirmationIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		confirmationIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		confirmationIcon.Hide();
		confirmRow.AddChild(confirmationIcon);

		confirmSettingsButton = CreateSettingsButton("Bestätigen");
		confirmSettingsButton.CustomMinimumSize = new Vector2(0, 42);
		confirmSettingsButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		confirmSettingsButton.Pressed += () =>
		{
			PlayerFish.SaveControlSettings();
			ShowSettingsConfirmation("Bestätigt");
		};
		confirmRow.AddChild(confirmSettingsButton);

		HBoxContainer bottomRow = new HBoxContainer();
		bottomRow.AddThemeConstantOverride("separation", 10);
		layout.AddChild(bottomRow);

		resetProgressButton = new Button();
		resetProgressButton.Text = "Spielstand löschen";
		resetProgressButton.CustomMinimumSize = new Vector2(0, 38);
		resetProgressButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		ApplyButtonStyle(resetProgressButton, false);
		resetProgressButton.Pressed += OnResetProgressPressed;
		bottomRow.AddChild(resetProgressButton);

		Button closeButton = new Button();
		closeButton.Text = "Zurück";
		closeButton.CustomMinimumSize = new Vector2(0, 38);
		closeButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		ApplyButtonStyle(closeButton, false);
		closeButton.Pressed += () =>
		{
			pendingCustomAction = "";
			captureLabel.Text = "";
			settingsBackdrop.Visible = false;
			settingsPanel.Visible = false;
		};
		bottomRow.AddChild(closeButton);

		CanvasLayer canvas = GetNode<CanvasLayer>("CanvasLayer");
		canvas.AddChild(settingsBackdrop);
		canvas.AddChild(settingsPanel);
		UpdateModeButtonLabels();
		UpdateCustomButtonLabels();
		UpdateSettingsStatus();
	}

	private void AddModeButton(VBoxContainer parent, string text, PlayerFish.ControlScheme scheme)
	{
		Button button = CreateSettingsButton(text);
		button.CustomMinimumSize = new Vector2(0, 36);
		button.Pressed += () => SelectControlScheme(scheme);
		modeButtons[scheme] = button;
		parent.AddChild(button);
	}

	private void AddCustomBindingButton(string label, string action)
	{
		Button button = CreateSettingsButton("");
		button.CustomMinimumSize = new Vector2(0, 32);
		button.Pressed += () =>
		{
			pendingCustomAction = action;
			captureLabel.Text = $"{label}: Taste oder Mausklick drücken";
		};

		customButtons[action] = button;
		customBindings.AddChild(button);
	}

	private Button CreateSettingsButton(string text)
	{
		Button button = new Button();
		button.Text = text;
		button.FocusMode = FocusModeEnum.None;
		ApplyButtonStyle(button, false);
		return button;
	}

	private void UpdateModeButtonLabels()
	{
		foreach (var pair in modeButtons)
		{
			bool selected = pair.Key == PlayerFish.CurrentControlScheme;
			pair.Value.Text = $"{(selected ? "✓ " : "")}{GetControlSchemeName(pair.Key)}";
			ApplyButtonStyle(pair.Value, selected);
		}
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

	private void ShowSettingsConfirmation(string text)
	{
		if (settingsStatusLabel != null)
			settingsStatusLabel.Text = $"✓ {text}";

		confirmationIcon?.Show();

		if (confirmSettingsButton != null)
			confirmSettingsButton.Text = "✓ Bestätigt";

		settingsConfirmationTimer = 1.8f;
	}

	private void UpdateSettingsConfirmation(float dt)
	{
		if (settingsConfirmationTimer <= 0f)
			return;

		settingsConfirmationTimer -= dt;

		if (settingsConfirmationTimer <= 0f)
			UpdateSettingsStatus();
	}

	private void UpdateResetConfirmation(float dt)
	{
		if (resetConfirmationTimer <= 0f)
			return;

		resetConfirmationTimer -= dt;

		if (resetConfirmationTimer <= 0f && resetProgressButton != null)
			resetProgressButton.Text = "Spielstand löschen";
	}

	private void OnResetProgressPressed()
	{
		if (resetConfirmationTimer <= 0f)
		{
			resetConfirmationTimer = ResetConfirmationDuration;

			if (resetProgressButton != null)
				resetProgressButton.Text = "Wirklich löschen?";

			ShowSettingsConfirmation("Zum Löschen nochmal drücken");
			return;
		}

		resetConfirmationTimer = 0f;
		GetNode<ScoreManager>("/root/ScoreManager").ResetAllProgress();
		DeleteTutorialSeenFlag();
		settingsBackdrop.Visible = false;
		settingsPanel.Visible = false;
		SceneTransition.FadeToScene(GetTree(), "res://Scenes/MainMenu.tscn", 0.2f);
	}

	private void UpdateSettingsStatus()
	{
		if (settingsStatusLabel != null)
		{
			settingsStatusLabel.Text =
				$"Aktiv: {GetControlSchemeName(PlayerFish.CurrentControlScheme)}";
		}

		if (confirmSettingsButton != null)
			confirmSettingsButton.Text = "Bestätigen";

		if (resetConfirmationTimer <= 0f && resetProgressButton != null)
			resetProgressButton.Text = "Spielstand löschen";

		confirmationIcon?.Hide();
	}

	private StyleBoxFlat CreatePanelStyle(Color color)
	{
		StyleBoxFlat style = new StyleBoxFlat();
		style.BgColor = color;
		style.BorderColor = new Color(0.36f, 0.78f, 0.9f, 0.45f);
		style.BorderWidthLeft = 2;
		style.BorderWidthTop = 2;
		style.BorderWidthRight = 2;
		style.BorderWidthBottom = 2;
		style.CornerRadiusTopLeft = 8;
		style.CornerRadiusTopRight = 8;
		style.CornerRadiusBottomLeft = 8;
		style.CornerRadiusBottomRight = 8;
		style.ShadowColor = new Color(0f, 0f, 0f, 0.42f);
		style.ShadowSize = 16;
		return style;
	}

	private void ApplyButtonStyle(Button button, bool selected)
	{
		Color normal = selected
			? new Color(0.07f, 0.34f, 0.31f, 0.96f)
			: new Color(0.03f, 0.17f, 0.23f, 0.92f);
		Color hover = selected
			? new Color(0.1f, 0.45f, 0.4f, 0.98f)
			: new Color(0.07f, 0.27f, 0.34f, 0.96f);
		Color pressed = selected
			? new Color(0.03f, 0.24f, 0.22f, 1f)
			: new Color(0.02f, 0.11f, 0.16f, 1f);

		button.AddThemeStyleboxOverride("normal", CreateButtonStyle(normal));
		button.AddThemeStyleboxOverride("hover", CreateButtonStyle(hover));
		button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(pressed));
		button.AddThemeColorOverride(
			"font_color",
			selected ? new Color(0.92f, 1f, 0.88f) : new Color(0.9f, 0.98f, 1f)
		);
		button.AddThemeFontSizeOverride("font_size", 15);
	}

	private StyleBoxFlat CreateButtonStyle(Color color)
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

	private void UpdateCustomButtonLabels()
	{
		if (customButtons.Count == 0)
			return;

		customButtons[PlayerFish.CustomMoveUp].Text =
			$"Hoch: {PlayerFish.GetCustomInputLabel(PlayerFish.CustomMoveUp)}";
		customButtons[PlayerFish.CustomMoveDown].Text =
			$"Runter: {PlayerFish.GetCustomInputLabel(PlayerFish.CustomMoveDown)}";
		customButtons[PlayerFish.CustomMoveLeft].Text =
			$"Links: {PlayerFish.GetCustomInputLabel(PlayerFish.CustomMoveLeft)}";
		customButtons[PlayerFish.CustomMoveRight].Text =
			$"Rechts: {PlayerFish.GetCustomInputLabel(PlayerFish.CustomMoveRight)}";
		customButtons[PlayerFish.CustomBoost].Text =
			$"Boost: {PlayerFish.GetCustomInputLabel(PlayerFish.CustomBoost)}";
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (string.IsNullOrEmpty(pendingCustomAction))
			return;

		if (inputEvent is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			PlayerFish.SetCustomInput(pendingCustomAction, keyEvent);
			AcceptEvent();
			FinishInputCapture();
		}
		else if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
		{
			PlayerFish.SetCustomInput(pendingCustomAction, mouseEvent);
			AcceptEvent();
			FinishInputCapture();
		}
	}

	private void FinishInputCapture()
	{
		pendingCustomAction = "";
		captureLabel.Text = "";
		UpdateCustomButtonLabels();
		ShowSettingsConfirmation("Taste gespeichert");
	}

	// LEADERBOARD
	private void OnLeaderboardPressed()
	{
		SceneTransition.FadeToScene(GetTree(), "res://Scenes/leaderboard.tscn");
	}

	private void _on_credits_button_pressed()
	{
		GD.Print("Credits gedrückt");
	}

	// QUIT BUTTON
	private void _on_exit_button_pressed()
	{
		GetTree().Quit();
	}
}
