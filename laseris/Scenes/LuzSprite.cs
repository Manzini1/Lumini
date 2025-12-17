using Godot;

public partial class LuzSprite : ElementIcon
{
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("Light"))
		{
			TryActivate();
		}

		if (Input.IsActionJustPressed("cast"))
		{
			controller.Cast();
		}
	}
}
