using Godot;

public partial class DamagePopup : Node2D
{
	[Export] public float RisePixels = 70f;
	[Export] public float Duration = 0.65f;
	[Export] public float SideJitter = 18f;

	private Label _label;

	public override void _Ready()
	{
		_label = GetNode<Label>("Label");
	}

	public void Play(string text, Color color, float scale = 1.0f)
	{
		_label.Text = text;
		_label.Modulate = color;

		// pequeno jitter lateral pra não ficar sempre igual
		float jitterX = (float)GD.RandRange(-SideJitter, SideJitter);
		Vector2 start = GlobalPosition + new Vector2(jitterX, 0);
		Vector2 end = start + new Vector2(0, -RisePixels);

		GlobalPosition = start;
		Scale = new Vector2(0.8f, 0.8f) * scale;
		Modulate = new Color(1, 1, 1, 1);

		var tw = CreateTween();
		tw.SetTrans(Tween.TransitionType.Back);
		tw.SetEase(Tween.EaseType.Out);

		// sobe
		tw.TweenProperty(this, "global_position", end, Duration);

		// bounce de escala (paralelo)
		tw.Parallel().TweenProperty(this, "scale", new Vector2(1.15f, 1.15f) * scale, 0.12f);
		tw.Parallel().TweenProperty(this, "scale", new Vector2(1.0f, 1.0f) * scale, 0.18f).SetDelay(0.12f);

		// fade out no final
		tw.TweenProperty(this, "modulate:a", 0.0f, 0.20f).SetDelay(Duration - 0.20f);

		tw.Finished += QueueFree;
	}
}
