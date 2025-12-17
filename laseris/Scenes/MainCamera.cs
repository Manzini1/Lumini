using Godot;

public partial class MainCamera : Camera2D
{
	public override void _Ready()
	{
		// Garante que esta câmera vire a câmera ativa do viewport
		MakeCurrent();
	}
}
