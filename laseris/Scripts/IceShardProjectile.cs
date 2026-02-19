using Godot;

namespace Game.Vfx
{
	public partial class IceShardProjectile : Node2D
	{
		[Signal] public delegate void ReachedTargetEventHandler(int shardIndex, Vector2 atGlobal);

		[ExportGroup("Refs")]
		[Export] public NodePath AnimPath = "Anim";      // AnimatedSprite2D
		[Export] public NodePath SpritePath = "Sprite";  // fallback (se existir)

		[ExportGroup("Form Animation")]
		[Export] public string FormAnimName = "Form";
		[Export] public bool StopOnLastFrame = true;

		[ExportGroup("Idle Hover (armed)")]
		[Export] public bool EnableHover = true;
		[Export] public float HoverAmpPx = 4f;
		[Export] public float HoverSpeed = 7f;

		private AnimatedSprite2D _anim;
		private Sprite2D _spriteFallback;

		private bool _formed;
		private bool _flying;

		private Vector2 _from, _to;
		private float _t, _dur;
		private int _index;

		private Vector2 _armedBasePos;
		private float _hoverTime;

		// Fire queued (se tentarem disparar antes de formar)
		private bool _pendingFire;
		private Vector2 _pendingTo;
		private float _pendingTravel;
		private float _pendingRotJit;

		// útil pro controller sincronizar wisp lifetime
		public float LastFormDurationSec { get; private set; } = 0.18f;

		public override void _Ready()
		{
			_anim = GetNodeOrNull<AnimatedSprite2D>(AnimPath);
			_spriteFallback = GetNodeOrNull<Sprite2D>(SpritePath);

			// conecta sinal da animação (robusto)
			if (_anim != null)
			{
				if (!_anim.IsConnected(AnimatedSprite2D.SignalName.AnimationFinished, new Callable(this, nameof(OnAnimFinished))))
					_anim.Connect(AnimatedSprite2D.SignalName.AnimationFinished, new Callable(this, nameof(OnAnimFinished)));
			}

			SetProcess(false);
		}

		public void Setup(Texture2D tex, int index)
		{
			_index = index;

			// fallback antigo: se ainda tiver Sprite2D e quiser variar textura estática
			if (_spriteFallback != null && tex != null)
				_spriteFallback.Texture = tex;
		}

		/// <summary>
		/// Spawna no ar e toca a animação "Form". Quando terminar, fica pronto pra disparar.
		/// </summary>
		public void ArmAt(Vector2 globalPos, float startScale)
		{
			_flying = false;
			_formed = false;
			_pendingFire = false;

			GlobalPosition = globalPos;
			_armedBasePos = globalPos;

			Rotation = 0f;
			Scale = Vector2.One * Mathf.Max(0.05f, startScale);

			// garante visível
			SetAlpha(1f);

			// calcula duração da animação (pra sincronizar wisp)
			LastFormDurationSec = EstimateFormDuration();

			// toca animação
			if (_anim != null && _anim.SpriteFrames != null && _anim.SpriteFrames.HasAnimation(FormAnimName))
			{
				_anim.Visible = true;
				_anim.SpeedScale = 1f;
				_anim.Play(FormAnimName);
			}
			else
			{
				// se não tem anim, considera formado instantâneo
				_formed = true;
			}

			SetProcess(true); // hover + possível voo depois
		}

		public void Fire(Vector2 to, float travelSec, float rotJitterRad)
		{
			// se ainda está formando, fila o disparo
			if (!_formed)
			{
				_pendingFire = true;
				_pendingTo = to;
				_pendingTravel = travelSec;
				_pendingRotJit = rotJitterRad;
				return;
			}

			StartFlight(to, travelSec, rotJitterRad);
		}

		private void StartFlight(Vector2 to, float travelSec, float rotJitterRad)
		{
			_from = GlobalPosition;
			_to = to;
			_t = 0f;
			_dur = Mathf.Max(0.01f, travelSec);

			// congela no último frame (shard completo)
			if (_anim != null && StopOnLastFrame && _anim.SpriteFrames != null && _anim.SpriteFrames.HasAnimation(FormAnimName))
			{
				int last = _anim.SpriteFrames.GetFrameCount(FormAnimName) - 1;
				last = Mathf.Max(0, last);
				_anim.Stop();
				_anim.Frame = last;
			}

			float baseRot = (_to - _from).Angle();
			//Rotation = baseRot + rotJitterRad;

			_flying = true;
			EnableHover = false;
		}

		private void OnAnimFinished()
		{
			// quando a animação terminar, marca formado
			_formed = true;

			// se alguém pediu Fire antes, dispara agora
			if (_pendingFire)
			{
				_pendingFire = false;
				StartFlight(_pendingTo, _pendingTravel, _pendingRotJit);
			}
		}

		public override void _Process(double delta)
		{
			float dt = (float)delta;

			// hover enquanto armado (não voando)
			if (!_flying && EnableHover)
			{
				_hoverTime += dt * HoverSpeed;
				float y = Mathf.Sin(_hoverTime) * HoverAmpPx;
				GlobalPosition = _armedBasePos + new Vector2(0f, y);
				return;
			}

			if (!_flying) return;

			_t += dt;
			float u = Mathf.Clamp(_t / _dur, 0f, 1f);

			// ease-out quad
			u = 1f - (1f - u) * (1f - u);

			GlobalPosition = _from.Lerp(_to, u);

			if (_t >= _dur)
			{
				_flying = false;
				SetProcess(false);

				EmitSignal(SignalName.ReachedTarget, _index, GlobalPosition);
				QueueFree();
			}
		}

		private float EstimateFormDuration()
		{
			if (_anim == null || _anim.SpriteFrames == null) return 0.18f;
			if (!_anim.SpriteFrames.HasAnimation(FormAnimName)) return 0.18f;

			int frames = _anim.SpriteFrames.GetFrameCount(FormAnimName);
			float fps = (float)_anim.SpriteFrames.GetAnimationSpeed(FormAnimName);
			fps *= Mathf.Max(0.001f, _anim.SpeedScale);

			if (frames <= 0 || fps <= 0.001f) return 0.18f;
			return Mathf.Max(0.05f, frames / fps);
		}

		private void SetAlpha(float a)
		{
			var m = Modulate;
			m.A = a;
			Modulate = m;
		}
	}
}
