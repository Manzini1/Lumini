using Godot;

public partial class RhythmProjectile : Node2D
{
	[ExportCategory("Refs")]
	[Export] public NodePath SpritePath = "Sprite";

	[ExportCategory("Move")]
	[Export] public float TravelSeconds = 0.12f;

	private Sprite2D _sprite;

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<Sprite2D>(SpritePath);
	}

	public async void Launch(Vector2 startWorld, Node2D mage, int damage, bool blocked)
	{
		GlobalPosition = startWorld;

		var blockPoint = mage.GetNodeOrNull<Marker2D>("BlockPoint");
		Vector2 end = blocked && blockPoint != null ? blockPoint.GlobalPosition : mage.GlobalPosition;

		var tween = CreateTween();
		tween.TweenProperty(this, "global_position", end, Mathf.Max(0.01f, TravelSeconds))
			 .SetTrans(Tween.TransitionType.Sine)
			 .SetEase(Tween.EaseType.Out);

		await ToSignal(tween, Tween.SignalName.Finished);

		if (blocked)
		{
			await ToSignal(GetTree().CreateTimer(0.18f), SceneTreeTimer.SignalName.Timeout);
			QueueFree();
			return;
		}

		if (mage != null && mage.HasMethod("ApplyDamage"))
			mage.Call("ApplyDamage", damage);

		QueueFree();
	}
}
