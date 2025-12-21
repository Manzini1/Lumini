using Godot;

public partial class Mage : Sprite2D
{
	// Por enquanto, o Mage é só visual.
	// Não lê input, não faz cast, não gerencia combo.

	public override void _Ready()
	{
		GD.Print("Mage pronto.");
	}

	public override void _Process(double delta)
	{
		// Futuro: animações do personagem, idle, cast pose, recoil, etc.
	}
}
