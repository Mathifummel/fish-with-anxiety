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
	private const string SettingsButtonPath = "UI/PanelLeft/VBoxContainer/Einstellungen";
	private const string CreditsButtonPath = "UI/PanelLeft/VBoxContainer/Credits";
	private const string QuitButtonPath = "UI/PanelLeft/VBoxContainer/Beenden";

	private const float HoverScale = 1.05f;
	private const float HoverTweenDuration = 0.14f;
	private const float MultiplayerNoticeDuration = 1.3f;

	private readonly List<Button> menuButtons = new List<Button>();
	private readonly Dictionary<Button, Tween> hoverTweens = new Dictionary<Button, Tween>();

	private SubViewport backgroundViewport;
	private Button multiplayerButton;
	private float multiplayerNoticeTimer = 0f;

	public override void _Ready()
	{
		SetupBackgroundGameplay();
		SetupGlassPanel();
		ConnectButton(ClassicButtonPath, StartClassic);
		ConnectButton(MultiplayerButtonPath, StartMultiplayer);
		ConnectButton(LeaderboardButtonPath, OpenLeaderboard);
		ConnectButton(TutorialButtonPath, OpenTutorial);
		ConnectButton(SettingsButtonPath, OpenSettings);
		ConnectButton(CreditsButtonPath, OpenCredits);
		ConnectButton(QuitButtonPath, QuitGame);

		multiplayerButton = GetNodeOrNull<Button>(MultiplayerButtonPath);
		CallDeferred(nameof(RefreshButtonPivots));
		GameUi.FocusFirstButton(this);
		SceneTransition.FadeIn(GetTree(), 0.28f);
	}

	public override void _Process(double delta)
	{
		if (multiplayerNoticeTimer <= 0f || multiplayerButton == null)
			return;

		multiplayerNoticeTimer -= (float)delta;

		if (multiplayerNoticeTimer <= 0f)
			multiplayerButton.Text = "2-Spieler Modus";
	}

	public override void _Notification(int what)
	{
		if (what == NotificationResized)
			ResizeBackgroundViewport();
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

		panel.CustomMinimumSize = new Vector2(300, 0);
		panel.AddThemeStyleboxOverride("panel", CreatePanelStyle());

		if (menuBox != null)
			menuBox.AddThemeConstantOverride("separation", 12);
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
