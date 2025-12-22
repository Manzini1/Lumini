using Godot;

public partial class ShadowAnimation : ElementIcon
{
	public override void _Process(double delta)
	{
		 if (Input.IsActionJustPressed("Shadow"))
		{
			TryActivate();
		}

		if (Input.IsActionJustPressed("cast"))
		{
			_controller.Cast();
		}
	}
}
