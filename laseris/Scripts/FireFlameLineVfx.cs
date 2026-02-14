using Godot;
using System;

public partial class FireFlameLineVfx : Node2D
{
	[ExportGroup("Debug")]
	[Export] public bool DebugPrint = false;
	[ExportGroup("Trail (cone feel)")]
[Export] public bool UseTrailParticles = true;
[Export] public float TrailFillHalfHeight = 14f; // “meia largura” do cone ao longo do caminho
	[ExportGroup("Refs")]
	[Export] public NodePath GlowLinePath = "GlowLine";
	[Export] public NodePath MidLinePath  = "MidLine";
	[Export] public NodePath CoreLinePath = "CoreLine";
	[Export] public NodePath MuzzleParticlesPath = "MuzzleParticles";
	[Export] public NodePath TrailParticlesPath  = "TrailParticles"; // ✅ novo
	[Export] public NodePath HitParticlesPath    = "HitParticles";
	[Export] public NodePath HitFlarePath        = "HitFlare";

	[ExportGroup("Timing")]
	[Export] public float LifeSeconds = 0.12f;
	[Export] public float FadeOutAt = 0.45f;
	[Export] public float HoldAfterEndSeconds = 0.08f;

	[ExportGroup("Flare")]
	[Export] public float FlareAtPercent = 0.55f;
	[Export] public string FlareAnimName = "default";

	[ExportGroup("Widths (start -> end)")]
	[Export] public float GlowWidthStart = 26f;
	[Export] public float GlowWidthEnd   = 10f;
	[Export] public float MidWidthStart  = 18f;
	[Export] public float MidWidthEnd    = 7f;
	[Export] public float CoreWidthStart = 9f;
	[Export] public float CoreWidthEnd   = 3f;

	[ExportGroup("Alpha (start -> end)")]
	[Export] public float AlphaStart = 1.0f;
	[Export] public float AlphaEnd   = 0.0f;

	[ExportGroup("Whip (curved)")]
	[Export] public bool UseWhip = false;
	[Export] public int WhipSegments = 8;
	[Export] public float WhipAmplitude = 14f;
	[Export] public float WhipFrequency = 10f;
	[Export] public float WhipSpeed = 22f;

	[ExportGroup("Trail (cone feel)")]
	
	[Export] public float TrailFollowPercent = 0.55f; // 0..1
	[Export] public bool TrailFollowsEveryFrame = false;

	private Line2D _glow, _mid, _core;
	private GpuParticles2D _muzzle, _trail, _hit;
	private AnimatedSprite2D _flare;

	private double _t0;
	private float _duration;
	private bool _playing;

	private Vector2 _toLocal = Vector2.Zero; // (len, 0) local
	private bool _flarePlayed;

	public override void _Ready()
	{
		_glow = GetNodeOrNull<Line2D>(GlowLinePath);
		_mid  = GetNodeOrNull<Line2D>(MidLinePath);
		_core = GetNodeOrNull<Line2D>(CoreLinePath);

		_muzzle = GetNodeOrNull<GpuParticles2D>(MuzzleParticlesPath);
		_trail  = GetNodeOrNull<GpuParticles2D>(TrailParticlesPath);
		_hit    = GetNodeOrNull<GpuParticles2D>(HitParticlesPath);

		_flare = GetNodeOrNull<AnimatedSprite2D>(HitFlarePath);

		if (_muzzle != null) _muzzle.Emitting = false;
		if (_trail != null)  _trail.Emitting = false;
		if (_hit != null)    _hit.Emitting = false;

		if (_flare != null)
		{
			_flare.Visible = false;
			_flare.Stop();
		}

		TopLevel = true; // ignora transform do parent (ajuda MUITO no Line2D)

		if (DebugPrint)
		{
			GD.Print($"[FireFlameLineVfx] Ready glow={_glow!=null} mid={_mid!=null} core={_core!=null} muzzle={_muzzle!=null} trail={_trail!=null} hit={_hit!=null} flare={_flare!=null}");
		}
	}

	// Chamado via ElementVfxLibrary: node.Call("Play", fromGlobal, toGlobal, travelSec)
	public void Play(Vector2 fromGlobal, Vector2 toGlobal, float travelSec = 0.10f)
	{
		_duration = Mathf.Max(LifeSeconds, Mathf.Max(0.04f, travelSec));
		_t0 = Time.GetTicksMsec() / 1000.0;
		_playing = true;
		_flarePlayed = false;

		Vector2 dir = toGlobal - fromGlobal;
		float len = dir.Length();
		if (len < 0.001f) len = 0.001f;

		GlobalPosition = fromGlobal;
		GlobalRotation = dir.Angle(); // aponta pro inimigo
		GlobalScale = Vector2.One;

		// como rotacionamos o root, o “fim” local é (len, 0)
		_toLocal = new Vector2(len, 0f);

		if (DebugPrint)
			GD.Print($"[FireFlameLineVfx] Play from={fromGlobal} to={toGlobal} len={len:0.0} dur={_duration:0.000}s");

		UpdateLines(0f, forceStraight: true);
		StartParticlesLocal(len);

		if (_flare != null)
		{
			_flare.Visible = false;
			_flare.Stop();
			_flare.Position = _toLocal;
		}
	}

	public override void _Process(double delta)
	{
		if (!_playing) return;

		double now = Time.GetTicksMsec() / 1000.0;
		float elapsed = (float)(now - _t0);

		float p = Mathf.Clamp(elapsed / _duration, 0f, 1f);

		SetLineWidth(_glow, Mathf.Lerp(GlowWidthStart, GlowWidthEnd, p));
		SetLineWidth(_mid,  Mathf.Lerp(MidWidthStart,  MidWidthEnd,  p));
		SetLineWidth(_core, Mathf.Lerp(CoreWidthStart, CoreWidthEnd, p));

		float fadeT = (p < FadeOutAt) ? 0f : Mathf.InverseLerp(FadeOutAt, 1f, p);
		float a = Mathf.Lerp(AlphaStart, AlphaEnd, fadeT);
		SetLineAlpha(_glow, a);
		SetLineAlpha(_mid,  a);
		SetLineAlpha(_core, a);

		UpdateLines(elapsed);

		// opcional: trail “seguindo”
		if (UseTrailParticles && TrailFollowsEveryFrame && _trail != null)
		{
			_trail.Position = _toLocal * Mathf.Clamp(TrailFollowPercent, 0f, 1f);
		}

		if (!_flarePlayed && p >= FlareAtPercent)
		{
			_flarePlayed = true;
			if (_flare != null)
			{
				_flare.Visible = true;
				if (string.IsNullOrEmpty(FlareAnimName)) _flare.Play();
				else _flare.Play(FlareAnimName);
			}
		}

		if (elapsed >= _duration + HoldAfterEndSeconds)
		{
			_playing = false;
			QueueFree();
		}
	}

	private void UpdateLines(float elapsed, bool forceStraight = false)
	{
		Vector2 a = Vector2.Zero;
		Vector2 b = _toLocal;

		if (!forceStraight && UseWhip)
		{
			int segs = Mathf.Clamp(WhipSegments, 2, 24);
			Vector2 n = Vector2.Up;

			var pts = new Vector2[segs + 1];
			for (int i = 0; i <= segs; i++)
			{
				float t = (float)i / segs;
				Vector2 baseP = a.Lerp(b, t);

				float falloff = (1f - t);
				falloff *= falloff;

				float wave = Mathf.Sin(t * WhipFrequency + elapsed * WhipSpeed);
				float amp = WhipAmplitude * falloff;

				pts[i] = baseP + n * wave * amp;
			}

			SetPoints(_glow, pts);
			SetPoints(_mid,  pts);
			SetPoints(_core, pts);
			return;
		}

		var straight = new Vector2[] { a, b };
		SetPoints(_glow, straight);
		SetPoints(_mid,  straight);
		SetPoints(_core, straight);
	}

	private void StartParticlesLocal(float len)
	{
		// Como rotacionamos o root, direção deve ser LOCAL:
		// +X = “vai pro inimigo”
		Vector2 fwd = new Vector2(1f, 0f);

		if (_muzzle != null)
		{
			_muzzle.Position = Vector2.Zero;
			ApplyParticleDirectionLocal(_muzzle, fwd);
			_muzzle.Restart();
			_muzzle.Emitting = true;
		}

			if (UseTrailParticles && _trail != null)
		{
			float halfLen = len * 0.5f;

			// Trail fica no CENTRO do caminho
			_trail.Position = new Vector2(halfLen, 0f);

			if (_trail.ProcessMaterial is ParticleProcessMaterial mat)
			{
				// Emite em um “retângulo” ao longo do caminho inteiro
				mat.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box;
				mat.EmissionBoxExtents = new Vector3(halfLen, TrailFillHalfHeight, 0f);

				// Direção local +X (já que o root foi rotacionado pro alvo)
				mat.Direction = new Vector3(1f, 0f, 0f);
			}

			_trail.Restart();
			_trail.Emitting = true;
		}

		if (_hit != null)
		{
			_hit.Position = _toLocal;
			ApplyParticleDirectionLocal(_hit, -fwd);
			_hit.Restart();
			_hit.Emitting = true;
		}
	}

	private static void ApplyParticleDirectionLocal(GpuParticles2D p, Vector2 dLocal)
	{
		if (p == null) return;
		if (p.ProcessMaterial is ParticleProcessMaterial mat)
			mat.Direction = new Vector3(dLocal.X, dLocal.Y, 0f);
	}

	private static void SetPoints(Line2D line, Vector2[] pts)
	{
		if (line == null) return;
		line.Points = pts;
	}

	private static void SetLineWidth(Line2D line, float w)
	{
		if (line == null) return;
		line.Width = Mathf.Max(0.5f, w);
	}

	private static void SetLineAlpha(Line2D line, float a)
	{
		if (line == null) return;
		var m = line.Modulate;
		m.A = Mathf.Clamp(a, 0f, 1f);
		line.Modulate = m;
	}
}
