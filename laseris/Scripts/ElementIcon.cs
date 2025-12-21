using Godot;

public partial class ElementIcon : AnimatedSprite2D
{
	[Export] public ElementType ElementType;

	[Export] public string AnimIdle = "idle";
	[Export] public string AnimActive = "activate";

	[Export] private NodePath ElementControllerPath;
	public ElementController _controller;
	
	public override void _Ready()
	{
		_controller = GetNode<ElementController>("../../ElementController");
		Play(AnimIdle);
	}

	public override void _Process(double delta)
	{ 
		// Você já tem input map por elemento; aqui é só exemplo se quiser.
	}

	public void SetActive(bool active)
	{
		Play(active ? AnimActive : AnimIdle);
	}

	public void ResetElement()
	{
		Play(AnimIdle);
	}

	// Você chama isso pelo seu input handling atual (quando tecla do elemento é apertada)
	public void TryActivate()
	{
		if (_controller == null) return;
		if (!_controller.CanActivate()) return;

		_controller.ActivateElement(this);
	}
}
