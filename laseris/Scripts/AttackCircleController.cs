using Godot;

namespace Game.Combat
{
	public partial class AttackCircleController : Node2D, IElementIndicator
	{
		[ExportGroup("Refs")]
		[Export] public NodePath IconPath = "Icon";           // AnimatedSprite2D (anims e1..e7)
		[Export] public NodePath ParticlesPath = "Particles"; // GpuParticles2D

		[ExportGroup("Rotation")]
		[Export] public float RotateEverySeconds = 0.35f;

		[ExportGroup("Particles per element (optional)")]
		// index 0 ignorado; 1..7
		[Export] public ParticleProcessMaterial[] ParticlesMatByElementId = new ParticleProcessMaterial[8];
		[Export] public Texture2D[] ParticlesTexByElementId = new Texture2D[8];

		[ExportGroup("Sizing (optional)")]
		[Export] public Vector2 IndicatorSize = new(140, 70);

		[ExportGroup("Tuning")]
		[Export] public bool ParticlesEnabled = true;
		[Export] public float ParticlesScale = 1.0f;

		[ExportGroup("Debug")]
		[Export] public bool DebugLogs = false;

		private AnimatedSprite2D _icon;
		private GpuParticles2D _particles;

		private bool _running;
		private double _lastStepSec;

		public int CurrentElementId { get; private set; } = 1;

		public override void _Ready()
		{
			_icon = GetNodeOrNull<AnimatedSprite2D>(IconPath);
			_particles = GetNodeOrNull<GpuParticles2D>(ParticlesPath);

			Visible = false;

			SetSize(IndicatorSize);
			SetElement(1);
			SetEnabled(false);
		}

		// -------- IElementIndicator --------
		public void SetEnabled(bool enabled)
		{
			Visible = enabled;
			if (_particles != null)
				_particles.Emitting = enabled && ParticlesEnabled;
		}

		public void SetSize(Vector2 size)
		{
			IndicatorSize = size;

			// Se tiver ParticleProcessMaterial, ajusta o “box” de emissão
			if (_particles != null && _particles.ProcessMaterial is ParticleProcessMaterial ppm)
			{
				ppm.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box;
				ppm.EmissionBoxExtents = new Vector3(
					Mathf.Max(1f, size.X * 0.5f),
					Mathf.Max(1f, size.Y * 0.5f),
					0f
				);
			}
			else
			{
				// fallback: escala visual
				Scale = new Vector2(
					Mathf.Max(0.01f, size.X / 140f),
					Mathf.Max(0.01f, size.Y / 70f)
				);
			}
		}

		public void SetElement(int elementId)
		{
			CurrentElementId = Mathf.Clamp(elementId, 1, 7);

			// ícone
			if (_icon?.SpriteFrames != null)
			{
				string anim = $"e{CurrentElementId}";
				if (_icon.SpriteFrames.HasAnimation(anim))
					_icon.Play(anim);
			}

			ApplyParticlesForElement(CurrentElementId);
		}

		// -------- Legado (se você quiser manter Start/Stop/UpdateNow) --------
		public void Start(double nowSec)
		{
			_running = true;
			_lastStepSec = nowSec;
			SetEnabled(true);

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
			SetEnabled(false);

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
				if (next > 7) next = 1;

				SetElement(next);
			}
		}

		private void ApplyParticlesForElement(int elementId)
		{
			if (_particles == null) return;

			if (!ParticlesEnabled)
			{
				_particles.Emitting = false;
				return;
			}

			// troca material (opcional)
			if (ParticlesMatByElementId != null && elementId < ParticlesMatByElementId.Length)
			{
				var mat = ParticlesMatByElementId[elementId];
				if (mat != null) _particles.ProcessMaterial = mat;
			}

			// troca textura (opcional)
			if (ParticlesTexByElementId != null && elementId < ParticlesTexByElementId.Length)
			{
				var tex = ParticlesTexByElementId[elementId];
				if (tex != null) _particles.Texture = tex;
			}

			_particles.Emitting = true;
			_particles.Restart();
		}
	}
}
