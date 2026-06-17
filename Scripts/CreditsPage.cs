using Godot;

public partial class CreditsPage : Control
{
	public override void _Ready()
	{
		OceanMapBackground mapBackground = new OceanMapBackground();
		mapBackground.ConfigureForScreen();
		AddChild(mapBackground);
		MoveChild(mapBackground, 0);

		GameAudio.EnsureMenuMusic(this);

		BuildCredits();
		AddBackButton();
		AddControllerHints();
		GameUi.FocusFirstButton(this);
		SceneTransition.FadeIn(GetTree(), 0.24f);
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (!GameUi.IsCancelPressed(inputEvent))
			return;

		GetViewport().SetInputAsHandled();
		SceneTransition.FadeToScene(GetTree(), "res://Scenes/MainMenu.tscn", 0.28f);
	}

	private void BuildCredits()
	{
		PanelContainer panel = new PanelContainer();
		panel.SetAnchorsPreset(LayoutPreset.FullRect);
		panel.OffsetLeft = 88f;
		panel.OffsetTop = 42f;
		panel.OffsetRight = -88f;
		panel.OffsetBottom = -118f;
		panel.AddThemeStyleboxOverride("panel", GameUi.CreatePanelStyle());
		AddChild(panel);

		ScrollContainer scroll = new ScrollContainer();
		scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
		panel.AddChild(scroll);

		VBoxContainer content = new VBoxContainer();
		content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		content.AddThemeConstantOverride("separation", 12);
		scroll.AddChild(content);

		AddLabel(content, "Credits", 40, GameUi.LightText);
		AddSpacer(content, 8f);
		AddLabel(content, "Hauptprogrammierer", 30, new Color(0.78f, 1f, 0.95f));
		AddLabel(content, "Head of Production, Ideas and Power - The Real mathifummel", 36, new Color(1f, 0.94f, 0.55f));
		AddLabel(content, "Head of Textures and Influence - The Jacob", 25, GameUi.LightText);
		AddLabel(content, "Head of Controller Support and Kurdistan - Rodi", 25, GameUi.LightText);
		AddLabel(content, "Head of Sound, Innovation and 3. Normalform - Vladimir Misovic", 25, GameUi.LightText);

		AddSpacer(content, 10f);
		AddLabel(content, "Our Great Alliances", 23, new Color(0.82f, 0.97f, 1f));
		AddLabel(content, "Head of Helping - The Great Nick", 19, GameUi.LightText);
		AddLabel(content, "Head of Truck Driving - (Johannes) Theodor Fritzen", 19, GameUi.LightText);
		AddLabel(content, "Head of Tech Support - Laali", 19, GameUi.LightText);

		AddSpacer(content, 14f);
		AddLabel(content, "Sound Credits", 24, new Color(0.98f, 0.9f, 0.34f));
		AddLabel(content, "Animal Crossing New Horizons - Main Theme Song", 16, GameUi.LightText);
		AddLabel(content, "Samba de Amigo - Samba de Janeiro", 16, GameUi.LightText);
		AddLabel(content, "WarioWare D.I.Y. - D.I.Y. Shuffle ~ Speed Up!", 16, GameUi.LightText);
		AddLabel(content, "WarioWare, Inc. Mega Microgames! - Speed Up and Level Up", 16, GameUi.LightText);
		AddLabel(content, "WarioWare Twisted! - Speed Up, Level Up", 16, GameUi.LightText);
		AddLabel(content, "Scuba Diver Bubbles SOUND Effect - source for sliced bubble one-shots", 16, GameUi.LightText);
		AddLabel(content, "HD - SpongeBob Jellyfish Sound Effect", 16, GameUi.LightText);
		AddLabel(content, "Stresssoundeffekt - edited into short stress warning", 16, GameUi.LightText);
		AddLabel(content, "Minecraft Menu Button Sound Effect", 16, GameUi.LightText);
		AddLabel(content, "Friday Night Funkin - 3, 2, 1, GO! Sound Effect", 16, GameUi.LightText);
	}

	private void AddLabel(VBoxContainer content, string text, int size, Color color)
	{
		Label label = new Label();
		label.Text = text;
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		GameUi.ApplyLabel(label, size, color);
		content.AddChild(label);
	}

	private void AddSpacer(VBoxContainer content, float height)
	{
		Control spacer = new Control();
		spacer.CustomMinimumSize = new Vector2(1f, height);
		content.AddChild(spacer);
	}

	private void AddBackButton()
	{
		Button button = new Button();
		button.Text = "Zurück";
		button.CustomMinimumSize = new Vector2(170f, 42f);
		button.SetAnchorsPreset(LayoutPreset.CenterBottom);
		button.OffsetLeft = -85f;
		button.OffsetTop = -88f;
		button.OffsetRight = 85f;
		button.OffsetBottom = -46f;
		GameUi.ApplyButton(button);
		button.Pressed += () => GameAudio.PlayOneShot(this, GameAudio.UiButtonPath, -7f);
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
