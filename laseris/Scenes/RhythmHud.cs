using Godot;
using System.Threading.Tasks;

public partial class RhythmHud : Control
{
	[ExportCategory("Refs")]
	[Export] public NodePath PreHintPath = "PreHint";
	[Export] public NodePath BeatHintPath = "BeatHint";
	[Export] public NodePath GradeLabelPath = "GradeLabel";
	[Export] public NodePath CountdownLabelPath = "CountdownLabel";
	[Export] public NodePath FlowBarPath = "FlowBar";

	[ExportCategory("Pulse")]
	[Export] public float PulseScaleUp = 1.25f;

	private Control _preHint;
	private Control _beatHint;
	private Label _gradeLabel;
	private Label _countdown;
	private ProgressBar _flowBar;

	public override void _Ready()
	{
		_preHint = GetNodeOrNull<Control>(PreHintPath);
		_beatHint = GetNodeOrNull<Control>(BeatHintPath);
		_gradeLabel = GetNodeOrNull<Label>(GradeLabelPath);
		_countdown = GetNodeOrNull<Label>(CountdownLabelPath);
		_flowBar = GetNodeOrNull<ProgressBar>(FlowBarPath);

		if (_preHint != null) _preHint.Visible = false;
		if (_beatHint != null) _beatHint.Visible = false;
		if (_gradeLabel != null) _gradeLabel.Text = "";
		if (_countdown != null) _countdown.Text = "";
		if (_flowBar != null) { _flowBar.MinValue = 0; _flowBar.MaxValue = 1; _flowBar.Value = 0; }
	}

	public void SetFlow(float flow01)
	{
		if (_flowBar != null) _flowBar.Value = Mathf.Clamp(flow01, 0f, 1f);
	}

	public void SetGrade(string text)
	{
		if (_gradeLabel != null) _gradeLabel.Text = text ?? "";
	}

	public void SetCountdown(string text)
	{
		if (_countdown != null) _countdown.Text = text ?? "";
	}

	public void HideCountdown()
	{
		if (_countdown != null) _countdown.Text = "";
	}

	public async void PulsePreHint(float seconds = 0.08f) => await PulseControl(_preHint, seconds, PulseScaleUp);
	public async void PulseBeatHint(float seconds = 0.10f) => await PulseControl(_beatHint, seconds, PulseScaleUp);

	private async Task PulseControl(Control c, float seconds, float scaleUp)
	{
		if (c == null) return;

		c.Visible = true;
		c.Scale = Vector2.One;

		var tween = CreateTween();
		tween.TweenProperty(c, "scale", Vector2.One * scaleUp, seconds * 0.5f);
		tween.TweenProperty(c, "scale", Vector2.One, seconds * 0.5f);

		await ToSignal(tween, Tween.SignalName.Finished);

		c.Visible = false;
	}
}
