using Godot;

public partial class ShopPage : Control
{
	private Label coinLabel;
	private Label statusLabel;
	private ScrollContainer skinScroll;
	private GridContainer skinGrid;
	private VBoxContainer itemList;
	private ScoreManager scoreManager;

	public override void _Ready()
	{
		GameAudio.EnsureMenuMusic(this);
		scoreManager = GetNode<ScoreManager>("/root/ScoreManager");
		BuildUi();
		RefreshShop();
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
		overlay.Color = new Color(0.01f, 0.05f, 0.08f, 0.28f);
		overlay.MouseFilter = MouseFilterEnum.Ignore;
		overlay.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(overlay);

		Panel panel = new Panel();
		panel.AnchorLeft = 0.5f;
		panel.AnchorTop = 0.5f;
		panel.AnchorRight = 0.5f;
		panel.AnchorBottom = 0.5f;
		panel.OffsetLeft = -520f;
		panel.OffsetTop = -322f;
		panel.OffsetRight = 520f;
		panel.OffsetBottom = 322f;
		panel.AddThemeStyleboxOverride("panel", GameUi.CreatePanelStyle());
		AddChild(panel);

		VBoxContainer layout = new VBoxContainer();
		layout.SetAnchorsPreset(LayoutPreset.FullRect);
		layout.OffsetLeft = 26f;
		layout.OffsetTop = 22f;
		layout.OffsetRight = -26f;
		layout.OffsetBottom = -22f;
		layout.AddThemeConstantOverride("separation", 10);
		panel.AddChild(layout);

		HBoxContainer header = new HBoxContainer();
		header.AddThemeConstantOverride("separation", 14);
		layout.AddChild(header);

		Label title = CreateLabel("Shop", 30, GameUi.LightText);
		title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		header.AddChild(title);

		coinLabel = CreateLabel("", 22, new Color(1f, 0.9f, 0.34f));
		coinLabel.HorizontalAlignment = HorizontalAlignment.Right;
		header.AddChild(coinLabel);

		HBoxContainer columns = new HBoxContainer();
		columns.SizeFlagsVertical = SizeFlags.ExpandFill;
		columns.AddThemeConstantOverride("separation", 16);
		layout.AddChild(columns);

		VBoxContainer skinsColumn = new VBoxContainer();
		skinsColumn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		skinsColumn.SizeFlagsVertical = SizeFlags.ExpandFill;
		skinsColumn.AddThemeConstantOverride("separation", 8);
		columns.AddChild(skinsColumn);

		Label skinsTitle = CreateLabel("Skins", 20, GameUi.AccentText);
		skinsColumn.AddChild(skinsTitle);

		skinScroll = new ScrollContainer();
		skinScroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		skinScroll.SizeFlagsVertical = SizeFlags.ExpandFill;
		skinScroll.CustomMinimumSize = new Vector2(640f, 420f);
		skinScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
		skinScroll.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
		skinScroll.FollowFocus = true;
		skinsColumn.AddChild(skinScroll);

		skinGrid = new GridContainer();
		skinGrid.Columns = 4;
		skinGrid.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		skinGrid.AddThemeConstantOverride("h_separation", 10);
		skinGrid.AddThemeConstantOverride("v_separation", 10);
		skinScroll.AddChild(skinGrid);

		VBoxContainer itemColumn = new VBoxContainer();
		itemColumn.CustomMinimumSize = new Vector2(296f, 0f);
		itemColumn.AddThemeConstantOverride("separation", 8);
		columns.AddChild(itemColumn);

		Label itemsTitle = CreateLabel("Start-Items", 20, GameUi.AccentText);
		itemColumn.AddChild(itemsTitle);

		itemList = new VBoxContainer();
		itemList.AddThemeConstantOverride("separation", 10);
		itemColumn.AddChild(itemList);

		statusLabel = CreateLabel("", 16, new Color(0.72f, 1f, 0.84f));
		statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
		layout.AddChild(statusLabel);

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

	private void RefreshShop()
	{
		coinLabel.Text = $"Münzen: {scoreManager.TotalCoins}";
		RebuildSkins();
		RebuildItems();
		CallDeferred(nameof(RefreshFocusNavigation));
	}

	private void RefreshFocusNavigation()
	{
		GameUi.ConfigureButtonNavigation(this);

		if (GetViewport().GuiGetFocusOwner() == null)
			GameUi.FocusFirstButton(this);
	}

	private void RebuildSkins()
	{
		ClearChildren(skinGrid);

		foreach (SkinDefinition skin in ShopCatalog.Skins)
			skinGrid.AddChild(CreateSkinCard(skin));
	}

	private Control CreateSkinCard(SkinDefinition skin)
	{
		PanelContainer card = new PanelContainer();
		card.CustomMinimumSize = new Vector2(160f, 198f);
		card.AddThemeStyleboxOverride("panel", CreateCardStyle());

		VBoxContainer layout = new VBoxContainer();
		layout.SetAnchorsPreset(LayoutPreset.FullRect);
		layout.AddThemeConstantOverride("separation", 5);
		card.AddChild(layout);

		layout.AddChild(CreateSkinPreview(skin.Frame1Path, new Vector2(140f, 94f)));

		Label name = CreateLabel(skin.DisplayName, 12, GameUi.LightText);
		name.HorizontalAlignment = HorizontalAlignment.Center;
		name.ClipText = true;
		layout.AddChild(name);

		Label pack = CreateLabel(skin.PackName, 11, new Color(0.72f, 0.9f, 0.96f));
		pack.HorizontalAlignment = HorizontalAlignment.Center;
		layout.AddChild(pack);

		Button action = CreateMenuButton(GetSkinButtonText(skin));
		action.CustomMinimumSize = new Vector2(0f, 34f);
		action.Disabled = scoreManager.SelectedSkinId == skin.Id;
		action.Pressed += () => HandleSkinPressed(skin);
		action.FocusEntered += () => EnsureSkinCardVisible(card);
		layout.AddChild(action);

		return card;
	}

	private TextureRect CreateSkinPreview(string texturePath, Vector2 size)
	{
		TextureRect preview = new TextureRect();
		preview.Texture = ResourceLoader.Load<Texture2D>(texturePath);
		preview.CustomMinimumSize = size;
		preview.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		preview.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		preview.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		return preview;
	}

	private void EnsureSkinCardVisible(Control card)
	{
		if (skinScroll == null || card == null)
			return;

		CallDeferred(nameof(ScrollSkinCardIntoView), card);
	}

	private void ScrollSkinCardIntoView(Control card)
	{
		if (skinScroll == null || card == null || !IsInstanceValid(card))
			return;

		VScrollBar scrollBar = skinScroll.GetVScrollBar();
		if (scrollBar == null)
			return;

		float padding = 14f;
		float top = card.Position.Y - padding;
		float bottom = card.Position.Y + card.Size.Y + padding;
		float viewTop = (float)scrollBar.Value;
		float viewBottom = viewTop + skinScroll.Size.Y;

		if (top < viewTop)
			scrollBar.Value = Mathf.Max(scrollBar.MinValue, top);
		else if (bottom > viewBottom)
			scrollBar.Value = Mathf.Min(scrollBar.MaxValue, bottom - skinScroll.Size.Y);
	}

	private string GetSkinButtonText(SkinDefinition skin)
	{
		if (scoreManager.SelectedSkinId == skin.Id)
			return "Aktiv";

		if (scoreManager.IsSkinOwned(skin.Id))
			return "Anziehen";

		return $"{skin.Price} M";
	}

	private void HandleSkinPressed(SkinDefinition skin)
	{
		bool owned = scoreManager.IsSkinOwned(skin.Id);
		if (!owned)
		{
			if (!scoreManager.TryPurchaseSkin(skin.Id))
			{
				SetStatus("Nicht genug Münzen");
				return;
			}

			SetStatus($"{skin.DisplayName} gekauft");
		}

		scoreManager.SelectSkin(skin.Id);
		GameAudio.PlayOneShot(this, GameAudio.UiButtonPath, -8f);
		RefreshShop();
	}

	private void RebuildItems()
	{
		ClearChildren(itemList);

		foreach (StartItemDefinition item in ShopCatalog.StartItems)
			itemList.AddChild(CreateStartItemCard(item));
	}

	private Control CreateStartItemCard(StartItemDefinition item)
	{
		PanelContainer card = new PanelContainer();
		card.AddThemeStyleboxOverride("panel", CreateCardStyle());

		HBoxContainer row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 10);
		card.AddChild(row);

		TextureRect icon = new TextureRect();
		icon.Texture = ResourceLoader.Load<Texture2D>(item.IconPath);
		icon.CustomMinimumSize = new Vector2(54f, 54f);
		icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		row.AddChild(icon);

		VBoxContainer copy = new VBoxContainer();
		copy.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		copy.AddThemeConstantOverride("separation", 3);
		row.AddChild(copy);

		Label title = CreateLabel(item.DisplayName, 15, GameUi.LightText);
		copy.AddChild(title);

		Label description = CreateLabel(item.Description, 11, new Color(0.75f, 0.92f, 0.96f));
		description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		copy.AddChild(description);

		Label count = CreateLabel($"Besitz: {scoreManager.GetStartItemCount(item.Kind)}", 12, GameUi.AccentText);
		copy.AddChild(count);

		Button buy = CreateMenuButton($"{item.Price} M");
		buy.CustomMinimumSize = new Vector2(74f, 42f);
		buy.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
		buy.Pressed += () => HandleStartItemPressed(item);
		row.AddChild(buy);

		return card;
	}

	private void HandleStartItemPressed(StartItemDefinition item)
	{
		if (!scoreManager.TryPurchaseStartItem(item.Kind))
		{
			SetStatus("Nicht genug Münzen");
			return;
		}

		SetStatus($"{item.DisplayName} gekauft");
		GameAudio.PlayOneShot(this, GameAudio.UiButtonPath, -8f);
		RefreshShop();
	}

	private void SetStatus(string text)
	{
		if (statusLabel != null)
			statusLabel.Text = text;
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
		button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		GameUi.ApplyButton(button, 13);
		return button;
	}

	private StyleBoxFlat CreateCardStyle()
	{
		StyleBoxFlat style = GameUi.CreateButtonStyle(
			new Color(0.02f, 0.14f, 0.2f, 0.68f),
			new Color(0.72f, 0.96f, 1f, 0.46f)
		);
		style.ContentMarginLeft = 10;
		style.ContentMarginTop = 8;
		style.ContentMarginRight = 10;
		style.ContentMarginBottom = 8;
		return style;
	}

	private void ClearChildren(Node parent)
	{
		foreach (Node child in parent.GetChildren())
		{
			parent.RemoveChild(child);
			child.QueueFree();
		}
	}

	private void GoBack()
	{
		SceneTransition.FadeToScene(GetTree(), "res://Scenes/MainMenu.tscn", 0.28f);
	}
}
