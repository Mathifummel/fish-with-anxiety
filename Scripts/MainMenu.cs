using Godot;
using System.Collections.Generic;

public partial class MainMenu : Control
{
	private Panel settingsPanel;
	private VBoxContainer customBindings;
	private Label captureLabel;
	private string pendingCustomAction = "";
	private VideoStreamPlayer backgroundVideo;
	private Node2D menuFishLayer;
	private Sprite2D menuPlayerFish;
	private Sprite2D[] menuNpcFish = new Sprite2D[4];
	private float visualTime = 0f;
	private FishSwimPath menuSwimPath = FishSwimPath.LeftToRight;
	private float menuPathProgress = 0f;
	private Vector2 menuLastLeaderPos = Vector2.Zero;
	private const float MenuSwimSpeed = 188f;
	private readonly Dictionary<string, Button> customButtons = new Dictionary<string, Button>();

	public override void _Ready()
	{
		GD.Print("Main Menu geladen");
		PlayerFish.LoadControlSettings();
		SetupMovingBackground();
		CreateMenuFishShowcase();
		CreateCoinCounter();
		CreateSettingsPanel();
		SceneTransition.FadeIn(GetTree(), 0.32f);
	}

	public override void _Process(double delta)
	{
		visualTime += (float)delta;
		AnimateBackground();
		AnimateMenuFish();
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
		canvas.AddChild(menuFishLayer);
		canvas.MoveChild(menuFishLayer, 2);

		menuPlayerFish = CreateMenuFish("res://Assets/MainCharacter.png", new Vector2(0.24f, 0.24f), 0);
		menuFishLayer.AddChild(menuPlayerFish);

		for (int i = 0; i < menuNpcFish.Length; i++)
		{
			menuNpcFish[i] = CreateMenuFish("res://Assets/EnemyCharacter.png", new Vector2(0.17f, 0.17f), -1);
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

		if (menuPathProgress >= 1f)
		{
			menuPathProgress = 0f;
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
		float leaderRotation = BackdropFishSwim.GetLeaderRotation(
			menuLastLeaderPos,
			leaderPos,
			tangent
		);
		menuLastLeaderPos = leaderPos;

		float panicPulse = 0.5f + Mathf.Sin(visualTime * 3.4f) * 0.5f;
		menuPlayerFish.Position = leaderPos;
		menuPlayerFish.Rotation = leaderRotation + Mathf.Sin(visualTime * 2.2f) * 0.05f;
		menuPlayerFish.Modulate = new Color(0.98f, 1f, 0.99f, 0.56f + panicPulse * 0.08f);

		BackdropFishSwim.PlaceFollowers(
			leaderPos,
			leaderRotation,
			tangent,
			menuNpcFish,
			visualTime * 1.75f,
			64f,
			34f,
			18f,
			12f
		);

		for (int i = 0; i < menuNpcFish.Length; i++)
			menuNpcFish[i].Modulate = new Color(1f, 1f, 1f, 0.5f - i * 0.04f);
	}

	private void CreateCoinCounter()
	{
		var sm = GetNode<ScoreManager>("/root/ScoreManager");

		Label coinLabel = new Label();
		coinLabel.Text = $"Muenzen: {sm.TotalCoins}";
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

	// START BUTTON
	private void _on_button_pressed()
	{
		SceneTransition.FadeToScene(GetTree(), "res://Scenes/main.tscn");
	}

	private void _on_settings_button_pressed()
	{
		settingsPanel.Visible = true;
	}

	private void SelectControlScheme(PlayerFish.ControlScheme scheme)
	{
		PlayerFish.SetControlScheme(scheme);
		customBindings.Visible = scheme == PlayerFish.ControlScheme.Custom;
		UpdateCustomButtonLabels();
	}

	private void CreateSettingsPanel()
	{
		settingsPanel = new Panel();
		settingsPanel.Visible = false;
		settingsPanel.AnchorLeft = 0.5f;
		settingsPanel.AnchorTop = 0.5f;
		settingsPanel.AnchorRight = 0.5f;
		settingsPanel.AnchorBottom = 0.5f;
		settingsPanel.OffsetLeft = -190;
		settingsPanel.OffsetTop = -230;
		settingsPanel.OffsetRight = 190;
		settingsPanel.OffsetBottom = 230;

		VBoxContainer layout = new VBoxContainer();
		layout.AnchorRight = 1f;
		layout.AnchorBottom = 1f;
		layout.OffsetLeft = 24;
		layout.OffsetTop = 24;
		layout.OffsetRight = -24;
		layout.OffsetBottom = -24;
		layout.AddThemeConstantOverride("separation", 10);
		settingsPanel.AddChild(layout);

		Label title = new Label();
		title.Text = "Steuerung";
		title.HorizontalAlignment = HorizontalAlignment.Center;
		title.AddThemeFontSizeOverride("font_size", 26);
		layout.AddChild(title);

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
		layout.AddChild(captureLabel);

		Button closeButton = new Button();
		closeButton.Text = "Zurueck";
		closeButton.Pressed += () =>
		{
			pendingCustomAction = "";
			captureLabel.Text = "";
			settingsPanel.Visible = false;
		};
		layout.AddChild(closeButton);

		GetNode<CanvasLayer>("CanvasLayer").AddChild(settingsPanel);
		UpdateCustomButtonLabels();
	}

	private void AddModeButton(VBoxContainer parent, string text, PlayerFish.ControlScheme scheme)
	{
		Button button = new Button();
		button.Text = text;
		button.CustomMinimumSize = new Vector2(0, 36);
		button.Pressed += () => SelectControlScheme(scheme);
		parent.AddChild(button);
	}

	private void AddCustomBindingButton(string label, string action)
	{
		Button button = new Button();
		button.CustomMinimumSize = new Vector2(0, 32);
		button.Pressed += () =>
		{
			pendingCustomAction = action;
			captureLabel.Text = $"{label}: Taste oder Mausklick druecken";
		};

		customButtons[action] = button;
		customBindings.AddChild(button);
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
	}

	// LEADERBOARD
	private void OnLeaderboardPressed()
	{
		SceneTransition.FadeToScene(GetTree(), "res://Scenes/leaderboard.tscn");
	}

	private void _on_credits_button_pressed()
	{
		GD.Print("Credits gedrueckt");
	}

	// QUIT BUTTON
	private void _on_exit_button_pressed()
	{
		GetTree().Quit();
	}
}
