using Godot;
using System.Collections.Generic;

public partial class DevResolutionTester : Node
{
	private int _i = 0;

	private readonly List<Vector2I> _res = new()
	{
		new Vector2I(1920, 1080),
		new Vector2I(1366, 768),
		new Vector2I(1280, 720),
		new Vector2I(2560, 1440),
		new Vector2I(3440, 1440),
		new Vector2I(1080, 1920),
	};

	public override void _Ready()
	{
		// garante que o node recebe input
		SetProcessInput(true);
		SetProcessUnhandledInput(true);

		GD.Print("[RES] DevResolutionTester ativo.");
	}

	// pega antes da UI consumir
	public override void _Input(InputEvent e)
	{
		if (e is not InputEventKey k || !k.Pressed || k.Echo) return;

		if (k.Keycode == Key.F2 || k.Keycode == Key.Key2)
		{
			_i = (_i + 1) % _res.Count;
			Apply();
		}
		else if (k.Keycode == Key.F1 || k.Keycode == Key.Key1)
		{
			_i = (_i - 1 + _res.Count) % _res.Count;
			Apply();
		}
	}

	private void Apply()
	{
		var r = _res[_i];

		// resizing só funciona de verdade em modo janela
		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		DisplayServer.WindowSetSize(r);

		// opcional: centralizar
		var screen = DisplayServer.ScreenGetUsableRect();
		var pos = screen.Position + (screen.Size - r) / 2;
		DisplayServer.WindowSetPosition(pos);

		GD.Print($"[RES] {r.X}x{r.Y}");
	}
}
