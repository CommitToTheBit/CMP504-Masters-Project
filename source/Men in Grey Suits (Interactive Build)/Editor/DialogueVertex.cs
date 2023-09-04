using Godot;
using System;

public partial class DialogueVertex : Sprite2D
{
	CollisionShape2D collision;
	CircleShape2D circle;

	public override void _Ready()
	{
		collision = GetNode<CollisionShape2D>("Area2D/CollisionShape2D");
		circle = (CircleShape2D)collision.Shape;
		GD.Print(circle.Radius);

		QueueRedraw();

		Scale *= 5.0f;
	}

	public override void _Process(double delta)
	{
	}

	public override void _Draw()
	{
		Vector2 centre = collision.Position;
		float radius = circle.Radius;
		float border = 0.25f * radius;

		Color circleColour = new Color(1.0f, 1.0f, 1.0f);
		DrawCircle(centre, radius, circleColour);

		Color arcColour = new Color(0.0f, 0.0f, 0.0f);
		DrawArc(centre, radius, 0.0f, 360.0f, 32, arcColour, border);
	}
}
