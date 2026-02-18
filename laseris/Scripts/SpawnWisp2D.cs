using Godot;

namespace Game.Vfx
{
	public partial class SpawnWisp2D : Node2D
	{
		[ExportGroup("Refs")]
		[Export] public NodePath SpritePath = "Sprite";
		[Export] public NodePath SpecksPath = "Specks";
		[Export] public NodePath SfxPath = "Sfx";

		[ExportGroup("Timing")]
		[Export] public float FadeInSec = 0.06f;
		[Export] public float HoldSec = 0.08f;
		[Export] public float FadeOutSec = 0.16f;

		[ExportGroup("Motion / Feel")]
		[Export] public float DriftUpPx = 14f;
		[Export] public float PunchScale = 1.12f;
		[Export] public float RotateDeg = 8f;

		private Sprite2D _sprite;
		private GpuParticles2D _specks;
		private AudioStreamPlayer _sfx;

		private Tween _tw;

		public override void _Ready()
		{
			_sprite = GetNodeOrNull<Sprite2D>(SpritePath);
			_specks = GetNodeOrNull<GpuParticles2D>(SpecksPath);
			_sfx = GetNodeOrNull<AudioStreamPlayer>(SfxPath);
		}

		/// <summary>
		/// Assinatura compatível com o Call do controller:
		/// Play(pos, color, scale, sfx)
		/// </summary>
		public void Play(Vector2 atGlobal, Color tint, float scale, AudioStream sfx)
		{
			_tw?.Kill();

			GlobalPosition = atGlobal;
			Rotation = Mathf.DegToRad(RotateDeg);
			Scale = Vector2.One * Mathf.Max(0.01f, scale);

			// aplica tint + começa invisível
			ApplyTint(tint);
			SetAlpha(0f);

			// particles
			if (_specks != null)
			{
				_specks.Emitting = true;
				_specks.OneShot = true;
			}

			// audio (opcional)
			if (_sfx != null && sfx != null)
			{
				_sfx.Stream = sfx;
				_sfx.PitchScale = (float)GD.RandRange(0.95f, 1.05f);
				_sfx.Play();
			}

			Vector2 startPos = atGlobal;
			Vector2 endPos = atGlobal + new Vector2(0f, -Mathf.Abs(DriftUpPx));

			Vector2 baseScale = Scale;
			Vector2 punch = baseScale * Mathf.Max(1.0f, PunchScale);

			_tw = CreateTween();
			_tw.SetEase(Tween.EaseType.Out);

			// Fade in + punch + drift
			_tw.TweenProperty(this, "modulate:a", tint.A, Mathf.Max(0.01f, FadeInSec))
			   .SetTrans(Tween.TransitionType.Quad);

			_tw.Parallel()
			   .TweenProperty(this, "scale", punch, Mathf.Max(0.01f, FadeInSec))
			   .SetTrans(Tween.TransitionType.Back);

			_tw.Parallel()
			   .TweenProperty(this, "global_position", endPos, Mathf.Max(0.01f, FadeInSec + HoldSec + FadeOutSec))
			   .SetTrans(Tween.TransitionType.Sine);

			// Hold
			if (HoldSec > 0f)
				_tw.TweenInterval(HoldSec);

			// Fade out + volta scale
			_tw.TweenProperty(this, "modulate:a", 0f, Mathf.Max(0.01f, FadeOutSec))
			   .SetTrans(Tween.TransitionType.Quad);

			_tw.Parallel()
			   .TweenProperty(this, "scale", baseScale * 0.92f, Mathf.Max(0.01f, FadeOutSec))
			   .SetTrans(Tween.TransitionType.Quad);

			_tw.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(this))
					QueueFree();
			}));
		}

		private void ApplyTint(Color tint)
		{
			if (_sprite != null) _sprite.Modulate = tint;
			if (_specks != null) _specks.Modulate = tint;

			// modulate do root também ajuda caso você não tenha sprite
			Modulate = tint;
		}

		private void SetAlpha(float a)
		{
			var m = Modulate;
			m.A = a;
			Modulate = m;

			if (_sprite != null)
			{
				var s = _sprite.Modulate;
				s.A = a;
				_sprite.Modulate = s;
			}

			if (_specks != null)
			{
				var p = _specks.Modulate;
				p.A = a;
				_specks.Modulate = p;
			}
		}
	}
}
