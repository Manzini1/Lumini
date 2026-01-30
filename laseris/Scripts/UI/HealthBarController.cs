using Godot;

namespace Game.UI;

public partial class HealthBarController : Control
{
	[ExportCategory("Refs")]
	[Export] public NodePath NameLabelPath = "Name";
	[Export] public NodePath BarPath = "Bar"; // agora aponta para TextureProgressBar
	[Export] public NodePath ValueLabelPath = "Value";

	private Label _name;
	private TextureProgressBar _bar;
	private Label _value;

	public override void _Ready()
	{
		_name = GetNodeOrNull<Label>(NameLabelPath);
		_bar  = GetNodeOrNull<TextureProgressBar>(BarPath);
		_value = GetNodeOrNull<Label>(ValueLabelPath);

		GD.Print($"[HealthBar] Ready at {GetPath()}");
		GD.Print($"[HealthBar] refs: name={_name!=null} bar={_bar!=null} value={_value!=null}");

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
