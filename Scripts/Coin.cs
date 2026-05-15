// ===============================
// COIN.CS
// NEU ERSTELLEN
// ===============================

using Godot;

public partial class Coin : Area2D
{
	[Export] public int Value = 100;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node body)
	{
		if (body is PlayerFish)
		{
			var sm = GetNode<ScoreManager>(
				"/root/ScoreManager"
			);

			sm.BonusScore += Value;

			QueueFree();
		}
	}
}
