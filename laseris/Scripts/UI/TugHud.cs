using Godot;

public partial class TugHud : Control
{
	[ExportCategory("Refs")]
	[Export] public NodePath TugPath = "/root/Tug";

	[ExportCategory("Layout")]
	[Export] public float LineWidth = 520f;
	[Export] public float LineThickness = 3f;
	[Export] public float DotRadius = 7f;
	[Export] public float YOffset = 50f; // distância do topo

	[ExportCategory("Smoothing")]
	[Export] public float FollowSpeed = 10f;

	private TugManager _tug;
	private float _shownValue;

	public override void _Ready()
	{
		_tug = GetNodeOrNull<TugManager>(TugPath);
		if (_tug == null)
			GD.PushWarning("[TugHud] Não encontrei TugManager em /root/Tug. (Autoload?)");
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		float target = _tug != null ? _tug.Value : 0f;
		_shownValue = Mathf.Lerp(_shownValue, target, 1f - Mathf.Exp(-FollowSpeed * dt));
		QueueRedraw();
	}

	public override void _Draw()
	{
		Vector2 center = new Vector2(Size.X * 0.5f, YOffset);

		float half = LineWidth * 0.5f;
		Vector2 a = center + new Vector2(-half, 0);
		Vector2 b = center + new Vector2(+half, 0);

		// linha
		DrawLine(a, b, Colors.White, LineThickness);

		// limites (marquinhas)
		DrawLine(a + new Vector2(0, -10), a + new Vector2(0, +10), Colors.White, 2);
		DrawLine(b + new Vector2(0, -10), b + new Vector2(0, +10), Colors.White, 2);

		// dot (Value -1..+1)
		float x = center.X + (_shownValue * half);
		Vector2 dot = new Vector2(x, center.Y);

		DrawCircle(dot, DotRadius, Colors.White);
	}
}
