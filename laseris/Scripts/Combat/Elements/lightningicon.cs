using Godot;

public partial class lightningicon : ElementIcon
{
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("lightning"))
		{
			TryActivate();
		}

		if (Input.IsActionJustPressed("cast"))
		{
			_controller.Cast();
		}
	}
}
