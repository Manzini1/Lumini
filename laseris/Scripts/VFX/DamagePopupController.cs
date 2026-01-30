using Godot;

public partial class DamagePopupController : Node2D
{
	[Export] public NodePath LabelPath = "Label";
	[Export] public float FloatPixels = 28f;
	[Export] public float DurationSeconds = 0.60f;

	// aumenta o tamanho geral sem depender de fonte
	[Export] public float BaseScale = 1.6f;

	private Label _label;

	public override void _Ready()
	{
		_label = GetNodeOrNull<Label>(LabelPath);
		Scale = Vector2.One * BaseScale;

		if (_label == null)
			GD.PushError("DamagePopupController: não achei Label em LabelPath.");
	}

	/// <summary>
	/// Ex: ShowText("10", Colors.Red, 1.8f)
	/// Ex: ShowText("BLOCKED", Colors.Cyan, 1.4f)
	/// </summary>
	public void ShowText(string text, Color color, float scaleMult = 1.0f)
	{
		if (_label != null)
		{
			_label.Text = text;
			_label.Modulate = color;
		}

		Scale = Vector2.One * BaseScale * Mathf.Max(0.1f, scaleMult);

		var start = Position;
		var end = start + new Vector2(0, -FloatPixels);

		Modulate = new Color(1, 1, 1, 1);

		var t = CreateTween();
		t.TweenProperty(this, "position", end, DurationSeconds)
		 .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);

		t.TweenProperty(this, "modulate:a", 0.0f, DurationSeconds)
		 .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Sine);

		t.Finished += QueueFree;
	}

	// compat se alguém ainda chamar ShowDamage(int)
	public void ShowDamage(int amount)
	{
		ShowText(amount.ToString(), Colors.White, 1.0f);
	}
}
