using Godot;

public partial class VenenoSprite : ElementIcon
{
	public override void _Process(double delta)
	{
		 if (Input.IsActionJustPressed("Venom"))
		{
			TryActivate();
		}

		if (Input.IsActionJustPressed("cast"))
		{
			_controller.Cast();
		}
	}
}
