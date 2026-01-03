using Godot;

public partial class Impact : Node2D
{
	public override void _Ready()
	{
		// Procura todos AnimatedSprite2D filhos e toca
		foreach (var child in GetChildren())
		{
			if (child is AnimatedSprite2D s)
			{
				s.Play();
				s.AnimationFinished += () => { if (IsInstanceValid(this)) QueueFree(); };
				return; // já garante queue_free no primeiro que terminar
			}
		}

		// fallback: se não achar sprite, se destrói
		QueueFree();
	}
}
