using Godot;

public partial class SimpleMenuPage : Control
{
	[Export] public string Title = "";

	public override void _Ready()
	{
		OceanMapBackground mapBackground = new OceanMapBackground();
		mapBackground.ConfigureForScreen();
		AddChild(mapBackground);
		MoveChild(mapBackground, 0);

		ColorRect background = GetNodeOrNull<ColorRect>("Background");
		if (background != null)
		{
			background.Color = new Color(0.01f, 0.06f, 0.09f, 0.18f);
			background.MouseFilter = MouseFilterEnum.Ignore;
		}

		PanelContainer panel = GetNodeOrNull<PanelContainer>("Panel");
		if (panel != null)
			panel.AddThemeStyleboxOverride("panel", GameUi.CreatePanelStyle());

		Label label = GetNodeOrNull<Label>("Panel/Label");
		if (label != null)
		{
			if (!string.IsNullOrWhiteSpace(Title))
				label.Text = Title;

			GameUi.ApplyLabel(label, 22, GameUi.DarkText);
			label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		}

		AddBackButton();
		AddControllerHints();
		GameUi.FocusFirstButton(this);
		SceneTransition.FadeIn(GetTree(), 0.24f);
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (GameUi.IsCancelPressed(inputEvent))
		{
			GetViewport().SetInputAsHandled();
			SceneTransition.FadeToScene(GetTree(), "res://Scenes/MainMenu.tscn", 0.28f);
		}
	}

	private void AddBackButton()
	{
		Button button = new Button();
		button.Text = "Zurück";
		button.CustomMinimumSize = new Vector2(170f, 42f);
		button.SetAnchorsPreset(LayoutPreset.CenterBottom);
		button.OffsetLeft = -85f;
		button.OffsetTop = -92f;
		button.OffsetRight = 85f;
		button.OffsetBottom = -50f;
		GameUi.ApplyButton(button);
		button.Pressed += () => SceneTransition.FadeToScene(GetTree(), "res://Scenes/MainMenu.tscn", 0.28f);
		AddChild(button);
	}

	private void AddControllerHints()
	{
		ControllerHintBar controllerHints = GameUi.CreateControllerHintBar(GameUi.ControllerHintMode.BackOnly);
		GameUi.PlaceControllerHintOverlay(controllerHints, 18f);
		AddChild(controllerHints);
	}
}
