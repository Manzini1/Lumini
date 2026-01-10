using Godot;
using System;

public partial class OneShotAnimatedVfx : Node2D
{
	[Export] public SpriteFrames Frames;
	[Export] public string AnimName = "play";
	[Export] public float SpeedScale = 1.0f;
	[Export] public bool AutoFree = true;

	private AnimatedSprite2D _sprite;

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (_sprite == null)
		{
			GD.PushError("[OneShotAnimatedVfx] AnimatedSprite2D não encontrado.");
			QueueFree();
			return;
		}

		if (Frames != null)
			_sprite.SpriteFrames = Frames;

		_sprite.SpeedScale = SpeedScale;

		if (_sprite.SpriteFrames != null && _sprite.SpriteFrames.HasAnimation(AnimName))
			_sprite.Play(AnimName);
		else
			_sprite.Play();

		_sprite.AnimationFinished += OnFinished;
	}

	private void OnFinished()
	{
		_sprite.AnimationFinished -= OnFinished;
		if (AutoFree) QueueFree();
	}
}
