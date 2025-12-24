using Godot;

public partial class OneShotVfx : Node2D
{
	[Export] public NodePath AnimPath = "Anim";
	[Export] public string AnimationName = "play";

	private AnimatedSprite2D _anim;

	public override void _Ready()
	{
		_anim = GetNodeOrNull<AnimatedSprite2D>(AnimPath);

		if (_anim == null)
		{
			GD.PushWarning($"{Name}: OneShotVfx não achou AnimatedSprite2D em '{AnimPath}'. Vou destruir.");
			QueueFree();
			return;
		}

		// garante que não fica preso na tela
		_anim.Visible = true;

		// toca e some quando terminar
		_anim.AnimationFinished += OnFinished;

		// se você tiver mais de uma animação, isso deixa configurável
		if (_anim.SpriteFrames != null && _anim.SpriteFrames.HasAnimation(AnimationName))
			_anim.Play(AnimationName);
		else
			_anim.Play(); // fallback
	}

	private void OnFinished()
	{
		QueueFree();
	}

	public override void _ExitTree()
	{
		if (_anim != null)
			_anim.AnimationFinished -= OnFinished;
	}
}
