using Godot;

public partial class ShieldAuraVfx : Node2D
{
	private AnimatedSprite2D _sprite;

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (_sprite == null)
		{
			GD.PushError("[ShieldAuraVfx] AnimatedSprite2D não encontrado.");
			QueueFree();
			return; // ✅ importante
		}
	}

	public void Configure(SpriteFrames frames, string animName, float speedScale, int zIndex)
	{
		if (_sprite == null) return;

		ZIndex = zIndex;
		_sprite.SpeedScale = speedScale;

		if (frames == null)
		{
			GD.PushWarning("[ShieldAuraVfx] Frames null.");
			return;
		}

		_sprite.SpriteFrames = frames;

		if (!frames.HasAnimation(animName))
		{
			GD.PushWarning($"[ShieldAuraVfx] Frames não tem animação '{animName}'.");
			var names = frames.GetAnimationNames(); // string[]
			if (names.Length > 0) animName = names[0];
		}

		_sprite.Play(animName);
	}
}
