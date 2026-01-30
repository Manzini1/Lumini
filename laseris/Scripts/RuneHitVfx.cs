using Godot;

namespace Game.UI;

public partial class RuneHitVfx : Control
{
	[Export] public NodePath BurstPath = "Burst";
	[Export] public NodePath SparklesPath = "Sparkles";
	[Export] public float AutoFreeSeconds = 1.2f;

	private GpuParticles2D _burst;
	private GpuParticles2D _sparkles;

	public override void _Ready()
	{
		_burst = GetNodeOrNull<GpuParticles2D>(BurstPath);
		_sparkles = GetNodeOrNull<GpuParticles2D>(SparklesPath);

		// garante que não é comido por layout
		SetAnchorsPreset(LayoutPreset.TopLeft);
		Size = Vector2.Zero;
		MouseFilter = MouseFilterEnum.Ignore;
		ZIndex = 100;
	}

	public void Play()
	{
		if (_burst != null) { _burst.Emitting = false; _burst.Restart(); _burst.Emitting = true; }
		if (_sparkles != null) { _sparkles.Emitting = false; _sparkles.Restart(); _sparkles.Emitting = true; }

		GetTree().CreateTimer(AutoFreeSeconds).Timeout += () =>
		{
			if (IsInstanceValid(this)) QueueFree();
		};
	}
}
