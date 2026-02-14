using Godot;
using System;

namespace Game.Combat
{
	public partial class ElementAuraController : Node2D, IElementIndicator
	{
		[ExportGroup("Refs")]
		[Export] public NodePath BodyPath = "Body";     // Sprite2D OU AnimatedSprite2D
		[Export] public NodePath DropsPath = "Drops";   // GPUParticles2D
		[Export] public NodePath WispsPath = "Wisps";   // GPUParticles2D (opcional)

		[ExportGroup("Follow")]
		[Export] public Vector2 AuraOffset = new Vector2(0, -18);
		[Export(PropertyHint.Range, "0,60,0.1")]
		public float FollowLerpSpeed = 20f; // 0 = instant

		[ExportGroup("Color Transition (no Tween)")]
		[Export(PropertyHint.Range, "0.01,1.0,0.01")]
		public float ColorTransitionSeconds = 0.14f;

		[Export(PropertyHint.Range, "0.01,0.5,0.01")]
		public float FlashSeconds = 0.08f;

		[Export(PropertyHint.Range, "0.0,1.0,0.01")]
		public float FlashStrength = 0.65f;

		[Export(PropertyHint.Range, "0.0,3.0,0.01")]
		public float FlashIntensityBoost = 0.35f;

		[ExportGroup("Tuning")]
		[Export(PropertyHint.Range, "0.0,3.0,0.01")]
		public float BodyIntensityBase = 1.0f;

		[Export(PropertyHint.Range, "0.0,2.0,0.01")]
		public float BodyAlphaMult = 1.0f;

		[Export(PropertyHint.Range, "0.0,3.0,0.01")]
		public float ParticlesAlphaMult = 1.0f;

		[ExportGroup("Size")]
		[Export] public Vector2 AuraSize = new Vector2(96, 48);

		[ExportGroup("AnimatedSprite2D")]
		[Export] public bool AutoPlayAnimationOnElementChange = true;

		// Se faltar animação do elemento, cai aqui.
		[Export] public string FallbackAnimationName = "fire";

		// Para trevas, você pode ter "shadow" ou "darkness" no SpriteFrames.
		[Export] public string ShadowAnimationName = "shadow";
		[Export] public string DarknessAnimationName = "darkness";

		[ExportGroup("Debug")]
		[Export] public bool DebugWarnings = true;

		private Sprite2D _bodySprite;
		private AnimatedSprite2D _bodyAnim;

		private GpuParticles2D _drops;
		private GpuParticles2D _wisps;

		public int CurrentElementId { get; private set; } = 1;

		private bool _enabled = true;

		private Vector2 _targetGlobal;
		private bool _hasTarget;

		private Color _fromColor = Colors.White;
		private Color _toColor = Colors.White;
		private float _tColor = 1f;

		private float _tFlash = 1f;

		public override void _Ready()
		{
			var bodyNode = GetNodeOrNull<Node>(BodyPath);
			_bodySprite = bodyNode as Sprite2D;
			_bodyAnim = bodyNode as AnimatedSprite2D;

			if (_bodySprite == null && _bodyAnim == null && DebugWarnings)
				GD.PushWarning("[ElementAuraController] Body precisa ser Sprite2D ou AnimatedSprite2D.");

			_drops = GetNodeOrNull<GpuParticles2D>(DropsPath);
			_wisps = GetNodeOrNull<GpuParticles2D>(WispsPath);

			DuplicateBodyMaterial();

			SetProcess(true);

			_fromColor = ElementToColor(CurrentElementId);
			_toColor = _fromColor;
			_tColor = 1f;
			_tFlash = 1f;

			ApplySizeNow();
			ApplyVisualsImmediate(_toColor);

			ValidateParticles(_drops, "Drops");
			ValidateParticles(_wisps, "Wisps");

			SetEnabled(true);

			if (_bodyAnim != null && AutoPlayAnimationOnElementChange)
				PlayElementAnimation(CurrentElementId, restart: true);
		}

		public override void _Process(double delta)
		{
			float dt = (float)delta;

			// follow suave + offset
			if (_hasTarget)
			{
				Vector2 desired = _targetGlobal + AuraOffset;

				if (FollowLerpSpeed <= 0.01f)
					GlobalPosition = desired;
				else
				{
					float k = 1f - Mathf.Exp(-FollowLerpSpeed * dt);
					GlobalPosition = GlobalPosition.Lerp(desired, k);
				}
			}

			// cor (blend)
			if (_tColor < 1f)
				_tColor = Mathf.Min(1f, _tColor + dt / Mathf.Max(0.01f, ColorTransitionSeconds));

			// flash
			if (_tFlash < 1f)
				_tFlash = Mathf.Min(1f, _tFlash + dt / Mathf.Max(0.01f, FlashSeconds));

			float blend = Smooth01(_tColor);

			Color baseC = _fromColor.Lerp(_toColor, blend);

			float flashBell = 0f;
			if (_tFlash < 1f)
				flashBell = Mathf.Sin(Mathf.Pi * Smooth01(_tFlash));

			Color flashC = _fromColor.Lerp(_toColor, 0.5f);

			Color finalC = baseC.Lerp(flashC, FlashStrength * flashBell);
			finalC.A = Mathf.Clamp(finalC.A * BodyAlphaMult, 0f, 1f);

			ApplyVisuals(finalC, flashBell);
		}

		// -------- IElementIndicator --------

		public void SetEnabled(bool enabled)
		{
			_enabled = enabled;
			Visible = enabled;

			if (_drops != null) _drops.Emitting = enabled;
			if (_wisps != null) _wisps.Emitting = enabled;

			if (enabled)
			{
				_drops?.Restart();
				_wisps?.Restart();

				if (_bodyAnim != null && AutoPlayAnimationOnElementChange)
				{
					if (!_bodyAnim.IsPlaying())
						_bodyAnim.Play();
				}
			}
			else
			{
				if (_bodyAnim != null)
					_bodyAnim.Stop();
			}
		}

		public void SetElement(int elementId)
		{
			elementId = Mathf.Clamp(elementId, 1, 7);
			if (elementId == CurrentElementId && _tColor >= 1f) return;

			int prev = CurrentElementId;
			CurrentElementId = elementId;

			if (_bodyAnim != null && AutoPlayAnimationOnElementChange)
			{
				bool restart = (elementId != prev);
				PlayElementAnimation(elementId, restart);
			}

			_fromColor = (_tColor >= 1f) ? _toColor : _fromColor.Lerp(_toColor, Smooth01(_tColor));
			_toColor = ElementToColor(elementId);

			_tColor = 0f;
			_tFlash = 0f;

			if (_enabled)
			{
				_drops?.Restart();
				_wisps?.Restart();
			}
		}

		public void SetSize(Vector2 size)
		{
			AuraSize = new Vector2(Mathf.Max(1, size.X), Mathf.Max(1, size.Y));
			ApplySizeNow();
		}

		public void SetTargetGlobal(Vector2 globalPos)
		{
			_targetGlobal = globalPos;
			_hasTarget = true;
		}

		// -------- Animation mapping --------

		private void PlayElementAnimation(int elementId, bool restart)
		{
			if (_bodyAnim == null) return;
			if (_bodyAnim.SpriteFrames == null) return;

			string anim = ResolveAnimNameForElement(elementId);

			// se não existir (mesmo após resolver), tenta fallback
			if (!_bodyAnim.SpriteFrames.HasAnimation(anim))
			{
				if (DebugWarnings)
					GD.PushWarning($"[ElementAuraController] SpriteFrames não tem animação '{anim}'. Usando fallback '{FallbackAnimationName}'.");
				anim = FallbackAnimationName;
			}

			if (!_bodyAnim.SpriteFrames.HasAnimation(anim))
			{
				if (DebugWarnings)
					GD.PushWarning($"[ElementAuraController] SpriteFrames não tem fallback '{FallbackAnimationName}'. Nenhuma animação tocada.");
				return;
			}

			if (_bodyAnim.Animation != anim)
				_bodyAnim.Animation = anim;

			if (restart)
				_bodyAnim.Frame = 0;

			if (_enabled)
				_bodyAnim.Play();
		}

		private string ResolveAnimNameForElement(int e)
		{
			// 1..7, onde 7 = trevas
			switch (e)
			{
				case 1: return "fire";
				case 2: return "water";
				case 3: return "earth";
				case 4: return "wind";
				case 5: return "lightning";
				case 6: return "light";
				case 7:
					// aceita shadow ou darkness (o que existir)
					if (_bodyAnim?.SpriteFrames != null)
					{
						if (_bodyAnim.SpriteFrames.HasAnimation(ShadowAnimationName)) return ShadowAnimationName;
						if (_bodyAnim.SpriteFrames.HasAnimation(DarknessAnimationName)) return DarknessAnimationName;
					}
					// se não tiver frames ainda, devolve "shadow" por padrão
					return ShadowAnimationName;
				default:
					return FallbackAnimationName;
			}
		}

		// -------- Internals --------

		private void DuplicateBodyMaterial()
		{
			CanvasItem ci = (CanvasItem)_bodySprite ?? _bodyAnim;
			if (ci == null) return;

			if (ci.Material is ShaderMaterial sm)
				ci.Material = (ShaderMaterial)sm.Duplicate(true);
		}

		private void ApplySizeNow()
		{
			var texSize = GetBodyTextureSize();
			if (texSize.X > 0 && texSize.Y > 0)
			{
				Vector2 scale = new Vector2(AuraSize.X / texSize.X, AuraSize.Y / texSize.Y);

				if (_bodySprite != null) _bodySprite.Scale = scale;
				if (_bodyAnim != null) _bodyAnim.Scale = scale;
			}

			ApplyEmissionBox(_drops);
			ApplyEmissionBox(_wisps);
		}

		private Vector2 GetBodyTextureSize()
		{
			if (_bodySprite?.Texture != null)
				return _bodySprite.Texture.GetSize();

			if (_bodyAnim?.SpriteFrames != null)
			{
				string anim = _bodyAnim.Animation;
				int frame = _bodyAnim.Frame;

				if (string.IsNullOrEmpty(anim))
					anim = FallbackAnimationName;

				if (_bodyAnim.SpriteFrames.HasAnimation(anim))
				{
					var t = _bodyAnim.SpriteFrames.GetFrameTexture(anim, frame);
					if (t != null) return t.GetSize();

					t = _bodyAnim.SpriteFrames.GetFrameTexture(anim, 0);
					if (t != null) return t.GetSize();
				}
			}

			return Vector2.Zero;
		}

		private void ApplyEmissionBox(GpuParticles2D p)
		{
			if (p == null) return;
			if (p.ProcessMaterial is not ParticleProcessMaterial ppm) return;

			ppm.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box;
			ppm.EmissionBoxExtents = new Vector3(AuraSize.X * 0.5f, AuraSize.Y * 0.5f, 0f);
		}

		private void ApplyVisuals(Color c, float flashBell)
		{
			CanvasItem body = (CanvasItem)_bodySprite ?? _bodyAnim;

			if (body?.Material is ShaderMaterial sm)
			{
				sm.SetShaderParameter("aura_color", c);
				sm.SetShaderParameter("aura_intensity", BodyIntensityBase + FlashIntensityBoost * flashBell);
			}
			else if (body != null)
			{
				body.Modulate = c;
			}

			ApplyParticlesColor(_drops, c);
			ApplyParticlesColor(_wisps, c);

			SetParticlesAlpha(_drops, c.A);
			SetParticlesAlpha(_wisps, c.A);
		}

		private void ApplyVisualsImmediate(Color c)
		{
			_tColor = 1f;
			_tFlash = 1f;
			ApplyVisuals(c, 0f);
		}

		private void ApplyParticlesColor(GpuParticles2D p, Color c)
		{
			if (p == null) return;

			if (p.ProcessMaterial is ParticleProcessMaterial ppm)
				ppm.Color = new Color(c.R, c.G, c.B, 1f);
		}

		private void SetParticlesAlpha(GpuParticles2D p, float a)
		{
			if (p == null) return;
			float aa = Mathf.Clamp(a * ParticlesAlphaMult, 0f, 1f);
			var m = p.Modulate;
			p.Modulate = new Color(m.R, m.G, m.B, aa);
		}

		private void ValidateParticles(GpuParticles2D p, string name)
		{
			if (!DebugWarnings || p == null) return;

			if (p.Texture == null)
				GD.PushWarning($"[ElementAuraController] {name}: Texture está null (partículas não vão aparecer).");

			if (p.ProcessMaterial == null)
				GD.PushWarning($"[ElementAuraController] {name}: ProcessMaterial está null (precisa de ParticleProcessMaterial).");
		}

		private static float Smooth01(float t)
		{
			t = Mathf.Clamp(t, 0f, 1f);
			return t * t * (3f - 2f * t);
		}

		private Color ElementToColor(int e)
		{
			return e switch
			{
				1 => new Color(1.00f, 0.35f, 0.23f, 0.95f), // fire
				2 => new Color(0.25f, 0.65f, 1.00f, 0.95f), // water
				3 => new Color(0.55f, 0.38f, 0.22f, 0.95f), // earth
				4 => new Color(0.60f, 1.00f, 0.75f, 0.90f), // wind
				5 => new Color(0.75f, 0.35f, 1.00f, 0.95f), // lightning
				6 => new Color(1.00f, 1.00f, 0.65f, 0.90f), // light
				7 => new Color(0.40f, 0.22f, 0.80f, 0.90f), // shadow/darkness
				_ => new Color(1, 1, 1, 0.9f)
			};
		}
	}
}
