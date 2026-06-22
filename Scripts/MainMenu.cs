using Godot;
using System;
using System.Collections.Generic;

public partial class MainMenu : Control
{
	private const string ClassicScenePath = "res://Scenes/main.tscn";
	private const string LeaderboardScenePath = "res://Scenes/leaderboard.tscn";
	private const string TutorialScenePath = "res://Scenes/Tutorial.tscn";
	private const string SettingsScenePath = "res://Scenes/Settings.tscn";
	private const string CreditsScenePath = "res://Scenes/Credits.tscn";
	private const string PartySetupScenePath = "res://Scenes/PartySetup.tscn";
	private const string ShopScenePath = "res://Scenes/Shop.tscn";
	private const string MissionsScenePath = "res://Scenes/Missions.tscn";

	private const string BackgroundViewportPath = "Background/SubViewport";
	private const string PanelLeftPath = "UI/PanelLeft";
	private const string MenuBoxPath = "UI/PanelLeft/VBoxContainer";
	private const string ClassicButtonPath = "UI/PanelLeft/VBoxContainer/Klassisch";
	private const string MultiplayerButtonPath = "UI/PanelLeft/VBoxContainer/Multiplayer";
	private const string LeaderboardButtonPath = "UI/PanelLeft/VBoxContainer/Leaderboard";
	private const string TutorialButtonPath = "UI/PanelLeft/VBoxContainer/Tutorial";
	private const string SettingsButtonPath = "UI/PanelLeft/VBoxContainer/Einstellungen";
	private const string CreditsButtonPath = "UI/PanelLeft/VBoxContainer/Credits";
	private const string QuitButtonPath = "UI/PanelLeft/VBoxContainer/Beenden";
	private const string MenuLogoPath = "UI/GameLogo";
	private const string GameLogoTexturePath = "res://Assets/Transparaentlogo.png";
	private const string ShopIconTexturePath = "res://Assets/Generated/Buttons/shop_icon.png";
	private const string MissionsIconTexturePath = "res://Assets/Generated/Buttons/missions_icon.png";

	private const float HoverScale = 1.05f;
	private const float HoverTweenDuration = 0.14f;
	private const float MultiplayerNoticeDuration = 1.3f;
	private const float ControllerNoticeDuration = 3.8f;

	private readonly List<Button> menuButtons = new List<Button>();
	private readonly Dictionary<Button, Tween> hoverTweens = new Dictionary<Button, Tween>();
	private readonly Dictionary<int, string> knownJoypads = new Dictionary<int, string>();

	private SubViewport backgroundViewport;
	private Button multiplayerButton;
	private Button shopIconButton;
	private Button missionsIconButton;
	private TextureRect gameLogo;
	private Panel controllerNoticePanel;
	private TextureRect controllerNoticeImage;
	private Label controllerNoticeTitle;
	private Label controllerNoticeName;
	private ControllerHintBar controllerHintBar;
	private ButtonGroup difficultyButtonGroup;
	private float multiplayerNoticeTimer = 0f;
	private float controllerNoticeTimer = 0f;
	private readonly Dictionary<GameDifficulty, Button> difficultyButtons =
		new Dictionary<GameDifficulty, Button>();

	public override void _Ready()
	{
		SetupBackgroundGameplay();
		StartMenuMusic();
		SetupGlassPanel();
		SetupGameLogo();
		CreateControllerNotice();
		CreateControllerHints();
		CreateRightIconButtons();
		ConnectButton(ClassicButtonPath, StartClassic);
		ConnectButton(MultiplayerButtonPath, StartMultiplayer);
		ConnectButton(LeaderboardButtonPath, OpenLeaderboard);
		ConnectButton(TutorialButtonPath, OpenTutorial);
		ConnectButton(SettingsButtonPath, OpenSettings);
		ConnectButton(CreditsButtonPath, OpenCredits);
		ConnectButton(QuitButtonPath, QuitGame);
		SetupDifficultySelector();

		multiplayerButton = GetNodeOrNull<Button>(MultiplayerButtonPath);
		CallDeferred(nameof(RefreshButtonPivots));
		GameUi.FocusFirstButton(this);
		CallDeferred(nameof(ConfigureMainMenuFocusNavigation));
		SyncConnectedJoypads();
		SceneTransition.FadeIn(GetTree(), 0.28f);
	}

	public override void _Process(double delta) 
	{
		float dt = (float)delta;
		SyncConnectedJoypads();
		UpdateControllerNotice(dt);

		if (multiplayerNoticeTimer > 0f && multiplayerButton != null)
		{
			multiplayerNoticeTimer -= dt;

			if (multiplayerNoticeTimer <= 0f)
				multiplayerButton.Text = "2-Spieler Modus";
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationResized)
		{
			ResizeBackgroundViewport();
			ResizeGameLogo();
			PositionControllerNotice();
			GameUi.PlaceControllerHintOverlay(controllerHintBar);
		}
	}

	public void StartClassic()
	{
		ChangeScene(ClassicScenePath);
	}

	public void StartMultiplayer()
	{
		ChangeScene(PartySetupScenePath);
	}

	public void OpenLeaderboard()
	{
		ChangeScene(LeaderboardScenePath);
	}

	public void OpenTutorial()
	{
		ChangeScene(TutorialScenePath);
	}

	public void OpenShop()
	{
		ChangeScene(ShopScenePath);
	}

	public void OpenMissions()
	{
		ChangeScene(MissionsScenePath);
	}

	public void OpenSettings()
	{
		ChangeScene(SettingsScenePath);
	}

	public void OpenCredits()
	{
		ChangeScene(CreditsScenePath);
	}

	public void QuitGame()
	{
		GetTree().Quit();
	}

	private void SetupBackgroundGameplay()
	{
		backgroundViewport = GetNodeOrNull<SubViewport>(BackgroundViewportPath);

		if (backgroundViewport == null)
		{
			GD.PushError($"MainMenu is missing SubViewport at '{BackgroundViewportPath}'.");
			return;
		}

		backgroundViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
		ResizeBackgroundViewport();

		MenuChaseBackdrop titleBackdrop = new MenuChaseBackdrop();
		backgroundViewport.AddChild(titleBackdrop);
	}

	private void StartMenuMusic()
	{
		GameAudio.EnsureMenuMusic(this);
	}

	private void ResizeBackgroundViewport()
	{
		if (backgroundViewport == null)
			return;

		if (backgroundViewport.GetParent() is SubViewportContainer { Stretch: true })
			return;

		Vector2 size = GetViewportRect().Size;
		backgroundViewport.Size = new Vector2I(
			Mathf.Max(1, Mathf.CeilToInt(size.X)),
			Mathf.Max(1, Mathf.CeilToInt(size.Y))
		);
	}

	private void SetupGlassPanel()
	{
		PanelContainer panel = GetNodeOrNull<PanelContainer>(PanelLeftPath);
		VBoxContainer menuBox = GetNodeOrNull<VBoxContainer>(MenuBoxPath);

		if (panel == null)
		{
			GD.PushError($"MainMenu is missing PanelContainer at '{PanelLeftPath}'.");
			return;
		}

		panel.CustomMinimumSize = new Vector2(316, 0);
		panel.OffsetTop = -292f;
		panel.OffsetBottom = 292f;
		panel.AddThemeStyleboxOverride("panel", CreatePanelStyle());

		if (menuBox != null)
			menuBox.AddThemeConstantOverride("separation", 12);
	}

	private void SetupDifficultySelector()
	{
		VBoxContainer menuBox = GetNodeOrNull<VBoxContainer>(MenuBoxPath);
		if (menuBox == null || menuBox.GetNodeOrNull("DifficultySelector") != null)
			return;

		VBoxContainer selector = new VBoxContainer();
		selector.Name = "DifficultySelector";
		selector.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		selector.AddThemeConstantOverride("separation", 5);

		Label label = new Label();
		label.Text = "Schwierigkeit";
		label.HorizontalAlignment = HorizontalAlignment.Center;
		GameUi.ApplyLabel(label, 13, new Color(0.78f, 0.96f, 1f, 0.9f));
		selector.AddChild(label);

		HBoxContainer row = new HBoxContainer();
		row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		row.AddThemeConstantOverride("separation", 6);
		selector.AddChild(row);

		difficultyButtonGroup = new ButtonGroup();
		row.AddChild(CreateDifficultyButton("Leicht", GameDifficulty.Easy));
		row.AddChild(CreateDifficultyButton("Mittel", GameDifficulty.Medium));
		row.AddChild(CreateDifficultyButton("Hart", GameDifficulty.Hard));

		menuBox.AddChild(selector);
		Button settingsButton = GetNodeOrNull<Button>(SettingsButtonPath);
		if (settingsButton != null)
			menuBox.MoveChild(selector, settingsButton.GetIndex());

		RefreshDifficultyButtons();
	}

	private Button CreateDifficultyButton(string text, GameDifficulty difficulty)
	{
		Button button = new Button();
		button.Text = text;
		button.ToggleMode = true;
		button.ButtonGroup = difficultyButtonGroup;
		button.CustomMinimumSize = new Vector2(74f, 34f);
		button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		button.FocusMode = FocusModeEnum.All;
		GameUi.ApplyButton(button, 13);
		button.Pressed += () =>
		{
			GameDifficultySettings.Current = difficulty;
			GameAudio.PlayOneShot(this, GameAudio.UiButtonPath, -9f);
			RefreshDifficultyButtons();
		};
		button.MouseEntered += () => AnimateButton(button, true);
		button.MouseExited += () => AnimateButton(button, false);
		button.Scale = Vector2.One;
		button.Modulate = new Color(1f, 1f, 1f, 0.97f);
		difficultyButtons[difficulty] = button;
		menuButtons.Add(button);
		return button;
	}

	private void RefreshDifficultyButtons()
	{
		foreach (var pair in difficultyButtons)
			pair.Value.ButtonPressed = pair.Key == GameDifficultySettings.Current;
	}

	private void SetupGameLogo()
	{
		CanvasLayer ui = GetNodeOrNull<CanvasLayer>("UI");
		if (ui == null)
			return;

		gameLogo = GetNodeOrNull<TextureRect>(MenuLogoPath);

		if (gameLogo == null)
		{
			gameLogo = new TextureRect();
			gameLogo.Name = "GameLogo";
			ui.AddChild(gameLogo);
		}

		gameLogo.Texture = ResourceLoader.Load<Texture2D>(GameLogoTexturePath);
		gameLogo.MouseFilter = MouseFilterEnum.Ignore;
		gameLogo.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
		gameLogo.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		gameLogo.Modulate = new Color(1f, 1f, 1f, 0.96f);
		gameLogo.SetAnchorsPreset(LayoutPreset.TopLeft);
		ResizeGameLogo();
	}

	private void ResizeGameLogo()
	{
		if (gameLogo == null)
			return;

		Vector2 viewport = GetViewportRect().Size;
		if (viewport.X <= 1f || viewport.Y <= 1f)
			viewport = new Vector2(1280f, 720f);

		float rightMargin = Mathf.Clamp(viewport.X * 0.035f, 30f, 54f);
		float leftSafe = Mathf.Clamp(viewport.X * 0.34f, 390f, 470f);
		float availableWidth = Mathf.Max(280f, viewport.X - leftSafe - rightMargin);
		float width = Mathf.Min(viewport.X * 0.58f, availableWidth);
		width = Mathf.Clamp(width, 330f, 760f);

		float aspect = 16f / 9f;
		if (gameLogo.Texture != null && gameLogo.Texture.GetHeight() > 0)
			aspect = gameLogo.Texture.GetWidth() / (float)gameLogo.Texture.GetHeight();

		float height = width / Mathf.Max(aspect, 0.1f);
		float maxHeight = viewport.Y * 0.66f;

		if (height > maxHeight)
		{
			height = maxHeight;
			width = height * aspect;
		}

		float x = viewport.X - rightMargin - width;
		float y = viewport.Y * 0.5f - height * 0.5f;
		y = Mathf.Clamp(y, 24f, Mathf.Max(24f, viewport.Y - height - 24f));

		gameLogo.Position = new Vector2(x, y);
		gameLogo.Size = new Vector2(width, height);
		gameLogo.CustomMinimumSize = gameLogo.Size;
	}

	private void CreateRightIconButtons()
	{
		CanvasLayer ui = GetNodeOrNull<CanvasLayer>("UI");
		if (ui == null || ui.GetNodeOrNull("RightIconButtons") != null)
			return;

		VBoxContainer bar = new VBoxContainer();
		bar.Name = "RightIconButtons";
		bar.AnchorLeft = 1f;
		bar.AnchorTop = 0f;
		bar.AnchorRight = 1f;
		bar.AnchorBottom = 0f;
		bar.OffsetLeft = -104f;
		bar.OffsetTop = 30f;
		bar.OffsetRight = -28f;
		bar.OffsetBottom = 210f;
		bar.Alignment = BoxContainer.AlignmentMode.Begin;
		bar.AddThemeConstantOverride("separation", 12);
		ui.AddChild(bar);

		shopIconButton = CreateIconMenuButton("Shop", ShopIconTexturePath, OpenShop);
		missionsIconButton = CreateIconMenuButton("Missionen", MissionsIconTexturePath, OpenMissions);
		bar.AddChild(shopIconButton);
		bar.AddChild(missionsIconButton);
	}

	private void ConfigureMainMenuFocusNavigation()
	{
		GameUi.ConfigureButtonNavigation(this);

		if (shopIconButton == null || missionsIconButton == null)
			return;

		List<Button> leftButtons = GetLeftMenuButtons();
		if (leftButtons.Count == 0)
			return;

		for (int i = 0; i < leftButtons.Count; i++)
		{
			Button current = leftButtons[i];
			Button previous = leftButtons[Mathf.PosMod(i - 1, leftButtons.Count)];
			Button next = leftButtons[Mathf.PosMod(i + 1, leftButtons.Count)];

			SetFocusNeighbor(current, Side.Top, previous);
			SetFocusNeighbor(current, Side.Bottom, next);
			SetFocusNeighbor(current, Side.Left, shopIconButton);
			SetFocusNeighbor(current, Side.Right, shopIconButton);
		}

		SetFocusNeighbor(shopIconButton, Side.Top, missionsIconButton);
		SetFocusNeighbor(shopIconButton, Side.Bottom, missionsIconButton);
		SetFocusNeighbor(shopIconButton, Side.Left, leftButtons[0]);
		SetFocusNeighbor(shopIconButton, Side.Right, leftButtons[0]);

		SetFocusNeighbor(missionsIconButton, Side.Top, shopIconButton);
		SetFocusNeighbor(missionsIconButton, Side.Bottom, shopIconButton);
		SetFocusNeighbor(missionsIconButton, Side.Left, leftButtons[0]);
		SetFocusNeighbor(missionsIconButton, Side.Right, leftButtons[0]);
	}

	private List<Button> GetLeftMenuButtons()
	{
		List<Button> buttons = new List<Button>();

		AddButtonIfValid(buttons, GetNodeOrNull<Button>(ClassicButtonPath));
		AddButtonIfValid(buttons, GetNodeOrNull<Button>(MultiplayerButtonPath));
		AddButtonIfValid(buttons, GetNodeOrNull<Button>(LeaderboardButtonPath));
		AddButtonIfValid(buttons, GetNodeOrNull<Button>(TutorialButtonPath));
		AddButtonIfValid(buttons, difficultyButtons.GetValueOrDefault(GameDifficulty.Easy));
		AddButtonIfValid(buttons, difficultyButtons.GetValueOrDefault(GameDifficulty.Medium));
		AddButtonIfValid(buttons, difficultyButtons.GetValueOrDefault(GameDifficulty.Hard));
		AddButtonIfValid(buttons, GetNodeOrNull<Button>(SettingsButtonPath));
		AddButtonIfValid(buttons, GetNodeOrNull<Button>(CreditsButtonPath));
		AddButtonIfValid(buttons, GetNodeOrNull<Button>(QuitButtonPath));

		return buttons;
	}

	private void AddButtonIfValid(List<Button> buttons, Button button)
	{
		if (button != null && button.IsVisibleInTree() && !button.Disabled)
			buttons.Add(button);
	}

	private void SetFocusNeighbor(Button source, Side side, Button target)
	{
		if (source == null || target == null)
			return;

		NodePath path = source.GetPathTo(target);
		switch (side)
		{
			case Side.Left:
				source.FocusNeighborLeft = path;
				break;
			case Side.Right:
				source.FocusNeighborRight = path;
				break;
			case Side.Top:
				source.FocusNeighborTop = path;
				break;
			case Side.Bottom:
				source.FocusNeighborBottom = path;
				break;
		}
	}

	private Button CreateIconMenuButton(string tooltip, string iconPath, Action handler)
	{
		Button button = new Button();
		button.Text = "";
		button.TooltipText = tooltip;
		button.Icon = ResourceLoader.Load<Texture2D>(iconPath);
		button.ExpandIcon = true;
		button.IconAlignment = HorizontalAlignment.Center;
		button.Alignment = HorizontalAlignment.Center;
		button.CustomMinimumSize = new Vector2(76f, 76f);
		button.FocusMode = FocusModeEnum.All;
		ApplyIconButtonStyle(button);
		button.Pressed += () => GameAudio.PlayOneShot(
			this,
			GameAudio.UiButtonPath,
			-8f,
			(float)GD.RandRange(0.96f, 1.04f)
		);
		button.Pressed += handler;
		button.MouseEntered += () => AnimateButton(button, true);
		button.MouseExited += () => AnimateButton(button, false);
		button.Scale = Vector2.One;
		button.Modulate = new Color(1f, 1f, 1f, 0.98f);
		menuButtons.Add(button);
		return button;
	}

	private void ApplyIconButtonStyle(Button button)
	{
		button.AddThemeStyleboxOverride("normal", CreateIconButtonStyle(0f, 0f));
		button.AddThemeStyleboxOverride("hover", CreateIconButtonStyle(0.08f, 0.35f));
		button.AddThemeStyleboxOverride("pressed", CreateIconButtonStyle(0.14f, 0.55f));
		button.AddThemeStyleboxOverride("focus", CreateIconButtonStyle(0.2f, 0.8f));
		button.AddThemeConstantOverride("h_separation", 0);
	}

	private StyleBoxFlat CreateIconButtonStyle(float backgroundAlpha, float borderAlpha)
	{
		StyleBoxFlat style = new StyleBoxFlat();
		style.BgColor = new Color(1f, 1f, 1f, backgroundAlpha);
		style.BorderColor = new Color(0.82f, 0.96f, 1f, borderAlpha);
		style.BorderWidthLeft = 2;
		style.BorderWidthTop = 2;
		style.BorderWidthRight = 2;
		style.BorderWidthBottom = 2;
		style.CornerRadiusTopLeft = 8;
		style.CornerRadiusTopRight = 8;
		style.CornerRadiusBottomLeft = 8;
		style.CornerRadiusBottomRight = 8;
		style.ContentMarginLeft = 2;
		style.ContentMarginTop = 2;
		style.ContentMarginRight = 2;
		style.ContentMarginBottom = 2;
		return style;
	}

	private void CreateControllerNotice()
	{
		CanvasLayer ui = GetNodeOrNull<CanvasLayer>("UI");
		if (ui == null)
			return;

		controllerNoticePanel = new Panel();
		controllerNoticePanel.Visible = false;
		controllerNoticePanel.MouseFilter = MouseFilterEnum.Ignore;
		controllerNoticePanel.AddThemeStyleboxOverride("panel", CreateControllerNoticeStyle());
		ui.AddChild(controllerNoticePanel);

		HBoxContainer row = new HBoxContainer();
		row.AnchorRight = 1f;
		row.AnchorBottom = 1f;
		row.OffsetLeft = 14f;
		row.OffsetTop = 10f;
		row.OffsetRight = -14f;
		row.OffsetBottom = -10f;
		row.Alignment = BoxContainer.AlignmentMode.Center;
		row.AddThemeConstantOverride("separation", 12);
		controllerNoticePanel.AddChild(row);

		controllerNoticeImage = new TextureRect();
		controllerNoticeImage.CustomMinimumSize = new Vector2(104f, 70f);
		controllerNoticeImage.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		controllerNoticeImage.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		row.AddChild(controllerNoticeImage);

		VBoxContainer copy = new VBoxContainer();
		copy.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		copy.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		copy.AddThemeConstantOverride("separation", 3);
		row.AddChild(copy);

		Label title = new Label();
		title.Text = "Controller erkannt";
		title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		title.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		title.ClipText = true;
		GameUi.ApplyLabel(title, 17, GameUi.LightText);
		copy.AddChild(title);
		controllerNoticeTitle = title;

		controllerNoticeName = new Label();
		controllerNoticeName.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		controllerNoticeName.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		GameUi.ApplyLabel(controllerNoticeName, 14, new Color(0.76f, 0.96f, 1f));
		copy.AddChild(controllerNoticeName);

		PositionControllerNotice();
	}

	private void CreateControllerHints()
	{
		CanvasLayer ui = GetNodeOrNull<CanvasLayer>("UI");
		if (ui == null)
			return;

		controllerHintBar = GameUi.CreateControllerHintBar();
		GameUi.PlaceControllerHintOverlay(controllerHintBar);
		ui.AddChild(controllerHintBar);
	}

	private void PositionControllerNotice()
	{
		if (controllerNoticePanel == null)
			return;

		Vector2 viewport = GetViewportRect().Size;
		float margin = 24f;
		float availableWidth = Mathf.Max(260f, viewport.X - margin * 2f);
		float width = Mathf.Clamp(availableWidth, 340f, 480f);
		float height = 108f;

		controllerNoticePanel.AnchorLeft = 1f;
		controllerNoticePanel.AnchorTop = 1f;
		controllerNoticePanel.AnchorRight = 1f;
		controllerNoticePanel.AnchorBottom = 1f;
		controllerNoticePanel.OffsetLeft = -width - margin;
		controllerNoticePanel.OffsetTop = -height - margin;
		controllerNoticePanel.OffsetRight = -margin;
		controllerNoticePanel.OffsetBottom = -margin;
	}

	private StyleBoxFlat CreateControllerNoticeStyle()
	{
		StyleBoxFlat style = new StyleBoxFlat();
		style.BgColor = new Color(0.02f, 0.12f, 0.17f, 0.92f);
		style.BorderColor = new Color(0.7f, 0.96f, 1f, 0.82f);
		style.BorderWidthLeft = 2;
		style.BorderWidthTop = 2;
		style.BorderWidthRight = 2;
		style.BorderWidthBottom = 2;
		style.CornerRadiusTopLeft = 4;
		style.CornerRadiusTopRight = 4;
		style.CornerRadiusBottomLeft = 4;
		style.CornerRadiusBottomRight = 4;
		style.ShadowColor = new Color(0f, 0.06f, 0.09f, 0.45f);
		style.ShadowSize = 10;
		return style;
	}

	private void SyncConnectedJoypads()
	{
		HashSet<int> connected = new HashSet<int>();

		foreach (int device in Input.GetConnectedJoypads())
		{
			connected.Add(device);

			if (knownJoypads.ContainsKey(device))
				continue;

			string joyName = Input.GetJoyName(device) ?? "";
			knownJoypads[device] = joyName;
			GameUi.FocusFirstButton(this);
			CallDeferred(nameof(ConfigureMainMenuFocusNavigation));
			ShowControllerNotice(joyName, true);
		}

		List<int> removedDevices = new List<int>();

		foreach (int device in knownJoypads.Keys)
		{
			if (!connected.Contains(device))
				removedDevices.Add(device);
		}

		foreach (int device in removedDevices)
		{
			string joyName = knownJoypads[device];
			knownJoypads.Remove(device);
			ShowControllerNotice(joyName, false);
		}
	}

	private void ShowControllerNotice(string joyName, bool connected)
	{
		if (controllerNoticePanel == null)
			return;

		if (connected && !GameUi.TryClaimControllerNotice())
			return;

		if (!connected)
			GameUi.ResetControllerNotice();

		GameUi.ControllerInfo info = GameUi.GetControllerInfo(joyName);

		controllerNoticeTitle.Text = connected
			? "Controller erkannt"
			: "Controller getrennt";
		controllerNoticeImage.Texture = ResourceLoader.Load<Texture2D>(info.TexturePath);
		controllerNoticeName.Text = string.IsNullOrWhiteSpace(joyName)
			? connected ? info.DisplayName : $"{info.DisplayName} nicht mehr erkannt"
			: connected ? $"{info.DisplayName}  ({joyName})" : $"{info.DisplayName} getrennt  ({joyName})";
		controllerNoticePanel.Modulate = Colors.White;
		controllerNoticePanel.Show();
		controllerNoticeTimer = ControllerNoticeDuration;

		if (connected)
			GameUi.RumbleConnectedJoypads(0.12f, 0.36f, 0.14f);
	}

	private void UpdateControllerNotice(float dt)
	{
		if (controllerNoticePanel == null || !controllerNoticePanel.Visible)
			return;

		controllerNoticeTimer -= dt;

		if (controllerNoticeTimer <= 0f)
		{
			controllerNoticePanel.Hide();
			return;
		}

		float alpha = Mathf.Clamp(controllerNoticeTimer, 0f, 1f);
		controllerNoticePanel.Modulate = new Color(1f, 1f, 1f, alpha);
	}

	private void ConnectButton(string path, Action handler)
	{
		Button button = GetNodeOrNull<Button>(path);

		if (button == null)
		{
			GD.PushError($"MainMenu is missing Button at '{path}'.");
			return;
		}

		button.Pressed += () => GameAudio.PlayOneShot(
			this,
			GameAudio.UiButtonPath,
			-8f,
			(float)GD.RandRange(0.96f, 1.04f)
		);
		button.Pressed += handler;
		button.MouseEntered += () => AnimateButton(button, true);
		button.MouseExited += () => AnimateButton(button, false);
		button.Scale = Vector2.One;
		button.Modulate = new Color(1f, 1f, 1f, 0.97f);
		button.CustomMinimumSize = new Vector2(240, 46);
		button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		button.FocusMode = FocusModeEnum.All;
		ApplyButtonStyle(button);
		menuButtons.Add(button);
	}

	private void AnimateButton(Button button, bool hovered)
	{
		if (button == null)
			return;

		button.PivotOffset = button.Size * 0.5f;

		if (hoverTweens.TryGetValue(button, out Tween existingTween))
			existingTween.Kill();

		Tween tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Cubic);
		tween.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(
			button,
			"scale",
			hovered ? new Vector2(HoverScale, HoverScale) : Vector2.One,
			HoverTweenDuration
		);
		tween.Parallel().TweenProperty(
			button,
			"modulate",
			hovered ? Colors.White : new Color(1f, 1f, 1f, 0.97f),
			HoverTweenDuration
		);

		hoverTweens[button] = tween;
	}

	private void RefreshButtonPivots()
	{
		foreach (Button button in menuButtons)
			button.PivotOffset = button.Size * 0.5f;
	}

	private void ChangeScene(string scenePath)
	{
		if (!ResourceLoader.Exists(scenePath))
		{
			GD.PushWarning($"Scene does not exist: {scenePath}");
			return;
		}

		SceneTransition.FadeToScene(GetTree(), scenePath, 0.32f);
	}

	private StyleBoxFlat CreatePanelStyle()
	{
		return GameUi.CreatePanelStyle();
	}

	private void ApplyButtonStyle(Button button)
	{
		GameUi.ApplyButton(button);
	}

	private StyleBoxFlat CreateButtonStyle(Color background, Color border)
	{
		return GameUi.CreateButtonStyle(background, border);
	}
}
