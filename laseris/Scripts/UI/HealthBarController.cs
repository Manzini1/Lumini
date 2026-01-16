using Godot;

namespace Game.UI;

public partial class HealthBarController : Control
{
	[ExportCategory("Refs")]
	[Export] public NodePath NameLabelPath = "Name";
	[Export] public NodePath BarPath = "Bar";
	[Export] public NodePath ValueLabelPath = "Value";

	private Label _name;
	private ProgressBar _bar;
	private Label _value;

	public override void _Ready()
	{
		_name = GetNodeOrNull<Label>(NameLabelPath);
		_bar = GetNodeOrNull<ProgressBar>(BarPath);
		_value = GetNodeOrNull<Label>(ValueLabelPath);

		// defaults seguros
		if (_bar != null)
		{
			_bar.MinValue = 0;
			_bar.MaxValue = 100;
			_bar.Value = 100;
		}
		if (_value != null)
			_value.Text = "";
	}

	public void SetName(string name)
	{
		if (_name != null) _name.Text = name ?? "";
	}

	public void SetHp(int current, int max)
	{
		if (_bar == null) return;

		if (max <= 0) max = 1;
		current = Mathf.Clamp(current, 0, max);

		_bar.MaxValue = max;
		_bar.Value = current;

		if (_value != null)
			_value.Text = $"{current}/{max}";
	}
}
