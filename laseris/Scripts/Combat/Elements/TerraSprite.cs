using Godot;

public partial class TerraSprite : ElementIcon
{
	public override void _Process(double delta)
	{
		 if (Input.IsActionJustPressed("Earth"))
		{
			TryActivate();
		}

		if (Input.IsActionJustPressed("cast"))
		{
			_controller.Cast();
		}
	}
}
