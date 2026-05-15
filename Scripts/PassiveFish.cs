using Godot;

public partial class PassiveFish : CharacterBody2D
{
	[Export] public float Speed = 80f;
	[Export] public float DirectionChangeTime = 3f;

	private Vector2 moveDir = Vector2.Right;
	private float timer = 0f;

	public override void _Ready()
	{
		RandomizeDirection();
	}

	public override void _PhysicsProcess(double delta)
	{
		timer -= (float)delta;

		if (timer <= 0f)
		{
			RandomizeDirection();
		}

		Velocity = moveDir * Speed;
		MoveAndSlide();

		if (Velocity.Length() > 0.1f)
		{
			Rotation = Velocity.Angle();
		}
	}

	private void RandomizeDirection()
	{
		moveDir = new Vector2(
			(float)GD.RandRange(-1, 1),
			(float)GD.RandRange(-1, 1)
		).Normalized();

		timer = DirectionChangeTime;
	}
}
