using Godot;

namespace Game.Combat;

public partial class LightBeamVfx : Node2D
{
	[Export] public Line2D Line;
	[Export] public float LifeSeconds = 0.08f;
	[Export] public float FadeOutSeconds = 0.03f;

	public override void _Ready()
	{
		Line ??= GetNodeOrNull<Line2D>("Line");
	}

	public void Play(Vector2 from, Vector2 to, float seconds = 0.06f)
	{
		if (Line == null) { QueueFree(); return; }

		GlobalPosition = from;
		Vector2 localTo = to - from;

		Line.ClearPoints();
		Line.AddPoint(Vector2.Zero);
		Line.AddPoint(localTo);

		// aparece rápido e some
		Modulate = new Color(1,1,1,1);

		var tw = CreateTween();
		tw.TweenInterval(Mathf.Max(0.0f, LifeSeconds - FadeOutSeconds));
		tw.TweenProperty(this, "modulate", new Color(1,1,1,0), FadeOutSeconds);
		tw.TweenCallback(Callable.From(QueueFree));
	}
}
