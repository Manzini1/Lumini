using Godot;
using System;

public partial class BeamSpellVfx : Node2D, IVfxPlayable, ISpellVfxConfigurable
{
	public event Action Impacted;

	[ExportCategory("Refs")]
	[Export] public NodePath BeamSpritePath = "BeamSprite";

	[ExportCategory("Markers")]
	[Export] public string CasterMarkerName = "VfxCast";
	[Export] public string TargetMarkerName = "VfxCenter";

	[ExportCategory("Debug")]
	[Export] public bool DebugLog = false;

	private Node2D _beamNode;                 // pode ser Sprite2D ou AnimatedSprite2D
	private Sprite2D _beamSprite;
	private AnimatedSprite2D _beamAnim;

	private bool _configured;

	private SpellVfxEntry _entry;
	private Node2D _caster;
	private Node2D _target;

	private float _elapsed;
	private bool _impactFired;

	private float _baseWidthPx = 1f;

	public override void _Ready()
	{
		_beamNode = GetNodeOrNull<Node2D>(BeamSpritePath);
		if (_beamNode == null)
		{
			GD.PushError("[BeamSpellVfx] BeamSprite não encontrado em BeamSpritePath.");
			QueueFree();
			return;
		}

		_beamSprite = _beamNode as Sprite2D;
		_beamAnim = _beamNode as AnimatedSprite2D;

		if (_beamSprite == null && _beamAnim == null)
		{
			GD.PushError("[BeamSpellVfx] BeamSprite precisa ser Sprite2D OU AnimatedSprite2D.");
			QueueFree();
			return;
		}

		_baseWidthPx = ResolveBaseWidthPx();
		if (_baseWidthPx <= 0.001f) _baseWidthPx = 1f;

		if (!Engine.IsEditorHint() && !_configured)
			GD.PushWarning("[BeamSpellVfx] Configure() não foi chamado.");
	}

	public void Configure(SpellVfxEntry entry, Node2D caster, Node2D target)
	{
		_configured = true;

		_entry = entry;
		_caster = caster;
		_target = target;

		_elapsed = 0f;
		_impactFired = false;

		TopLevel = true;
		ZAsRelative = false;

		if (_entry != null)
			ZIndex = _entry.ZIndex;

		// ✅ Se for AnimatedSprite2D, aplica SpriteFrames do entry
		if (_beamAnim != null)
		{
			if (_entry?.Frames == null)
				GD.PushWarning("[BeamSpellVfx] Entry.Frames está null (beam não vai aparecer).");
			else
			{
				_beamAnim.SpriteFrames = _entry.Frames;
				_beamAnim.SpeedScale = _entry.SpeedScale;
				if (_beamAnim.SpriteFrames.HasAnimation(_entry.AnimationName))
					_beamAnim.Play(_entry.AnimationName);
				else
					_beamAnim.Play(); // fallback
			}
		}
		else if (_beamSprite != null)
		{
			// Sprite2D precisa de Texture (se não tiver, nunca aparece)
			if (!_beamSprite.RegionEnabled && _beamSprite.Texture == null)
				GD.PushWarning("[BeamSpellVfx] BeamSprite (Sprite2D) está sem Texture.");
		}

		// recalcula largura base depois de setar frames
		_baseWidthPx = ResolveBaseWidthPx();
		if (_baseWidthPx <= 0.001f) _baseWidthPx = 1f;

		UpdateBeam(force: true);

		if (DebugLog)
			GD.Print($"[BeamSpellVfx] Configure baseWidthPx={_baseWidthPx:0.00} anim={_entry?.AnimationName}");
	}

	public override void _Process(double delta)
	{
		_elapsed += (float)delta;

		UpdateBeam(force: false);

		float delay = _entry?.BeamImpactDelaySeconds ?? 0f;
		if (!_impactFired && _elapsed >= Mathf.Max(0f, delay))
		{
			_impactFired = true;
			Impacted?.Invoke();
		}

		if (_entry?.AutoFreeOnFinish ?? true)
		{
			float life = Mathf.Max(0.01f, _entry?.FallbackLifetime ?? 0.15f);
			if (_elapsed >= life)
				QueueFree();
		}
	}

	private void UpdateBeam(bool force)
	{
		if (_caster == null || !GodotObject.IsInstanceValid(_caster)) return;
		if (_target == null || !GodotObject.IsInstanceValid(_target)) return;

		Vector2 start = ResolveMarkerPos(_caster, CasterMarkerName, _caster.GlobalPosition);
		Vector2 end = ResolveMarkerPos(_target, TargetMarkerName, _target.GlobalPosition);

		if (_entry != null)
		{
			start += _entry.Offset;
			end += _entry.Offset;
		}

		Vector2 dir = end - start;
		float len = Mathf.Max(0.001f, dir.Length());
		float angle = dir.Angle();

		GlobalPosition = start;
		GlobalRotation = angle + ((_entry != null) ? Mathf.DegToRad(_entry.RotationDegrees) : 0f);

		Vector2 entryScale = _entry?.Scale ?? Vector2.One;

		float sx = (len / _baseWidthPx) * entryScale.X;
		float sy = Mathf.Max(0.01f, entryScale.Y);

		// estica no eixo X (comprimento) e Y (grossura)
		_beamNode.Scale = new Vector2(sx, sy);

		// ✅ desloca o sprite/anim pra frente (pra começar no caster e ir até o alvo)
		float renderedLen = _baseWidthPx * sx;
		_beamNode.Position = new Vector2(renderedLen * 0.5f, 0f);
	}

	private float ResolveBaseWidthPx()
	{
		if (_beamSprite != null)
		{
			if (_beamSprite.RegionEnabled) return _beamSprite.RegionRect.Size.X;
			if (_beamSprite.Texture != null) return _beamSprite.Texture.GetWidth();
			return 1f;
		}

		if (_beamAnim != null)
		{
			var frames = _beamAnim.SpriteFrames;
			if (frames == null) return 1f;

			string anim = frames.HasAnimation(_entry?.AnimationName ?? "play")
				? (_entry?.AnimationName ?? "play")
				: (frames.GetAnimationNames().Length > 0 ? frames.GetAnimationNames()[0] : "");

			if (string.IsNullOrEmpty(anim)) return 1f;

			var tex = frames.GetFrameTexture(anim, 0);
			return tex != null ? tex.GetWidth() : 1f;
		}

		return 1f;
	}

	private static Vector2 ResolveMarkerPos(Node2D root, string markerName, Vector2 fallback)
	{
		if (root == null) return fallback;
		if (string.IsNullOrWhiteSpace(markerName)) return fallback;
		var m = root.GetNodeOrNull<Marker2D>(markerName);
		return (m != null) ? m.GlobalPosition : fallback;
	}
}
