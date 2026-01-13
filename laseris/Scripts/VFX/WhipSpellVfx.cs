using Godot;
using System;

public partial class WhipSpellVfx : Node2D, IVfxPlayable, ISpellVfxConfigurable
{
	public event Action Impacted;

	[ExportCategory("Refs")]
	[Export] public NodePath LinePath = "Whipline";
	[Export] public NodePath TelegraphNodePath = "Telegraph";

	[ExportCategory("Markers")]
	[Export] public string CasterMarkerName = "VfxCast";
	[Export] public string TargetMarkerName = "VfxCenter";

	[ExportCategory("Defaults (fallback se entry não tiver)")]
	[Export] public float DefaultTelegraphSeconds = 0.25f;
	[Export] public float DefaultStrikeSeconds = 0.10f;
	[Export] public float DefaultRetractSeconds = 0.15f;

	[Export] public int DefaultSegments = 14;

	// Curva em "eixos locais do segmento":
	// X = ao longo do dir (normalmente 0)
	// Y = perpendicular ao dir (ex: -40 p/ curvar pra “cima”)
	[Export] public Vector2 DefaultCurveOffset = new Vector2(0, -40);

	// Offset de origem em eixos locais do segmento (ex: x=-40 p/ nascer um pouco antes)
	[Export] public Vector2 DefaultOriginOffset = new Vector2(-40, 0);

	[ExportCategory("Debug")]
	[Export] public bool DebugLog = false;

	private Line2D _line;
	private Node2D _telegraph;

	private SpellVfxEntry _entry;
	private Node2D _caster;
	private Node2D _target;

	private float _t;
	private bool _configured;
	private bool _impactFired;

	private enum Phase { Telegraph, Strike, Retract, Done }
	private Phase _phase = Phase.Telegraph;

	public override void _Ready()
	{
		_line = GetNodeOrNull<Line2D>(LinePath);
		if (_line == null)
		{
			GD.PushError("[WhipSpellVfx] Line2D não encontrado em LinePath.");
			QueueFree();
			return;
		}

		_telegraph = GetNodeOrNull<Node2D>(TelegraphNodePath);

		// Segurança: começa invisível até Configure
		_line.Visible = false;
		_line.ClearPoints();
		if (_telegraph != null) _telegraph.Visible = false;

		if (!Engine.IsEditorHint() && !_configured)
			GD.PushWarning("[WhipSpellVfx] Configure() não foi chamado (pipeline de VFX).");
	}

	public void Configure(SpellVfxEntry entry, Node2D caster, Node2D target)
	{
		_configured = true;

		_entry = entry;
		_caster = caster;
		_target = target;

		_phase = Phase.Telegraph;
		_t = 0f;
		_impactFired = false;

		// Pra não herdar transform estranho do parent (se VfxRoot/Anchor mover)
		TopLevel = true;

		// Render base do entry
		if (_entry != null)
		{
			ZIndex = _entry.ZIndex;
			Scale = _entry.Scale;
		}

		if (_telegraph != null) _telegraph.Visible = true;

		if (DebugLog)
			GD.Print($"[WhipSpellVfx] Configure caster={caster?.Name} target={target?.Name} path={GetPath()}");

		// Força desenhar 1 frame já (telegraph)
		UpdateVisual(0f);
	}

	public override void _Process(double delta)
	{
		if (_line == null) return;
		if (_caster == null || !GodotObject.IsInstanceValid(_caster)) return;
		if (_target == null || !GodotObject.IsInstanceValid(_target)) return;

		float dt = (float)delta;
		_t += dt;

		float tele = DefaultTelegraphSeconds;
		float strike = DefaultStrikeSeconds;
		float retract = DefaultRetractSeconds;

		// (se você depois quiser overrides por entry, dá pra plugar aqui)

		switch (_phase)
		{
			case Phase.Telegraph:
				UpdateVisual(progress: 0f);

				if (_t >= tele)
				{
					_t = 0f;
					_phase = Phase.Strike;
					_line.Visible = true;
				}
				break;

			case Phase.Strike:
				{
					float p = Mathf.Clamp(_t / Mathf.Max(0.001f, strike), 0f, 1f);
					UpdateVisual(progress: p);

					if (!_impactFired && p >= 1f)
					{
						_impactFired = true;
						Impacted?.Invoke();
					}

					if (_t >= strike)
					{
						_t = 0f;
						_phase = Phase.Retract;
					}
				}
				break;

			case Phase.Retract:
				{
					float p = 1f - Mathf.Clamp(_t / Mathf.Max(0.001f, retract), 0f, 1f);
					UpdateVisual(progress: p);

					if (_t >= retract)
					{
						_phase = Phase.Done;
						QueueFree();
					}
				}
				break;
		}
	}

	private void UpdateVisual(float progress)
	{
		// Resolve posições no mundo
		Vector2 casterPos = ResolveMarker(_caster, CasterMarkerName, _caster.GlobalPosition);
		Vector2 targetPos = ResolveMarker(_target, TargetMarkerName, _target.GlobalPosition);

		// Entre caster->target (se o entry pedir)
		Vector2 origin = casterPos;

		// ✅ se seu SpellVfxEntry tem esses campos (pelas suas prints, tem)
		if (_entry != null && _entry.UseBetweenSpawn)
		{
			float t = Mathf.Clamp(_entry.BetweenT, 0f, 1f);
			origin = casterPos.Lerp(targetPos, t);

			// offset local em eixos do segmento
			ApplyLocalOffset(ref origin, casterPos, targetPos, _entry.BetweenOffsetLocal);
		}

		// Offset global comum do entry
		if (_entry != null)
		{
			origin += _entry.Offset;
			targetPos += _entry.Offset;
		}

		// Offset de origem default (em eixos do segmento)
		ApplyLocalOffset(ref origin, casterPos, targetPos, DefaultOriginOffset);

		// Coloca o nó no origin e trabalha com pontos locais
		GlobalPosition = origin;

		Vector2 endLocal = targetPos - origin;

		// Telegraph fica no origin
		if (_telegraph != null)
		{
			_telegraph.GlobalPosition = origin;
			_telegraph.Visible = (_phase == Phase.Telegraph);
		}

		// Sem progresso? deixa linha vazia “safe”
		if (progress <= 0.0001f)
		{
			_line.ClearPoints();
			return;
		}

		// Calcula curva (em local)
		Vector2 dir = endLocal;
		float len = dir.Length();
		if (len < 0.001f) len = 0.001f;

		Vector2 dirN = dir / len;
		Vector2 normalN = dirN.Rotated(Mathf.Pi * 0.5f);

		Vector2 curveVecLocal = dirN * DefaultCurveOffset.X + normalN * DefaultCurveOffset.Y;

		// desenha só até a fração progress
		float tMax = Mathf.Clamp(progress, 0f, 1f);

		int segs = Mathf.Max(2, DefaultSegments);
		int used = Mathf.Max(2, Mathf.RoundToInt(segs * tMax));

		_line.ClearPoints();

		for (int i = 0; i < used; i++)
		{
			float u = (used <= 1) ? 1f : (float)i / (used - 1);
			u *= tMax;

			Vector2 p = endLocal * u;

			// curva “chicote”
			float bulge = Mathf.Sin(Mathf.Pi * u); // 0..1..0
			p += curveVecLocal * bulge;

			_line.AddPoint(p);
		}

		if (DebugLog && used >= 2)
			GD.Print($"[WhipSpellVfx] progress={progress:0.00} used={used} end={endLocal} origin={origin}");
	}

	private static void ApplyLocalOffset(ref Vector2 pos, Vector2 startWorld, Vector2 endWorld, Vector2 localOffset)
	{
		Vector2 dir = endWorld - startWorld;
		if (dir.Length() < 0.001f) return;

		Vector2 dirN = dir.Normalized();
		Vector2 normalN = dirN.Rotated(Mathf.Pi * 0.5f);

		pos += dirN * localOffset.X + normalN * localOffset.Y;
	}

	private static Vector2 ResolveMarker(Node2D root, string markerName, Vector2 fallback)
	{
		if (root == null) return fallback;
		if (string.IsNullOrWhiteSpace(markerName)) return fallback;

		var m = root.GetNodeOrNull<Marker2D>(markerName);
		return (m != null) ? m.GlobalPosition : fallback;
	}
}
