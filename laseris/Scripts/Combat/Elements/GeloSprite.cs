using Godot;

public partial class GeloSprite : ElementIcon
{
	public override void _Process(double delta)
	{
		 if (Input.IsActionJustPressed("Ice"))
		{
			TryActivate();
		}

		if (Input.IsActionJustPressed("cast"))
		{
			controller.Cast();
		}
	}
}
