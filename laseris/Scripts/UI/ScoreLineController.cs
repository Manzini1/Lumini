using Godot;

public partial class ScoreLineController : Control
{
	[Export] public NodePath TrackPath = "Track";
	[Export] public NodePath FillMaskPath = "FillMask";
	[Export] public NodePath FillPath = "FillMask/Fill";
	[Export] public NodePath TipGlowPath = "TipGlow";

	// Mago (esquerda): true => cresce do centro pra esquerda
	// Inimigo (direita): false => cresce do centro pra direita
	[Export] public bool FillFromRight = false;

	[ExportGroup("Glow / Lead")]
	[Export] public float LeadBoostOn = 1.0f;
	[Export] public float LeadBoostOff = 0.0f;
	[Export] public float LeadPulseSpeed = 10.0f;
	[Export] public float TipGlowAlphaLead = 0.95f;
	[Export] public float TipGlowAlphaOff = 0.25f;

	private Control _track;
	private Control _fillMask;
	private CanvasItem _fill;
	private ColorRect _tipGlow;

	private float _current01 = 0f;
	private float _target01 = 0f;

	private Tween _tween;
	private bool _isLeading;

	public override void _Ready()
	{
		_track = GetNodeOrNull<Control>(TrackPath);
		_fillMask = GetNodeOrNull<Control>(FillMaskPath);
		_fill = GetNodeOrNull<CanvasItem>(FillPath);
		_tipGlow = GetNodeOrNull<ColorRect>(TipGlowPath);

		if (_fillMask != null)
		{
			// ✅ essencial pra máscara funcionar (clipping)
			_fillMask.ClipContents = true;

			// ✅ anchors fixos pra você controlar offsets sem warning
			_fillMask.AnchorLeft = 0f;
			_fillMask.AnchorRight = 0f;
			_fillMask.AnchorTop = 0f;
			_fillMask.AnchorBottom = 0f;
		}

		// espera layout ficar pronto
		CallDeferred(nameof(_InitVisualsDeferred));
	}

	private void _InitVisualsDeferred()
	{
		SetFillImmediate(0f);
		SetLeading(false, immediate: true);
	}

	public override void _Notification(int what)
	{
		// quando a UI mudar de tamanho (resize / container), recalcula
		if (what == NotificationResized)
			ApplyFill(_current01);
	}

	public void SetFillImmediate(float v01)
	{
		_current01 = Mathf.Clamp(v01, 0f, 1f);
		_target01 = _current01;
		CallDeferred(nameof(_ApplyFillDeferred));
	}

	private void _ApplyFillDeferred() => ApplyFill(_current01);

	public void AnimateFillTo(float v01, float duration)
	{
		_target01 = Mathf.Clamp(v01, 0f, 1f);

		_tween?.Kill();
		_tween = CreateTween();
		_tween.SetTrans(Tween.TransitionType.Quart);
		_tween.SetEase(Tween.EaseType.Out);

		float from = _current01;
		float to = _target01;

		_tween.TweenMethod(Callable.From<float>((x) =>
		{
			_current01 = x;
			ApplyFill(_current01);
		}), from, to, Mathf.Max(0.01f, duration));
	}

	public void SetLeading(bool leading, bool immediate = false)
	{
		_isLeading = leading;

		if (_tipGlow != null)
		{
			var m = _tipGlow.Modulate;
			m.A = leading ? TipGlowAlphaLead : TipGlowAlphaOff;
			_tipGlow.Modulate = m;
		}

		SetShaderLeadBoost(leading ? LeadBoostOn : LeadBoostOff);
		if (immediate) return;
	}

	public override void _Process(double delta)
	{
		var mat = GetFillShaderMaterial();
		if (_isLeading && mat != null)
		{
			float time = (float)Time.GetTicksMsec() / 1000f;
			float p = 0.5f + 0.5f * Mathf.Sin(time * LeadPulseSpeed);
			float boost = Mathf.Lerp(0.75f, 1.0f, p) * LeadBoostOn;
			mat.SetShaderParameter("lead_boost", boost);
		}
	}

	private void SetShaderLeadBoost(float v)
	{
		var mat = GetFillShaderMaterial();
		if (mat != null)
			mat.SetShaderParameter("lead_boost", v);
	}

	private ShaderMaterial GetFillShaderMaterial()
	{
		if (_fill == null) return null;
		return _fill.Material as ShaderMaterial;
	}

	private void ApplyFill(float v01)
	{
		if (_fillMask == null) return;

		float fullW = (_track != null && _track.Size.X > 0) ? _track.Size.X : Size.X;
		float fullH = (_track != null && _track.Size.Y > 0) ? _track.Size.Y : Size.Y;

		if (fullW <= 1f || fullH <= 1f) return;

		v01 = Mathf.Clamp(v01, 0f, 1f);
		float w = Mathf.Round(fullW * v01);

		float x0 = FillFromRight ? (fullW - w) : 0f;

		// ✅ sem mexer em Size (evita warning), só offsets
		_fillMask.OffsetLeft = x0;
		_fillMask.OffsetRight = x0 + w;
		_fillMask.OffsetTop = 0f;
		_fillMask.OffsetBottom = fullH;

		// TipGlow na “ponta”
		if (_tipGlow != null)
		{
			float tipW = _tipGlow.Size.X;
			float x = FillFromRight ? (fullW - w) - tipW * 0.5f : w - tipW * 0.5f;
			x = Mathf.Clamp(x, -tipW * 0.5f, fullW - tipW * 0.5f);

			_tipGlow.Position = new Vector2(x, (fullH - _tipGlow.Size.Y) * 0.5f);
			_tipGlow.Visible = w > 2;
		}
	}
}
