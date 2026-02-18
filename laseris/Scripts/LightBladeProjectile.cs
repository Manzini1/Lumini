using Godot;

namespace Game.Vfx
{
	public partial class LightBladeProjectile : Node2D
	{
		[Signal] public delegate void ReachedTargetEventHandler(int bladeIndex);

		[Export] public NodePath SpritePath = "Sprite";

		[ExportGroup("Presentation (AAA)")]
		[Export] public float SpawnInSec = 0.06f;           // tempo do "pop" inicial
		[Export] public float SpawnScaleFrom = 0.78f;        // escala inicial do pop
		[Export] public float SpawnScaleOvershoot = 1.08f;   // overshoot (Back feel)

		[Export] public float AimRotationOffsetDeg = 0f;     // ajuste fino se seu sprite "aponta" pra outro lado

		[ExportGroup("Exit / Fly-through")]
		[Export] public float ExitDistance = 900f;           // quanto passa do alvo (pra sair da tela)
		[Export] public float ExitTravelSec = 0.32f;         // tempo pra atravessar/sair

		[ExportGroup("Impact SFX")]
		[Export] public NodePath ImpactSfxPath = "ImpactSfx";  // opcional: AudioStreamPlayer2D
		[Export] public AudioStream ImpactSfx;                 // opcional: setar direto no inspector
		[Export] public float ImpactSfxVolumeDb = -6f;

		private Sprite2D _sprite;
		private AudioStreamPlayer2D _impactPlayer;

		private Tween _moveTw;
		private Tween _fxTw;

		public int BladeIndex { get; private set; }

		public override void _Ready()
		{
			_sprite = GetNodeOrNull<Sprite2D>(SpritePath);
			_impactPlayer = GetNodeOrNull<AudioStreamPlayer2D>(ImpactSfxPath);

			// garante estado visual inicial consistente
			if (_sprite != null)
			{
				var m = _sprite.Modulate;
				m.A = 1f;
				_sprite.Modulate = m;
			}
		}

		public void Setup(Texture2D tex, int index)
		{
			BladeIndex = index;
			if (_sprite != null && tex != null)
				_sprite.Texture = tex;
		}

		public void Launch(Vector2 from, Vector2 to, float travelSec, float stickSec, float fadeOutSec)
		{
			_moveTw?.Kill();
			_fxTw?.Kill();

			GlobalPosition = from;

			// direção e rotação (com offset opcional)
			Vector2 d = to - from;
			Vector2 dir = d.Length() > 0.001f ? d.Normalized() : Vector2.Right;

			float aimOffsetRad = Mathf.DegToRad(AimRotationOffsetDeg);
			Rotation = dir.Angle() + aimOffsetRad;

			// ===== Spawn "pop" (paralelo ao movimento) =====
			PlaySpawnPop();

			// ===== Movimento até o alvo =====
			_moveTw = CreateTween();
			_moveTw.SetTrans(Tween.TransitionType.Quad);
			_moveTw.SetEase(Tween.EaseType.Out);

			float tTravel = Mathf.Max(0.01f, travelSec);
			_moveTw.TweenProperty(this, "global_position", to, tTravel);

			_moveTw.TweenCallback(Callable.From(() =>
			{
				if (!GodotObject.IsInstanceValid(this)) return;

				// Impact moment
				PlayImpactSfx();
				EmitSignal(SignalName.ReachedTarget, BladeIndex);

				// opcional: pausa curtinha no alvo antes de atravessar
				float hold = Mathf.Max(0f, stickSec);
				if (hold > 0.001f)
				{
					_moveTw = CreateTween();
					_moveTw.TweenInterval(hold);
					_moveTw.TweenCallback(Callable.From(() =>
					{
						if (!GodotObject.IsInstanceValid(this)) return;
						FlyThroughAndExit(to, dir, fadeOutSec);
					}));
				}
				else
				{
					FlyThroughAndExit(to, dir, fadeOutSec);
				}
			}));
		}

		private void PlaySpawnPop()
		{
			if (_sprite == null || !GodotObject.IsInstanceValid(_sprite))
				return;

			// alpha 0 -> 1 e scale pop (no Sprite ou no root; aqui uso root pra ficar simples)
			Scale = Vector2.One * SpawnScaleFrom;

			var m = _sprite.Modulate;
			m.A = 0f;
			_sprite.Modulate = m;

			_fxTw = CreateTween();
			_fxTw.SetTrans(Tween.TransitionType.Back);
			_fxTw.SetEase(Tween.EaseType.Out);

			float t = Mathf.Max(0.01f, SpawnInSec);

			// overshoot rápido e volta pra 1
			_fxTw.TweenProperty(this, "scale", Vector2.One * SpawnScaleOvershoot, t * 0.65f);
			_fxTw.TweenProperty(this, "scale", Vector2.One, t * 0.35f);

			// alpha em paralelo (um tween separado pra não depender de SetParallel)
			var twA = CreateTween();
			twA.SetTrans(Tween.TransitionType.Quad);
			twA.SetEase(Tween.EaseType.Out);
			twA.TweenProperty(_sprite, "modulate:a", 1f, t);
		}

		private void FlyThroughAndExit(Vector2 impactPos, Vector2 dir, float fadeOutSec)
		{
			_moveTw?.Kill();

			Vector2 exit = impactPos + dir * Mathf.Max(50f, ExitDistance);

			// move até "sair"
			_moveTw = CreateTween();
			_moveTw.SetTrans(Tween.TransitionType.Quad);
			_moveTw.SetEase(Tween.EaseType.In);

			float tExit = Mathf.Max(0.05f, ExitTravelSec);
			_moveTw.TweenProperty(this, "global_position", exit, tExit);

			// fade out durante a saída (pode ser mais curto/mais longo que tExit)
			if (_sprite != null && GodotObject.IsInstanceValid(_sprite))
			{
				float tFade = Mathf.Max(0.05f, fadeOutSec);

				var fadeTw = CreateTween();
				fadeTw.SetTrans(Tween.TransitionType.Quad);
				fadeTw.SetEase(Tween.EaseType.In);
				fadeTw.TweenProperty(_sprite, "modulate:a", 0f, tFade);
			}

			_moveTw.TweenCallback(Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(this))
					QueueFree();
			}));
		}

		private void PlayImpactSfx()
		{
			// prioridade: player da cena -> export ImpactSfx
			if (_impactPlayer != null && GodotObject.IsInstanceValid(_impactPlayer))
			{
				if (_impactPlayer.Stream == null && ImpactSfx != null)
					_impactPlayer.Stream = ImpactSfx;

				_impactPlayer.VolumeDb = ImpactSfxVolumeDb;

				if (_impactPlayer.Stream != null)
					_impactPlayer.Play();

				return;
			}

			// fallback: cria um player rápido se tiver ImpactSfx setado
			if (ImpactSfx == null) return;

			var p = new AudioStreamPlayer2D();
			p.Stream = ImpactSfx;
			p.VolumeDb = ImpactSfxVolumeDb;
			AddChild(p);
			p.Play();

			p.Finished += () =>
			{
				if (GodotObject.IsInstanceValid(p))
					p.QueueFree();
			};
		}
	}
}
