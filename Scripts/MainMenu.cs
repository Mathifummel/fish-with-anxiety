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

	private const string BackgroundViewportPath = "Background/SubViewport";
	private const string PanelLeftPath = "UI/PanelLeft";
	private const string MenuBoxPath = "UI/PanelLeft/VBoxContainer";
	private const string ClassicButtonPath = "UI/PanelLeft/VBoxContainer/Klassisch";
	private const string MultiplayerButtonPath = "UI/PanelLeft/VBoxContainer/Multiplayer";
	private const string LeaderboardButtonPath = "UI/PanelLeft/VBoxContainer/Leaderboard";
	private const string TutorialButtonPath = "UI/PanelLeft/VBoxContainer/Tutorial";
	private const string ShopButtonPath = "UI/PanelLeft/VBoxContainer/Shop";
	private const string SettingsButtonPath = "UI/PanelLeft/VBoxContainer/Einstellungen";
	private const string CreditsButtonPath = "UI/PanelLeft/VBoxContainer/Credits";
	private const string QuitButtonPath = "UI/PanelLeft/VBoxContainer/Beenden";
	private const string MenuLogoPath = "UI/GameLogo";
	private const string GameLogoTexturePath = "res://Assets/Transparaentlogo.png";

	private const float HoverScale = 1.05f;
	private const float HoverTweenDuration = 0.14f;
	private const float MultiplayerNoticeDuration = 1.3f;
	private const float ShopNoticeDuration = 1.15f;

	private readonly List<Button> menuButtons = new List<Button>();
	private readonly Dictionary<Button, Tween> hoverTweens = new Dictionary<Button, Tween>();

	private SubViewport backgroundViewport;
	private Button multiplayerButton;
	private Button shopButton;
	private TextureRect gameLogo;
	private float multiplayerNoticeTimer = 0f;
	private float shopNoticeTimer = 0f;

	public override void _Ready()
	{
		SetupBackgroundGameplay();
		SetupGlassPanel();
		SetupGameLogo();
		ConnectButton(ClassicButtonPath, StartClassic);
		ConnectButton(MultiplayerButtonPath, StartMultiplayer);
		ConnectButton(LeaderboardButtonPath, OpenLeaderboard);
		ConnectButton(TutorialButtonPath, OpenTutorial);
		ConnectButton(ShopButtonPath, OpenShop);
		ConnectButton(SettingsButtonPath, OpenSettings);
		ConnectButton(CreditsButtonPath, OpenCredits);
		ConnectButton(QuitButtonPath, QuitGame);

		multiplayerButton = GetNodeOrNull<Button>(MultiplayerButtonPath);
		shopButton = GetNodeOrNull<Button>(ShopButtonPath);
		if (shopButton != null)
		{
			Texture2D shopTexture = ResourceLoader.Load<Texture2D>("res://Assets/Shop.png");
			if (shopTexture != null)
			{
				shopButton.Icon = shopTexture;
				shopButton.ExpandIcon = true;
				shopButton.IconAlignment = HorizontalAlignment.Left;
			}
		}
		CallDeferred(nameof(RefreshButtonPivots));
		GameUi.FocusFirstButton(this);
		SceneTransition.FadeIn(GetTree(), 0.28f);
	}

	public override void _Process(double delta)
	{
		if (multiplayerNoticeTimer <= 0f || multiplayerButton == null)
		{
			UpdateShopNotice((float)delta);
			return;
		}

		multiplayerNoticeTimer -= (float)delta;

		if (multiplayerNoticeTimer <= 0f)
			multiplayerButton.Text = "2-Spieler Modus";

		UpdateShopNotice((float)delta);
	}

	public override void _Notification(int what)
	{
		if (what == NotificationResized)
		{
			ResizeBackgroundViewport();
			ResizeGameLogo();
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
		if (shopButton == null)
			return;

		shopButton.Text = "Shop kommt bald";
		shopNoticeTimer = ShopNoticeDuration;
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
		panel.AddThemeStyleboxOverride("panel", CreatePanelStyle());

		if (menuBox != null)
			menuBox.AddThemeConstantOverride("separation", 12);
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

	private void UpdateShopNotice(float dt)
	{
		if (shopNoticeTimer <= 0f || shopButton == null)
			return;

		shopNoticeTimer -= dt;

		if (shopNoticeTimer <= 0f)
			shopButton.Text = "Shop";
	}

	private void ConnectButton(string path, Action handler)
	{
		Button button = GetNodeOrNull<Button>(path);

		if (button == null)
		{
			GD.PushError($"MainMenu is missing Button at '{path}'.");
			return;
		}

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
