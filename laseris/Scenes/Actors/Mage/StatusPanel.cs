using Godot;

public partial class StatusPanelFollow : PanelContainer
{
	[Export] public Vector2 PixelOffset = new(0, -10); // sobe mais um pouco

	public override void _Ready()
	{
		// espera 1 frame pro container calcular Size certinho
		CallDeferred(nameof(Align));
	}

	private void Align()
	{
		// queremos que o "pé" do painel fique no anchor:
		// X centralizado, Y acima do anchor
		Position = new Vector2(-Size.X * 0.5f, -Size.Y) + PixelOffset;
	}
}
