using Godot;

public partial class FireIcon : ElementIcon
{
	public override void _Process(double delta)
	{
	 if (Input.IsActionJustPressed("fire"))
		{
			TryActivate();
		}

		if (Input.IsActionJustPressed("cast"))
		{
			_controller.Cast();
		}
	}
}
