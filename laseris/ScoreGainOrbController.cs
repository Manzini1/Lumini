using Godot;
using System;

public partial class ScoreGainOrbController : Control
{
	[Export] public NodePath OrbPath = "Orb";
	[Export] public NodePath TrailPath = "Trail";
	[Export] public NodePath BurstPath = "Burst";
	[Export] public NodePath DeltaFxPath = "DeltaText"; // ScoreTextFx (Control)

	[ExportGroup("Timing")]
	[Export] public float RiseTime = 0.18f;
	[Export] public float TravelTime = 0.42f;

	[ExportGroup("Curve")]
	[Export] public float ArcPixels = 90f;
	[Export] public float WobbleAmp = 14f;
	[Export] public float WobbleFreq = 10f;
	[Export] public float RandomArcJitter = 0.35f;

	private ScoreTextFxController _deltaFx;
	private Control _orb;                 // ✅ era CanvasItem
	private GpuParticles2D _trail;
	private GpuParticles2D _burst;

	private Vector2 _start;
	private Vector2 _end;
	private Vector2 _ctrl;
	private Vector2 _perp;

	private float _t;
	private float _phase;

	private bool _traveling;
	private Action _onAbsorb;

	public override void _Ready()
	{
		_deltaFx = GetNodeOrNull<ScoreTextFxController>(DeltaFxPath);
		_orb = GetNodeOrNull<Control>(OrbPath);
		_trail = GetNodeOrNull<GpuParticles2D>(TrailPath);
		_burst = GetNodeOrNull<GpuParticles2D>(BurstPath);

		if (_deltaFx != null) _deltaFx.Hide();
		if (_orb != null) _orb.Hide();
		if (_trail != null) _trail.Emitting = false;
		if (_burst != null) _burst.Emitting = false;
	}

	/// <param name="fromGlobal">onde nasce o número</param>
	/// <param name="toGlobal">tip da barra (GetTipGlobalCenter)</param>
	/// <param name="leadingFx">se quer metal (true) ou rubi (false)</param>
	public void Play(int amount, Vector2 fromGlobal, Vector2 toGlobal, bool leadingFx, Action onAbsorb)
	{
		_onAbsorb = onAbsorb;

		_start = fromGlobal;
		_end = toGlobal;

		GlobalPosition = _start;

		if (_deltaFx != null)
		{
			_deltaFx.Show();
			_deltaFx.SetText($"+{amount}");
			_deltaFx.SetLeading(leadingFx);
			_deltaFx.Scale = Vector2.One;
		}
		else
		{
			GD.PushWarning("[ScoreGainOrb] DeltaFx não encontrado. Verifique DeltaFxPath.");
		}

		if (_orb != null)
		{
			_orb.Hide();
			_orb.Modulate = new Color(1, 1, 1, 1);
			_orb.Scale = Vector2.One;
		}

		if (_trail != null) _trail.Emitting = false;
		if (_burst != null) _burst.Emitting = false;

		_phase = (float)GD.RandRange(0.0, 6.283185);

		var riseTo = _start + new Vector2(0, -28);

		var tw = CreateTween();
		tw.SetEase(Tween.EaseType.Out);
		tw.SetTrans(Tween.TransitionType.Quad);

		tw.TweenProperty(this, "global_position", riseTo, Mathf.Max(0.01f, RiseTime));

		// ✅ respiração no ScoreTextFx (não no Label antigo)
		if (_deltaFx != null)
		{
			tw.Parallel().TweenProperty(_deltaFx, "scale", Vector2.One * 1.08f, RiseTime * 0.6f);
			tw.TweenProperty(_deltaFx, "scale", Vector2.One, RiseTime * 0.4f);
		}

		tw.TweenCallback(Callable.From(() => BeginTravel(riseTo)));
	}

	private void BeginTravel(Vector2 startFrom)
	{
		// some com o número e mostra orb
		if (_deltaFx != null) _deltaFx.Hide();
		if (_orb != null) _orb.Show();

		_start = startFrom;
		GlobalPosition = _start;

		Vector2 dir = (_end - _start);
		float len = dir.Length();
		if (len < 0.001f) len = 0.001f;
		dir /= len;

		_perp = new Vector2(-dir.Y, dir.X);

		float arc = ArcPixels * Mathf.Clamp(len / 520f, 0.55f, 1.35f);
		float jitter = 1.0f + (float)GD.RandRange(-RandomArcJitter, RandomArcJitter);
		arc *= jitter;

		if (GD.Randf() < 0.5f) arc *= -1f;

		Vector2 mid = (_start + _end) * 0.5f;
		_ctrl = mid + _perp * arc;

		_t = 0f;
		_traveling = true;

		if (_trail != null) _trail.Emitting = true;
	}

	public override void _Process(double delta)
	{
		if (!_traveling) return;

		_t += (float)(delta / Mathf.Max(0.01f, TravelTime));
		float t = Mathf.Clamp(_t, 0f, 1f);

		Vector2 p =
			(1 - t) * (1 - t) * _start +
			2 * (1 - t) * t * _ctrl +
			t * t * _end;

		float wob = Mathf.Sin(t * WobbleFreq + _phase + (float)Time.GetTicksMsec() * 0.0025f);
		p += _perp * wob * WobbleAmp * (1.0f - t);

		GlobalPosition = p;

		if (_orb != null)
		{
			float s = 1.0f + (1.0f - t) * 0.08f;
			_orb.Scale = new Vector2(s, s);
		}

		if (t >= 1.0f - 0.0001f)
			FinishAbsorb();
	}

	private void FinishAbsorb()
	{
		_traveling = false;

		if (_trail != null) _trail.Emitting = false;

		if (_burst != null)
		{
			_burst.GlobalPosition = _end;
			_burst.Emitting = true;
		}

		_orb?.Hide();

		_onAbsorb?.Invoke();

		GetTree().CreateTimer(0.12f).Timeout += () =>
		{
			if (GodotObject.IsInstanceValid(this))
				QueueFree();
		};
	}
}
