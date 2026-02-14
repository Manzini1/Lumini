using Godot;

public partial class AutoVfxSprite : Node2D
{
	[Export] public NodePath SpritePath = "Anim";
	[Export] public float FallbackLifetime = 0.6f;

	public override void _Ready()
	{
		var spr = GetNodeOrNull<AnimatedSprite2D>(SpritePath);

		if (spr != null)
		{
			// toca a animação padrão do spriteframes
			// (sem depender de existir "default")
			spr.Play();
			spr.AnimationFinished += () => QueueFree();
		}
		else
		{
			// fallback: se não tem sprite, mata depois de um tempo
			GetTree().CreateTimer(FallbackLifetime).Timeout += () => QueueFree();
		}
	}
}
