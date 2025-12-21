using Godot;

public partial class ShadowAnimation : ElementIcon
{
	public override void _Process(double delta)
	{
		 if (Input.IsActionJustPressed("Darkness"))
		{
			TryActivate();
		}

		if (Input.IsActionJustPressed("cast"))
		{
			_controller.Cast();
		}
	}
}
