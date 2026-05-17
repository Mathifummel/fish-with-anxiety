using Godot;
using System.Threading.Tasks;

public static class SceneTransition
{
	private static bool transitionActive = false;

	public static bool IsActive => transitionActive;

	public static async void FadeIn(SceneTree tree, float duration = 0.28f)
	{
		if (transitionActive || tree == null)
			return;

		ColorRect overlay = CreateOverlay(tree.Root, new Color(0f, 0.02f, 0.04f, 1f));
		Tween tween = tree.CreateTween();
		tween.TweenProperty(overlay, "color:a", 0f, duration)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);

		await overlay.ToSignal(tween, Tween.SignalName.Finished);
		overlay.GetParent().QueueFree();
	}

	public static async void FadeToScene(SceneTree tree, string scenePath, float duration = 0.34f)
	{
		if (transitionActive || tree == null)
			return;

		transitionActive = true;

		ColorRect overlay = CreateOverlay(tree.Root, new Color(0f, 0.02f, 0.04f, 0f));
		Tween fadeOut = tree.CreateTween();
		fadeOut.TweenProperty(overlay, "color:a", 1f, duration)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);

		await overlay.ToSignal(fadeOut, Tween.SignalName.Finished);
		tree.ChangeSceneToFile(scenePath);
		await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

		Tween fadeIn = tree.CreateTween();
		fadeIn.TweenProperty(overlay, "color:a", 0f, duration * 0.9f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);

		await overlay.ToSignal(fadeIn, Tween.SignalName.Finished);
		overlay.GetParent().QueueFree();
		transitionActive = false;
	}

	private static ColorRect CreateOverlay(Window root, Color color)
	{
		CanvasLayer transitionLayer = new CanvasLayer();
		transitionLayer.Name = "SceneTransitionLayer";
		transitionLayer.Layer = 4096;
		root.AddChild(transitionLayer);

		ColorRect overlay = new ColorRect();
		overlay.Name = "SceneFadeOverlay";
		overlay.Color = color;
		overlay.MouseFilter = Control.MouseFilterEnum.Stop;
		transitionLayer.AddChild(overlay);
		overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		return overlay;
	}
}
