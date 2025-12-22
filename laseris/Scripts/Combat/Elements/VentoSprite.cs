using Godot;

public partial class VentoSprite : ElementIcon
{
	public override void _Process(double delta)
	{
		 if (Input.IsActionJustPressed("Air"))
		{
			TryActivate();
		}

		if (Input.IsActionJustPressed("cast"))
		{
			_controller.Cast();
		}
	}
}
