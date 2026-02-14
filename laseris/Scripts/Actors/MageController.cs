using Godot;
using System;

namespace Game.Characters
{
	public partial class MageController : Node2D
	{
		[Signal] public delegate void HealthChangedEventHandler(int current, int max);
		[Signal] public delegate void DiedEventHandler();

		[Export] public NodePath DefenseSpritePath = "Sprite";

		private AnimatedSprite2D _defendSprite;

		[ExportGroup("Stats")]
		[Export] public int MaxHp = 100;

		[ExportGroup("HP System")]
		[Export] public bool DisableHpLoss = true; // ✅ NOVO: quando true, ApplyDamage NÃO reduz HP

		[ExportGroup("Defense")]
		[Export] public float DefenseGraceSeconds = 0.05f;
		[Export] public bool DebugPrints = true;

		[ExportGroup("Shield Visual")]
		[Export] public NodePath ShieldSpritePath = "Shield"; // AnimatedSprite2D com anims e1..e6
		private Vector2 _shieldBaseScale = Vector2.One;

		[ExportGroup("Shield VFX (Smooth + Pulse)")]
		[Export] public float ShieldFadeIn = 0.08f;
		[Export] public float ShieldFadeOut = 0.10f;
		[Export] public float ShieldScaleInFrom = 0.92f;
		[Export] public float ShieldScaleOutTo = 0.96f;
		[Export] public float ShieldPulseScale = 1.02f;
		[Export] public float ShieldPulseHalfPeriod = 0.14f;

		[ExportGroup("Refs")]
		[Export] public NodePath HitPointPath = "HitPoint"; // Marker2D

		[ExportGroup("VFX")]
		[Export] public PackedScene DamagePopupScene;
		[Export] public NodePath DamagePopupParentPath = "../../Vfx";
		[Export] public Vector2 DamagePopupOffset = new Vector2(0, -48);

		public int Hp { get; private set; }
		public bool IsDead => Hp <= 0;

		public bool IsDefenseWindowActive { get; private set; }
		public bool IsShieldActive { get; private set; }

		public AnimatedSprite2D Sprite { get; private set; }
		public AnimatedSprite2D ShieldSprite { get; private set; }
		public Marker2D WeaponSocket { get; private set; }
		public Marker2D BlockPoint { get; private set; }
		public Marker2D HitPoint { get; private set; }

		private Node2D _damagePopupParent;

		private int _defenseToken = 0;
		private int _holdToken = 0;

		private bool _shieldChosenThisWindow = false;
		private int _shieldElementNow = 0;

		private bool _forceShieldHold = false;

		private Tween _shieldTween;
		private Tween _shieldPulseTween;

		public override void _Ready()
		{
			Sprite = GetNodeOrNull<AnimatedSprite2D>("Sprite");
			ShieldSprite = GetNodeOrNull<AnimatedSprite2D>(ShieldSpritePath);
			_defendSprite = GetNodeOrNull<AnimatedSprite2D>(DefenseSpritePath);

			WeaponSocket = GetNodeOrNull<Marker2D>("WeaponSocket");
			BlockPoint = GetNodeOrNull<Marker2D>("BlockPoint");
			HitPoint = GetNodeOrNull<Marker2D>(HitPointPath);

			_damagePopupParent = GetNodeOrNull<Node2D>(DamagePopupParentPath);

			if (ShieldSprite != null)
				_shieldBaseScale = ShieldSprite.Scale;

			ClearShieldVisual(force: true);

			Hp = MaxHp;
			EmitSignal(SignalName.HealthChanged, Hp, MaxHp);

			PlayIdle();

			if (HitPoint == null)
				GD.PushWarning("MageController: HitPoint não encontrado. Crie um Marker2D chamado 'HitPoint' dentro do Mage (ou ajuste HitPointPath).");
		}

		public void PlayRandomDefendAnim()
		{
			if (_defendSprite == null) return;

			int idx = GD.RandRange(1, 6);
			string anim = $"defend{idx}";

			if (_defendSprite.SpriteFrames != null && _defendSprite.SpriteFrames.HasAnimation(anim))
				_defendSprite.Play(anim);
		}

		public void PlayRandomAttackAnim()
		{
			if (Sprite == null) return;

			int idx = GD.RandRange(1, 6);
			string anim = $"cast{idx}";

			if (Sprite.SpriteFrames != null && Sprite.SpriteFrames.HasAnimation(anim))
				Sprite.Play(anim);
		}

		public Vector2 GetWeaponSocketGlobal() => WeaponSocket != null ? WeaponSocket.GlobalPosition : GlobalPosition;
		public Vector2 GetBlockPointGlobal() => BlockPoint != null ? BlockPoint.GlobalPosition : GlobalPosition;
		public Vector2 GetHitPointGlobal() => HitPoint != null ? HitPoint.GlobalPosition : GlobalPosition;

		public void SetShieldElement(int elementId)
		{
			if (!IsDefenseWindowActive) return;
			if (ShieldSprite == null || ShieldSprite.SpriteFrames == null) return;

			string anim = $"e{elementId}";
			if (!ShieldSprite.SpriteFrames.HasAnimation(anim))
			{
				if (DebugPrints) GD.PushWarning($"[Mage] Shield anim '{anim}' não existe no Shield SpriteFrames.");
				return;
			}

			_shieldChosenThisWindow = true;
			_shieldElementNow = elementId;

			ShowShieldSmooth();

			if (ShieldSprite.Animation != anim || !ShieldSprite.IsPlaying())
				ShieldSprite.Play(anim);
		}

		public void HoldShield(float seconds)
		{
			if (ShieldSprite == null) return;
			if (!_shieldChosenThisWindow) return;

			_forceShieldHold = true;

			ShowShieldSmooth();

			int myToken = ++_holdToken;
			GetTree().CreateTimer(Mathf.Max(0.01f, seconds)).Timeout += () =>
			{
				if (myToken != _holdToken) return;

				_forceShieldHold = false;

				if (!IsDefenseWindowActive)
					ClearShieldVisual(force: true);
			};
		}

		public void ClearShieldVisual(bool force)
		{
			if (!force && _forceShieldHold)
				return;

			_shieldChosenThisWindow = false;
			_shieldElementNow = 0;

			HideShieldSmooth(forceHide: true);
		}

		public void ArmDefenseWindow(double durationSeconds)
		{
			CancelDefenseWindow();

			IsDefenseWindowActive = true;
			IsShieldActive = true;

			_forceShieldHold = false;

			ClearShieldVisual(force: true);

			int myToken = ++_defenseToken;
			GetTree().CreateTimer((float)durationSeconds + DefenseGraceSeconds).Timeout += () =>
			{
				if (myToken != _defenseToken) return;

				IsDefenseWindowActive = false;
				IsShieldActive = false;

				if (!_forceShieldHold)
					ClearShieldVisual(force: true);

				PlayIdle();
			};

			if (DebugPrints)
				GD.Print($"[Mage] Defense window armed for {durationSeconds:0.000}s");
		}

		public void CancelDefenseWindow()
		{
			IsDefenseWindowActive = false;
			IsShieldActive = false;

			_forceShieldHold = false;

			_defenseToken++;
			_holdToken++;

			ClearShieldVisual(force: true);
		}

		public void ApplyDamage(int amount)
		{
			if (IsDead) return;

			int dmg = Mathf.Max(0, amount);

			// ✅ NOVO: modo sem perder HP
			if (!DisableHpLoss)
			{
				Hp = Mathf.Max(0, Hp - dmg);
				EmitSignal(SignalName.HealthChanged, Hp, MaxHp);
			}

			if (DebugPrints)
				GD.Print($"[Mage] Took damage: {dmg}. HP={(DisableHpLoss ? "(no-loss)" : $"{Hp}/{MaxHp}")}");

			if (dmg > 0)
				SpawnTextPopup(dmg.ToString(), Colors.Red, 1.7f);

			PlayIfExists("hit");

			if (!DisableHpLoss && Hp <= 0)
			{
				PlayIfExists("dead");
				EmitSignal(SignalName.Died);
			}
		}

		public void ApplyDamageStyled(int amount, string text, Color color, float scaleMult)
		{
			if (IsDead) return;

			int dmg = Mathf.Max(0, amount);

			if (!DisableHpLoss)
			{
				Hp = Mathf.Max(0, Hp - dmg);
				EmitSignal(SignalName.HealthChanged, Hp, MaxHp);
			}

			if (!string.IsNullOrEmpty(text))
				SpawnTextPopup(text, color, scaleMult);

			PlayIfExists("hit");

			if (!DisableHpLoss && Hp <= 0)
			{
				PlayIfExists("dead");
				EmitSignal(SignalName.Died);
			}
		}

		public void ShowDamagePopupText(string text, Color color, float scaleMult = 1.0f)
		{
			SpawnTextPopup(text, color, scaleMult);
		}

		private void SpawnTextPopup(string text, Color color, float scaleMult)
		{
			if (DamagePopupScene == null || _damagePopupParent == null) return;

			var inst = DamagePopupScene.Instantiate();
			if (inst is not Node2D popupNode) { inst.QueueFree(); return; }

			_damagePopupParent.AddChild(popupNode);
			popupNode.GlobalPosition = GlobalPosition + DamagePopupOffset;

			if (inst.HasMethod("ShowText"))
				inst.Call("ShowText", text, color, scaleMult);
		}

		public void OnDefendSuccess()
		{
			if (DebugPrints) GD.Print("[Mage] Defend SUCCESS");
			PlayDefend();
		}

		public void OnDefendFail()
		{
			if (DebugPrints) GD.Print("[Mage] Defend FAIL");
		}

		public void OnAttackSuccess()
		{
			if (DebugPrints) GD.Print("[Mage] Attack SUCCESS");
			PlayIfExists("attack");
		}

		public void OnAttackFail()
		{
			if (DebugPrints) GD.Print("[Mage] Attack FAIL");
			PlayIdle();
		}

		private void ShowShieldSmooth()
		{
			if (ShieldSprite == null) return;

			ShieldSprite.Visible = true;

			_shieldTween?.Kill();
			_shieldTween = CreateTween();

			var m = ShieldSprite.Modulate;
			ShieldSprite.Modulate = new Color(m.R, m.G, m.B, 0f);
			ShieldSprite.Scale = _shieldBaseScale * ShieldScaleInFrom;

			_shieldTween.TweenProperty(ShieldSprite, "modulate:a", 1.0f, ShieldFadeIn);
			_shieldTween.Parallel().TweenProperty(ShieldSprite, "scale", _shieldBaseScale, ShieldFadeIn);

			StartShieldPulseLoop();
		}

		private void StartShieldPulseLoop()
		{
			if (ShieldSprite == null) return;

			_shieldPulseTween?.Kill();
			_shieldPulseTween = CreateTween();
			_shieldPulseTween.SetLoops();

			_shieldPulseTween.TweenProperty(ShieldSprite, "scale", _shieldBaseScale * ShieldPulseScale, ShieldPulseHalfPeriod);
			_shieldPulseTween.TweenProperty(ShieldSprite, "scale", _shieldBaseScale, ShieldPulseHalfPeriod);
		}

		private void HideShieldSmooth(bool forceHide)
		{
			if (ShieldSprite == null) return;

			_shieldPulseTween?.Kill();
			_shieldPulseTween = null;

			_shieldTween?.Kill();
			_shieldTween = CreateTween();

			_shieldTween.TweenProperty(ShieldSprite, "modulate:a", 0.0f, ShieldFadeOut);
			_shieldTween.Parallel().TweenProperty(ShieldSprite, "scale", _shieldBaseScale * ShieldScaleOutTo, ShieldFadeOut);

			_shieldTween.TweenCallback(Callable.From(() =>
			{
				ShieldSprite.Stop();
				ShieldSprite.Visible = false;

				var m = ShieldSprite.Modulate;
				ShieldSprite.Modulate = new Color(m.R, m.G, m.B, 0f);
				ShieldSprite.Scale = _shieldBaseScale * ShieldScaleInFrom;
			}));
		}

		public void PlayIdle()
		{
			int idx = GD.RandRange(1, 6);
			string anim = $"idle{idx}";
			PlayAny(anim);
		}

		private void PlayDefend()
		{
			if (!PlayAny("defend"))
				PlayAny("block");
		}

		public bool PlayAny(string anim)
		{
			if (Sprite?.SpriteFrames == null) return false;
			if (!Sprite.SpriteFrames.HasAnimation(anim)) return false;
			if (Sprite.Animation == anim && Sprite.IsPlaying()) return true;
			Sprite.Play(anim);
			return true;
		}

		private void PlayIfExists(string anim)
		{
			if (Sprite?.SpriteFrames == null) return;
			if (!Sprite.SpriteFrames.HasAnimation(anim)) return;
			if (Sprite.Animation == anim && Sprite.IsPlaying()) return;
			Sprite.Play(anim);
		}
	}
}
