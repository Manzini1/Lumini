using Godot;

public partial class FireIcon : ElementIcon
{
	public override void _Process(double delta)
	{
	 if (Input.IsActionJustPressed("Fire"))
		{
			TryActivate();
		}

		if (Input.IsActionJustPressed("cast"))
		{
			_controller.Cast();
		}
	}
}
