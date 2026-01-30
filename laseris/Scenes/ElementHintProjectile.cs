using Godot;
using System;

namespace Game.UI;

public partial class ElementHintProjectile : Control
{
	[ExportGroup("Refs")]
	[Export] public NodePath HintSpritePath = "HintSprite";     // Sprite2D OU TextureRect
	[Export] public NodePath HitParticlesPath = "HitParticles"; // GpuParticles2D (opcional)

	[ExportGroup("Spawn / Travel")]
	[Export] public float LeadSeconds = 0.50f;            // “prepare longo”
	[Export] public float SpawnOffsetPx = 300f;           // modo A
	[Export] public float SpawnOffsetViewportFrac = 0.28f;// modo B (recomendado)
	[Export] public bool UseViewportFrac = true;

	[Export] public float EndMarginPx = 40f;              // passa um pouco do fundo
	[Export] public float FadeOutBandPx = 120f;           // começa a “sumir” perto do fim

	[ExportGroup("Feedback")]
	[Export] public float PopScale = 1.15f;
	[Export] public float PopIn = 0.05f;
	[Export] public float PopOut = 0.10f;

	[Export] public float MissShakeDuration = 0.18f;
	[Export] public float MissShakeStrength = 8f;
	[Export] public float MissFadeOut = 0.12f;

	[ExportGroup("Feedback Colors")]
	[Export] public Color HitGreen = new Color(0.20f, 1.00f, 0.35f, 1f);
	[Export] public Color MissRed = new Color(1.00f, 0.20f, 0.20f, 1f);

	// Refs (aceita Sprite2D ou TextureRect)
	private CanvasItem _hintItem;     // Sprite2D ou TextureRect
	private GpuParticles2D _particles;

	private Vector2 _hintBaseScale = Vector2.One;

	// motion
	private bool _running;
	private bool _resolved;
	private double _beatSec;
	private double _startSec;
	private float _speed;  // px/sec
	private float _hitY;
	private float _spawnY;
	private float _endY;
	private float _x;

	private Color _elementColor = Colors.White;

	public override void _Ready()
	{
		ResolveRefs();

		// estado inicial
		Visible = false;
		Modulate = Colors.White;

		if (_particles != null)
			_particles.Emitting = false;
	}

	private void ResolveRefs()
	{
		// Hint item: tenta pelo path configurado
		_hintItem = GetNodeOrNull<CanvasItem>(HintSpritePath);

		// fallback: tenta achar por nome em filhos (caso você tenha mudado o path)
		if (_hintItem == null)
		{
			_hintItem = FindChild("HintSprite", recursive: true, owned: false) as CanvasItem;
		}

		// Valida tipo (Sprite2D OU TextureRect)
		if (_hintItem == null || (_hintItem is not Sprite2D && _hintItem is not TextureRect))
		{
			GD.PushWarning("[HintProjectile] HintSprite não encontrado ou tipo inválido. " +
						   "Use Sprite2D OU TextureRect e confira HintSpritePath.");
			_hintItem = null;
		}
		else
		{
			// captura escala base
			if (_hintItem is Node2D n2)
				_hintBaseScale = n2.Scale;
			else if (_hintItem is Control c)
				_hintBaseScale = c.Scale;
		}

		_particles = GetNodeOrNull<GpuParticles2D>(HitParticlesPath);
		if (_particles == null)
		{
			// fallback por nome
			_particles = FindChild("HitParticles", recursive: true, owned: false) as GpuParticles2D;
		}
	}

	/// <summary>
	/// Configura um hint que vai cruzar o centro da runa em beatSec.
	/// runeCenterCanvas: rune.GetGlobalTransformWithCanvas().Origin
	/// nowSec: tempo atual do áudio no momento do arm/spawn.
	/// </summary>
	public void Arm(Vector2 runeCenterCanvas, double beatSec, double nowSec, Color elementColor)
	{
		// refs podem não estar prontas se o node foi instanciado e Arm chamado no mesmo frame
		// (raro, mas possível). Garante:
		if (_hintItem == null && _particles == null)
			ResolveRefs();

		_resolved = false;
		_running = true;

		_beatSec = beatSec;
		_startSec = beatSec - LeadSeconds;

		_x = runeCenterCanvas.X;
		_hitY = runeCenterCanvas.Y;

		float viewportH = GetViewportRect().Size.Y;

		float offset = UseViewportFrac
			? Mathf.Max(1f, viewportH * SpawnOffsetViewportFrac)
			: Mathf.Max(1f, SpawnOffsetPx);

		_spawnY = _hitY - offset;
		_endY = viewportH + EndMarginPx;

		float distToHit = (_hitY - _spawnY);
		_speed = distToHit / Mathf.Max(0.01f, LeadSeconds);

		// reset de visuais
		Visible = true;
		Modulate = Colors.White;

		_elementColor = elementColor;
		ApplyColor(_elementColor);

		ResetHintScale();

		GlobalPosition = new Vector2(_x, _spawnY);

		UpdateNow(nowSec);
	}

	private void ResetHintScale()
	{
		if (_hintItem == null) return;

		if (_hintItem is Node2D n2)
			n2.Scale = _hintBaseScale;
		else if (_hintItem is Control c)
			c.Scale = _hintBaseScale;
	}

	private void ApplyColor(Color c)
	{
		// pinta o item (sprite/textureRect)
		if (_hintItem != null)
		{
			_hintItem.SelfModulate = Colors.White;
			_hintItem.Modulate = c;
		}

		// pinta partículas
		if (_particles != null)
		{
			_particles.SelfModulate = Colors.White;
			_particles.Modulate = c;
		}
	}

	/// <summary>Chame todo frame (ou via ElementBarController.SetSongTime)</summary>
	public void UpdateNow(double nowSec)
	{
		if (!_running || _resolved) return;

		if (nowSec < _startSec)
		{
			GlobalPosition = new Vector2(_x, _spawnY);
			ApplyFade(_spawnY);
			return;
		}

		float y = _spawnY + _speed * (float)(nowSec - _startSec);
		GlobalPosition = new Vector2(_x, y);

		ApplyFade(y);

		if (y > _endY)
			ResolveMiss();
	}

	private void ApplyFade(float y)
	{
		float fadeStart = _endY - FadeOutBandPx;

		float a = 1f;
		if (y >= fadeStart)
		{
			float t = Mathf.InverseLerp(fadeStart, _endY, y);
			a = 1f - t;
		}

		var m = Modulate;
		m.A = Mathf.Clamp(a, 0f, 1f);
		Modulate = m;
	}

	/// <summary>Erro em pixels do alinhamento do hint com o centro da runa (hitY).</summary>
	public float GetErrorPixelsAt(double nowSec)
	{
		float y = _spawnY + _speed * (float)Math.Max(0, nowSec - _startSec);
		return Mathf.Abs(y - _hitY);
	}

	public void ResolveGoodOrPerfect(bool perfect)
	{
		if (_resolved) return;
		_resolved = true;
		_running = false;

		ApplyColor(HitGreen);

		var tw = CreateTween();

		// pop no item (Sprite2D/TextureRect)
		if (_hintItem != null)
		{
			if (_hintItem is Node2D n2)
			{
				n2.Scale = _hintBaseScale;
				tw.TweenProperty(n2, "scale", _hintBaseScale * PopScale, PopIn);
				tw.TweenProperty(n2, "scale", _hintBaseScale, PopOut);
			}
			else if (_hintItem is Control c)
			{
				c.Scale = _hintBaseScale;
				tw.TweenProperty(c, "scale", _hintBaseScale * PopScale, PopIn);
				tw.TweenProperty(c, "scale", _hintBaseScale, PopOut);
			}
		}

		// partículas no perfect (ou sempre, se quiser)
		if (_particles != null && perfect)
		{
			_particles.Emitting = false;
			_particles.Restart();
			_particles.Emitting = true;
		}

		tw.TweenProperty(this, "modulate:a", 0.0f, 0.10f);
		tw.TweenCallback(Callable.From(QueueFree));
	}

	public void ResolveMiss()
	{
		if (_resolved) return;
		_resolved = true;
		_running = false;

		ApplyColor(MissRed);

		Vector2 basePos = GlobalPosition;
		var tw = CreateTween();

		int steps = 10;
		float stepT = MissShakeDuration / steps;

		for (int i = 0; i < steps; i++)
		{
			float dir = (i % 2 == 0) ? 1f : -1f;
			Vector2 p = basePos + new Vector2(dir * MissShakeStrength, 0);
			tw.TweenProperty(this, "global_position", p, stepT);
		}

		tw.TweenProperty(this, "global_position", basePos, 0.01f);
		tw.TweenProperty(this, "modulate:a", 0.0f, MissFadeOut);
		tw.TweenCallback(Callable.From(QueueFree));
	}
}
