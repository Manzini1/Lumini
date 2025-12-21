using Godot;

public partial class VentoSprite : ElementIcon
{
	public override void _Process(double delta)
	{
		 if (Input.IsActionJustPressed("Wind"))
		{
			TryActivate();
		}

		if (Input.IsActionJustPressed("cast"))
		{
			controller.Cast();
		}
	}
}
