using Godot;
using System.Collections.Generic;

public partial class InfoPage : Control
{
	private const string MainMenuScenePath = "res://Scenes/MainMenu.tscn";

	private readonly InfoSection[] sections =
	{
		new InfoSection(
			"Items",
			new InfoEntry[]
			{
				new InfoEntry("Herz", "Gibt dir ein zusätzliches Leben, wenn du es als Start-Item nutzt.", "res://Assets/Herz.png"),
				new InfoEntry("Alkohol", "Macht dich kurz unverwundbar. Gut als Rettung, wenn viele Gegner nah sind.", "res://Assets/Alkohol.png"),
				new InfoEntry("Chorusfrucht", "Teleportiert dich aus einer gefährlichen Situation heraus.", "res://Assets/Chorusfrucht.png"),
				new InfoEntry("Müll", "Sammelt Stress und ist gefährlich. Besser ausweichen.", "res://Assets/Müll.png"),
				new InfoEntry("Münze", "Sammeln, im Shop ausgeben und damit Skins oder Start-Items kaufen.", "res://Assets/münze.png")
			}
		),
		new InfoSection(
			"Spielmodi",
			new InfoEntry[]
			{
				new InfoEntry("Klassisch", "Weiche Gegnerfischen, Müll und Quallen aus, sammle Münzen und halte so lange wie möglich durch.", "res://Assets/Fisch_1 1.png"),
				new InfoEntry("Tutorial", "Lernt Bewegung, Boost, Items und gefährliche Fische Schritt für Schritt.", "res://Assets/Pfeil.png"),
				new InfoEntry("2-Spieler Fangen", "Spieler 1 ist der kleine Fisch. Spieler 2 jagt als Gegnerfisch und nutzt den Biss-Boost mit P.", "res://Assets/Gegnerfischframe1.png"),
				new InfoEntry("Münzen sammeln", "Im Party-Modus sammelt ihr Münzen. Wer besser ausweicht und schneller sammelt, punktet.", "res://Assets/münze.png"),
				new InfoEntry("Räuber und Gendarm", "Kleine Fische fliehen, Gegnerfische jagen. Teamarbeit und gutes Timing entscheiden.", "res://Assets/Gegnerfisch2frame1.png")
			}
		),
		new InfoSection(
			"Mechaniken",
			new InfoEntry[]
			{
				new InfoEntry("Stress-Leiste", "Nähe zu Gegnern erhöht Stress. Im grünen Bereich ist der Boost am stärksten.", "res://Assets/exclamation_spritesheet_01.png"),
				new InfoEntry("Boost", "Der Spielerfisch sprintet kurz nach vorne. Perfektes Timing macht den Boost stärker.", "res://Assets/Pfeil.png"),
				new InfoEntry("Biss-Boost", "Der Gegnerfisch lädt seinen Biss auf. Wenn die Skala voll ist, mit P schütteln und nach vorne zubeißen.", "res://Assets/Gegnerfischframe2.png"),
				new InfoEntry("Warnzeichen", "Ein Ausrufezeichen zeigt gefährliche Fische an, bevor sie durch die Wasserwelt schießen.", "res://Assets/exclamation_spritesheet_01.png"),
				new InfoEntry("Shop & Missionen", "Missionen bringen Münzen. Im Shop kaufst du Skins und Start-Items für neue Runden.", "res://Assets/Generated/Buttons/shop_icon.png")
			}
		)
	};

	public override void _Ready()
	{
		GameAudio.EnsureMenuMusic(this);
		BuildUi();
		GameUi.FocusFirstButton(this);
		SceneTransition.FadeIn(GetTree(), 0.24f);
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (!GameUi.IsCancelPressed(inputEvent))
			return;

		GetViewport().SetInputAsHandled();
		GoBack();
	}

	private void BuildUi()
	{
		OceanMapBackground background = new OceanMapBackground();
		background.ConfigureForScreen();
		AddChild(background);
		MoveChild(background, 0);

		ColorRect overlay = new ColorRect();
		overlay.Color = new Color(0.01f, 0.05f, 0.08f, 0.34f);
		overlay.MouseFilter = MouseFilterEnum.Ignore;
		overlay.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(overlay);

		Panel panel = new Panel();
		panel.AnchorLeft = 0.5f;
		panel.AnchorTop = 0.5f;
		panel.AnchorRight = 0.5f;
		panel.AnchorBottom = 0.5f;
		panel.OffsetLeft = -540f;
		panel.OffsetTop = -328f;
		panel.OffsetRight = 540f;
		panel.OffsetBottom = 328f;
		panel.AddThemeStyleboxOverride("panel", CreatePanelStyle());
		AddChild(panel);

		VBoxContainer layout = new VBoxContainer();
		layout.SetAnchorsPreset(LayoutPreset.FullRect);
		layout.OffsetLeft = 28f;
		layout.OffsetTop = 22f;
		layout.OffsetRight = -28f;
		layout.OffsetBottom = -22f;
		layout.AddThemeConstantOverride("separation", 10);
		panel.AddChild(layout);

		HBoxContainer header = new HBoxContainer();
		header.AddThemeConstantOverride("separation", 14);
		layout.AddChild(header);

		Label title = CreateLabel("Info", 32, GameUi.LightText);
		title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		header.AddChild(title);

		Label subtitle = CreateLabel("Items, Modi und Mechaniken", 17, GameUi.AccentText);
		subtitle.HorizontalAlignment = HorizontalAlignment.Right;
		subtitle.VerticalAlignment = VerticalAlignment.Center;
		header.AddChild(subtitle);

		ScrollContainer scroll = new ScrollContainer();
		scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
		scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
		scroll.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
		layout.AddChild(scroll);

		VBoxContainer content = new VBoxContainer();
		content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		content.AddThemeConstantOverride("separation", 14);
		scroll.AddChild(content);

		foreach (InfoSection section in sections)
			content.AddChild(CreateSection(section));

		HBoxContainer footer = new HBoxContainer();
		footer.Alignment = BoxContainer.AlignmentMode.Center;
		layout.AddChild(footer);

		Button backButton = CreateMenuButton("Zurück");
		backButton.CustomMinimumSize = new Vector2(240f, 42f);
		backButton.Pressed += GoBack;
		footer.AddChild(backButton);

		ControllerHintBar controllerHints = GameUi.CreateControllerHintBar(GameUi.ControllerHintMode.BackOnly);
		controllerHints.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		layout.AddChild(controllerHints);
	}

	private Control CreateSection(InfoSection section)
	{
		VBoxContainer sectionBox = new VBoxContainer();
		sectionBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		sectionBox.AddThemeConstantOverride("separation", 8);

		Label heading = CreateLabel(section.Title, 22, GameUi.AccentText);
		sectionBox.AddChild(heading);

		GridContainer grid = new GridContainer();
		grid.Columns = 2;
		grid.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		grid.AddThemeConstantOverride("h_separation", 10);
		grid.AddThemeConstantOverride("v_separation", 10);
		sectionBox.AddChild(grid);

		foreach (InfoEntry entry in section.Entries)
			grid.AddChild(CreateInfoCard(entry));

		return sectionBox;
	}

	private Control CreateInfoCard(InfoEntry entry)
	{
		PanelContainer card = new PanelContainer();
		card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		card.CustomMinimumSize = new Vector2(0f, 106f);
		card.AddThemeStyleboxOverride("panel", CreateCardStyle());

		HBoxContainer row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 12);
		card.AddChild(row);

		Control iconBox = CreateIconBox(entry.TexturePath);
		row.AddChild(iconBox);

		VBoxContainer copy = new VBoxContainer();
		copy.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		copy.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		copy.AddThemeConstantOverride("separation", 4);
		row.AddChild(copy);

		Label title = CreateLabel(entry.Title, 16, GameUi.LightText);
		copy.AddChild(title);

		Label description = CreateLabel(entry.Description, 12, new Color(0.76f, 0.93f, 0.98f));
		description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		copy.AddChild(description);

		return card;
	}

	private Control CreateIconBox(string texturePath)
	{
		PanelContainer frame = new PanelContainer();
		frame.CustomMinimumSize = new Vector2(76f, 76f);
		frame.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		frame.AddThemeStyleboxOverride("panel", CreateIconFrameStyle());

		TextureRect icon = new TextureRect();
		icon.Texture = GetDisplayTexture(texturePath);
		icon.CustomMinimumSize = new Vector2(62f, 62f);
		icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		frame.AddChild(icon);
		return frame;
	}

	private Texture2D GetDisplayTexture(string texturePath)
	{
		return ResourceLoader.Load<Texture2D>(texturePath);
	}

	private Label CreateLabel(string text, int size, Color color)
	{
		Label label = new Label();
		label.Text = text;
		GameUi.ApplyLabel(label, size, color);
		return label;
	}

	private Button CreateMenuButton(string text)
	{
		Button button = new Button();
		button.Text = text;
		button.FocusMode = FocusModeEnum.All;
		GameUi.ApplyButton(button, 13);
		return button;
	}

	private StyleBoxFlat CreatePanelStyle()
	{
		StyleBoxFlat style = GameUi.CreatePanelStyle();
		style.BgColor = new Color(0.02f, 0.13f, 0.18f, 0.82f);
		style.BorderColor = new Color(0.78f, 0.98f, 1f, 0.78f);
		return style;
	}

	private StyleBoxFlat CreateCardStyle()
	{
		StyleBoxFlat style = GameUi.CreateButtonStyle(
			new Color(0.02f, 0.15f, 0.2f, 0.68f),
			new Color(0.72f, 0.96f, 1f, 0.34f)
		);
		style.ContentMarginLeft = 12;
		style.ContentMarginTop = 10;
		style.ContentMarginRight = 12;
		style.ContentMarginBottom = 10;
		return style;
	}

	private StyleBoxFlat CreateIconFrameStyle()
	{
		StyleBoxFlat style = GameUi.CreateButtonStyle(
			new Color(0.02f, 0.23f, 0.28f, 0.6f),
			new Color(1f, 0.94f, 0.62f, 0.48f)
		);
		style.ContentMarginLeft = 7;
		style.ContentMarginTop = 7;
		style.ContentMarginRight = 7;
		style.ContentMarginBottom = 7;
		return style;
	}

	private void GoBack()
	{
		SceneTransition.FadeToScene(GetTree(), MainMenuScenePath, 0.28f);
	}

	private readonly struct InfoSection
	{
		public readonly string Title;
		public readonly IReadOnlyList<InfoEntry> Entries;

		public InfoSection(string title, IReadOnlyList<InfoEntry> entries)
		{
			Title = title;
			Entries = entries;
		}
	}

	private readonly struct InfoEntry
	{
		public readonly string Title;
		public readonly string Description;
		public readonly string TexturePath;

		public InfoEntry(string title, string description, string texturePath)
		{
			Title = title;
			Description = description;
			TexturePath = texturePath;
		}
	}
}
