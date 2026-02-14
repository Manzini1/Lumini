using Godot;
using System;

public partial class DamageTotalLabelController : Control
{
	[Export] public NodePath MainLabelPath;
	[Export] public NodePath GlowLabelPath;
	[Export] public PackedScene DeltaLabelScene; // opcional

	[ExportGroup("Format")]
	[Export] public string Prefix = "DMG ";
	[Export] public bool Thousands = true;

	[ExportGroup("Timing")]
	[Export] public float RollMin = 0.10f;
	[Export] public float RollMax = 0.22f;

	[ExportGroup("Rolling Digits")]
	[Export] public float StepRateNormal = 240.0f;
	[Export] public float StepRateBigHit = 320.0f;
	[Export] public int MaxStepsShown = 140;

	[ExportGroup("Punch")]
	[Export] public float PunchScale = 1.08f;
	[Export] public float PunchIn = 0.05f;
	[Export] public float PunchOut = 0.10f;

	[ExportGroup("Flash")]
	[Export] public float FlashTime = 0.06f;

	[ExportGroup("Glint (Shader on MainLabel)")]
	[Export] public float GlintDuration = 0.18f;
	[Export] public float GlintOvershoot = 0.20f;
	[Export] public float GlintBaseIntensity = 0.85f;
	[Export] public float GlintBigHitIntensity = 1.15f;

	private Label _main;
	private Label _glow;
	private ShaderMaterial _mainMat;

	private long _targetValue;
	private long _displayIntValue;

	private Tween _rollTween;
	private Tween _punchTween;
	private Tween _glintTween;

	private Vector2 _baseScale;

	public override void _Ready()
	{
		_main = GetNodeOrNull<Label>(MainLabelPath);
		_glow = GetNodeOrNull<Label>(GlowLabelPath);

		_baseScale = Scale;

		if (_main != null)
		{
			_mainMat = _main.Material as ShaderMaterial;
			if (_mainMat == null && _main.Material != null)
				GD.PushWarning("[DamageTotal] MainLabel.Material não é ShaderMaterial.");
		}

		SetImmediate(0);
	}

	public void SetImmediate(long v)
	{
		_targetValue = v;
		_displayIntValue = v;
		ApplyText(v);
		SetGlint(-1.0f, 0.0f);
	}

	public void Add(int amount, bool bigHit = false)
	{
		if (amount <= 0) return;

		long fromTarget = _targetValue;
		_targetValue += amount;
		long toTarget = _targetValue;

		float t = Mathf.Clamp(amount / 200f, 0f, 1f);
		float dur = Mathf.Lerp(RollMin, RollMax, t);
		if (bigHit) dur *= 1.25f;

		AnimateStepRolling(toTarget, dur, bigHit);
		Punch(bigHit);
		SpawnDelta(amount, bigHit);
		PlayGlint(bigHit);
	}

	private void AnimateStepRolling(long to, float duration, bool bigHit)
	{
		_rollTween?.Kill();

		long start = _displayIntValue;
		long delta = to - start;
		if (delta <= 0)
		{
			_displayIntValue = to;
			ApplyText(to);
			return;
		}

		float rate = bigHit ? StepRateBigHit : StepRateNormal;
		float ideal = delta / rate;

		float durFinal = Mathf.Max(0.05f, Mathf.Max(duration, ideal));
		durFinal = Mathf.Min(durFinal, 0.45f);

		long steps = delta;
		long jump = 1;

		if (steps > MaxStepsShown)
		{
			jump = (long)Math.Ceiling(steps / (double)MaxStepsShown);
			steps = (long)Math.Ceiling(delta / (double)jump);
		}

		float stepDt = durFinal / (float)steps;

		_rollTween = CreateTween();
		_rollTween.SetEase(Tween.EaseType.Out);
		_rollTween.SetTrans(Tween.TransitionType.Quad);

		long cur = start;

		for (long i = 0; i < steps; i++)
		{
			cur = Math.Min(to, cur + jump);
			long v = cur;

			_rollTween.TweenCallback(Callable.From(() =>
			{
				_displayIntValue = v;
				ApplyText(v);
			}));
			_rollTween.TweenInterval(stepDt);
		}

		_rollTween.TweenCallback(Callable.From(() =>
		{
			_displayIntValue = to;
			ApplyText(to);
		}));
	}

	private void PlayGlint(bool bigHit)
	{
		if (_mainMat == null) return;

		_glintTween?.Kill();
		_glintTween = CreateTween();
		_glintTween.SetEase(Tween.EaseType.Out);
		_glintTween.SetTrans(Tween.TransitionType.Quad);

		float intensity = bigHit ? GlintBigHitIntensity : GlintBaseIntensity;

		float from = -0.25f;
		float to = 1.25f + GlintOvershoot;

		SetGlint(from, intensity);

		_glintTween.TweenMethod(
			Callable.From<float>((x) => { SetGlint(x, intensity); }),
			from, to, Mathf.Max(0.05f, GlintDuration)
		);

		_glintTween.TweenCallback(Callable.From(() =>
		{
			SetGlint(-1.0f, 0.0f);
		}));
	}

	private void SetGlint(float pos, float intensity)
	{
		if (_mainMat == null) return;
		_mainMat.SetShaderParameter("glint_pos", pos);
		_mainMat.SetShaderParameter("glint_intensity", Mathf.Max(0.0f, intensity));
	}

	private void Punch(bool bigHit)
	{
		_punchTween?.Kill();
		_punchTween = CreateTween();
		_punchTween.SetEase(Tween.EaseType.Out);
		_punchTween.SetTrans(Tween.TransitionType.Back);

		float s = PunchScale * (bigHit ? 1.05f : 1.0f);

		_punchTween.TweenProperty(this, "scale", _baseScale * s, PunchIn);
		_punchTween.TweenProperty(this, "scale", _baseScale, PunchOut);

		if (_main != null)
		{
			var tw = CreateTween();
			tw.TweenProperty(_main, "modulate", new Color(1, 1, 1, 1), 0.0f);
			tw.TweenProperty(_main, "modulate", new Color(1.2f, 1.1f, 0.8f, 1), FlashTime);
			tw.TweenProperty(_main, "modulate", new Color(1, 1, 1, 1), FlashTime);
		}
	}

	private void SpawnDelta(int amount, bool bigHit)
	{
		if (DeltaLabelScene == null) return;

		var inst = DeltaLabelScene.Instantiate();
		if (inst is not Label l) { inst.QueueFree(); return; }

		AddChild(l);
		l.Text = $"+{amount}";
		l.Modulate = bigHit ? new Color(1.0f, 0.95f, 0.6f, 1) : new Color(1, 1, 1, 1);

		l.Position = new Vector2(0, 0);

		var tw = CreateTween();
		tw.SetEase(Tween.EaseType.Out);
		tw.SetTrans(Tween.TransitionType.Quad);

		Vector2 start = l.Position;
		Vector2 end = start + new Vector2(0, -18);

		tw.TweenProperty(l, "position", end, 0.22f);
		tw.Parallel().TweenProperty(l, "modulate:a", 0.0f, 0.22f);
		tw.TweenCallback(Callable.From(() =>
		{
			if (GodotObject.IsInstanceValid(l)) l.QueueFree();
		}));
	}

	private void ApplyText(long v)
	{
		string num = Thousands ? v.ToString("N0") : v.ToString("0");
		string text = $"{Prefix}{num}";

		if (_main != null) _main.Text = text;
		if (_glow != null) _glow.Text = text;
	}
}
