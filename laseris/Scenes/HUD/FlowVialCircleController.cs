using Godot;

namespace Game.UI;

public partial class FlowVialCircleController : Control
{
	[ExportGroup("Nodes")]
	[Export] public NodePath LiquidPath = "Liquid";
	[Export] public NodePath GlowPath = "Glow";

	[ExportGroup("Debug")]
	[Export] public bool DebugLogs = false;

	[ExportGroup("Smoothing")]
	[Export] public float SmoothUp = 10f;
	[Export] public float SmoothDown = 16f;

	[ExportGroup("Wobble Feel")]
	[Export] public float WobbleOnGain = 0.22f;
	[Export] public float WobbleOnLoss = 0.35f;
	[Export] public float WobbleDecay = 8f;

	[ExportGroup("Full / Charged")]
	[Export] public float FullThreshold = 0.999f;     // considera “cheio”
	[Export] public float UnchargeThreshold = 0.98f;  // abaixo disso desativa charged
	[Export] public float ChargedFadeIn = 0.12f;      // suaviza entrada do charged
	[Export] public float ChargedFadeOut = 0.18f;     // suaviza saída do charged

	private CanvasItem _liquid;
	private CanvasItem _glow;
	private ShaderMaterial _matLiquid;
	private ShaderMaterial _matGlow;

	private float _target01;
	private float _fill01;
	private float _wobble;

	private float _lastTargetForFlash = 0f;

	private Tween _flashTween;
	private Tween _chargedTween;

	private float _charged01 = 0f;
	private bool _isCharged = false;

	public override void _Ready()
	{
		_liquid = GetNodeOrNull<CanvasItem>(LiquidPath);
		_glow = GetNodeOrNull<CanvasItem>(GlowPath);

		if (_liquid == null) { GD.PushWarning("FlowVial: Liquid não encontrado."); return; }
		if (_glow == null) { GD.PushWarning("FlowVial: Glow não encontrado."); return; }

		_matLiquid = _liquid.Material as ShaderMaterial;
		_matGlow = _glow.Material as ShaderMaterial;

		if (_matLiquid == null) { GD.PushWarning("FlowVial: Liquid sem ShaderMaterial."); return; }
		if (_matGlow == null) { GD.PushWarning("FlowVial: Glow sem ShaderMaterial."); return; }

		_target01 = 0f;
		_fill01 = 0f;
		_wobble = 0f;
		_charged01 = 0f;
		_isCharged = false;

		// init params
		SetAll("fill01", 0f);
		SetAll("wobble", 0f);
		SetAll("time", 0f);
		SetAll("flash01", 0f);
		SetAll("charged01", 0f);
	}

	public void SetFill01(float f)
	{
		if (_matLiquid == null || _matGlow == null) return;

		f = Mathf.Clamp(f, 0f, 1f);

		// wobble ao mudar target
		if (f > _target01 + 0.0001f) _wobble = Mathf.Max(_wobble, WobbleOnGain);
		else if (f < _target01 - 0.0001f) _wobble = Mathf.Max(_wobble, WobbleOnLoss);

		_target01 = f;

		// ✅ flash quando o ALVO cruza “cheio” (não depende do smoothing)
		if (_lastTargetForFlash < FullThreshold && _target01 >= FullThreshold)
		{
			if (DebugLogs) GD.Print($"[FlowVial] FULL FLASH (by target) target={_target01:0.000}");
			TriggerFullFlash();
			SetCharged(true);
		}

		// se cair abaixo do limiar, descharge
		if (_isCharged && _target01 <= UnchargeThreshold)
		{
			if (DebugLogs) GD.Print($"[FlowVial] UNCHARGE target={_target01:0.000}");
			SetCharged(false);
		}

		_lastTargetForFlash = _target01;

		if (DebugLogs) GD.Print($"[FlowVial] SetFill01 target={_target01:0.000}");
	}

	public override void _Process(double delta)
	{
		if (_matLiquid == null || _matGlow == null) return;

		float dt = (float)delta;

		// smoothing fill
		float speed = (_target01 >= _fill01) ? SmoothUp : SmoothDown;
		_fill01 = Mathf.Lerp(_fill01, _target01, 1f - Mathf.Exp(-speed * dt));

		// wobble decay
		_wobble = Mathf.Lerp(_wobble, 0f, 1f - Mathf.Exp(-WobbleDecay * dt));

		float t = (float)Time.GetTicksMsec() * 0.001f;

		SetAll("fill01", _fill01);
		SetAll("wobble", _wobble);
		SetAll("time", t);
		SetAll("charged01", _charged01);
	}

	private void SetAll(string param, float value)
	{
		_matLiquid.SetShaderParameter(param, value);
		_matGlow.SetShaderParameter(param, value);
	}

	private void TriggerFullFlash()
	{
		if (_flashTween != null && GodotObject.IsInstanceValid(_flashTween))
			_flashTween.Kill();

		SetAll("flash01", 1.0f);

		_flashTween = CreateTween();
		_flashTween.TweenMethod(
			Callable.From<float>(v => SetAll("flash01", v)),
			1.0f, 0.0f, 0.18f
		).SetTrans(Tween.TransitionType.Quad)
		 .SetEase(Tween.EaseType.Out);
	}

	private void SetCharged(bool on)
	{
		_isCharged = on;

		if (_chargedTween != null && GodotObject.IsInstanceValid(_chargedTween))
			_chargedTween.Kill();

		float from = _charged01;
		float to = on ? 1.0f : 0.0f;
		float dur = on ? Mathf.Max(0.01f, ChargedFadeIn) : Mathf.Max(0.01f, ChargedFadeOut);

		_chargedTween = CreateTween();
		_chargedTween.TweenMethod(
			Callable.From<float>(v => _charged01 = v),
			from, to, dur
		).SetTrans(Tween.TransitionType.Quad)
		 .SetEase(on ? Tween.EaseType.Out : Tween.EaseType.In);
	}
}
