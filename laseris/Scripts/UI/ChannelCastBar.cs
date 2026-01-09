using Godot;
using System;

public partial class ChannelCastBar : Control
{
	[ExportCategory("Refs")]
	[Export] public NodePath BarPath = "Bar";   // TextureProgressBar
	[Export] public NodePath TextPath = "Txt";  // Label opcional

	[ExportCategory("Find Controller")]
	[Export] public NodePath ElementControllerPath;
	[Export] public string ElementControllerGroup = "element_controller";

	[ExportCategory("Smoothing")]
	[Export] public float SmoothSpeed = 18f; // maior = mais “snappy”, menor = mais suave

	private TextureProgressBar _bar;
	private Label _txt;
	private ElementController _ec;

	private bool _running;
	private float _duration = 1f;
	private float _elapsed = 0f;

	// valor exibido (suavizado)
	private float _shown01 = 0f;

	public override void _Ready()
	{
		//HideBar();
		_bar = GetNodeOrNull<TextureProgressBar>(BarPath);
		_txt = GetNodeOrNull<Label>(TextPath);

		if (_bar == null)
		{
			GD.PushError("[CastChannelBar] BarPath inválido (não achei TextureProgressBar).");
			return;
		}

		_ec = !ElementControllerPath.IsEmpty
			? GetNodeOrNull<ElementController>(ElementControllerPath)
			: GetTree().GetFirstNodeInGroup(ElementControllerGroup) as ElementController;

		if (_ec == null)
		{
			GD.PushWarning("[CastChannelBar] Não achei ElementController (seta ElementControllerPath ou põe ele no group).");
			HideBar();
			return;
		}

		// Só esses 3 eventos:
		_ec.DualChannelStarted += OnStarted;
		_ec.DualChannelCancelled += OnCancelled;
		_ec.DualChannelReleased += OnReleased;

		_bar.MinValue = 0;
		_bar.MaxValue = 1;
		_bar.Step = 0; // contínuo
		HideBar();
	}

	public override void _ExitTree()
	{
		if (_ec == null) return;
		_ec.DualChannelStarted -= OnStarted;
		_ec.DualChannelCancelled -= OnCancelled;
		_ec.DualChannelReleased -= OnReleased;
	}

	private void OnStarted(float duration)
	{
		_duration = Mathf.Max(0.01f, duration);
		_elapsed = 0f;
		_running = true;
		_shown01 = 0f;

		_bar.Value = 0;
		Visible = true;
	}

	private void OnCancelled()
	{
		_running = false;
		HideBar();
	}

	private void OnReleased()
	{
		_running = false;
		HideBar();
	}

	public override void _Process(double delta)
	{
		if (!_running) return;

		float dt = (float)delta;
		_elapsed += dt;

		float target01 = Mathf.Clamp(_elapsed / _duration, 0f, 1f);

		// smoothing exponencial (fica bonito e estável)
		float k = Mathf.Max(1f, SmoothSpeed);
		float a = 1f - Mathf.Exp(-k * dt);
		_shown01 = Mathf.Lerp(_shown01, target01, a);

		_bar.Value = _shown01;

		if (_txt != null)
		{
			float left = Mathf.Max(0f, _duration - _elapsed);
			_txt.Text = $"{left:0.00}s";
		}

		if (_elapsed >= _duration)
		{
			_running = false;
			HideBar();
		}
	}

	private void HideBar()
	{
		Visible = false;
		if (_bar != null) _bar.Value = 0;
	}
}
