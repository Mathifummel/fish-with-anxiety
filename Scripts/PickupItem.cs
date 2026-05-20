using Godot;

public partial class PickupItem : Area2D
{
	[Export] public ItemType Type = ItemType.Alcohol;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node body)
	{
		if (body is not PlayerFish)
			return;

		Main main = GetTree().GetFirstNodeInGroup("game_main") as Main;
		main?.ApplyItem(Type);
		QueueFree();
	}
}
