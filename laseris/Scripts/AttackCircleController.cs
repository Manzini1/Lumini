using Godot;
using System;

namespace Game.Combat;

public partial class AttackCircleController : Node2D
{
	[ExportGroup("Refs")]
	[Export] public NodePath IconPath = "Icon";                // AnimatedSprite2D com anims e1..e6
	[Export] public NodePath ParticlesPath = "Particles";      // GPUParticles2D (sparks/aura)

	[ExportGroup("Rotation")]
	[Export] public float RotateEverySeconds = 0.35f;

	[ExportGroup("Particles per element")]
	// index 0 ignorado; 1..6
	[Export] public ParticleProcessMaterial[] ParticlesMatByElementId = new ParticleProcessMaterial[7];

	// se quiser variar a textura também (opcional)
	[Export] public Texture2D[] ParticlesTexByElementId = new Texture2D[7];

	[ExportGroup("Tuning")]
	[Export] public bool ParticlesEnabled = true;
	[Export] public float ParticlesScale = 1.0f;  // multiplicador visual

	[ExportGroup("Debug")]
	[Export] public bool DebugLogs = false;

	private AnimatedSprite2D _icon;
	private GpuParticles2D _particles;

	private bool _running;
	private double _lastStepSec;

	public int CurrentElementId { get;  set; } = 1;

	public override void _Ready()
	{
		_icon = GetNodeOrNull<AnimatedSprite2D>(IconPath);
		_particles = GetNodeOrNull<GpuParticles2D>(ParticlesPath);

		Visible = false;
		SetElement(1);
		ApplyParticlesForElement(1);
	}

	public void Start(double nowSec)
	{
		_running = true;
		_lastStepSec = nowSec;
		Visible = true;

		// opcional: começa random
		SetElement((int)GD.RandRange(1, 7));

		if (_particles != null)
		{
			_particles.Scale = Vector2.One * ParticlesScale;
			_particles.Emitting = ParticlesEnabled;
			_particles.Restart();
		}

		if (DebugLogs) GD.Print($"[AttackCircle] Start now={nowSec:0.000} elem={CurrentElementId}");
	}

	public void Stop()
	{
		_running = false;
		Visible = false;

		if (_particles != null)
			_particles.Emitting = false;
	}

	public void UpdateNow(double nowSec)
	{
		if (!_running) return;

		double elapsed = nowSec - _lastStepSec;
		if (elapsed >= RotateEverySeconds)
		{
			_lastStepSec = nowSec;

			int next = CurrentElementId + 1;
			if (next > 6) next = 1;

			SetElement(next);
		}
	}

	public void SetElement(int elementId)
	{
		CurrentElementId = Mathf.Clamp(elementId, 1, 7);

		// icon anim
		if (_icon?.SpriteFrames != null)
		{
			string anim = $"e{CurrentElementId}";
			if (_icon.SpriteFrames.HasAnimation(anim))
				_icon.Play(anim);
		}

		ApplyParticlesForElement(CurrentElementId);
	}

	private void ApplyParticlesForElement(int elementId)
	{
		if (_particles == null) return;

		if (!ParticlesEnabled)
		{
			_particles.Emitting = false;
			return;
		}

		// troca material
		if (ParticlesMatByElementId != null && elementId < ParticlesMatByElementId.Length)
		{
			var mat = ParticlesMatByElementId[elementId];
			if (mat != null)
				_particles.ProcessMaterial = mat;
		}

		// troca textura (opcional)
		if (ParticlesTexByElementId != null && elementId < ParticlesTexByElementId.Length)
		{
			var tex = ParticlesTexByElementId[elementId];
			if (tex != null)
				_particles.Texture = tex;
		}

		_particles.Emitting = true;
		_particles.Restart();
	}
}
