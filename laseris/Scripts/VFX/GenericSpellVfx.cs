using Godot;

public partial class GenericSpellVfx : Node2D
{
	private AnimatedSprite2D _sprite;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		if (_sprite == null)
		{
			GD.PushError("[VFX] AnimatedSprite2D não encontrado.");
			QueueFree();
			return;
		}

		_sprite.Play();

		_sprite.AnimationFinished += () =>
		{
			QueueFree();
		};
	}
}
