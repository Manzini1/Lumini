using Godot;

public partial class SpellVfx : Node2D
{
	[Export] public NodePath AnimPath = "Anim";

	private AnimatedSprite2D _anim;

	public override void _Ready()
	{
		_anim = GetNodeOrNull<AnimatedSprite2D>(AnimPath);

		if (_anim == null)
		{
			GD.PushWarning($"{Name}: SpellVfx não achou AnimatedSprite2D em '{AnimPath}'.");
			return;
		}

		// garante que começa tocando
		if (!_anim.IsPlaying())
			_anim.Play();

		// quando terminar, some
		_anim.AnimationFinished += OnAnimFinished;
	}

	private void OnAnimFinished()
	{
		QueueFree();
	}
}
