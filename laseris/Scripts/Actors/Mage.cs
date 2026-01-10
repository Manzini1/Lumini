using Godot;
using System;

public partial class Mage : CharacterBody2D
{
	// =========================================================
	// Refs
	// =========================================================
	[ExportCategory("Refs")]
	[Export] public NodePath VisualPath = "Sprite";                 // opcional
	[Export] public NodePath AnimPath = "Sprite";                   // AnimatedSprite2D (p/ flip)
	[Export] public NodePath AnimPlayerPath = "AnimationPlayer";    // AnimationPlayer
	[Export] public NodePath TargetControllerPath;                  // p/ pegar alvo atual
	[Export] public NodePath ElementControllerPath;                 // ✅ casting/cast via eventos

	[ExportCategory("Weapon Refs")]
	[Export] public NodePath WeaponSocketPath = "WeaponSocket";                       // Node2D
	[Export] public NodePath WeaponInHandPath = "WeaponSocket/Weapon";                // Node2D
	[Export] public NodePath ThrownWeaponRootPath = "WeaponSocket/ThrownWeaponsRoot"; // Node2D onde instancia
	[Export] public PackedScene ThrownWeaponScene;                                     // ThrownWeapon.tscn

	[ExportCategory("Weapon Visual")]
	[Export] public float ThrownWeaponScale = 1.0f;

	[ExportCategory("Damage")]
	[Export] public int PhysDamage = 100;

	[ExportCategory("Anim Names")]
	[Export] public string IdleAnim = "idle";
	[Export] public string CastingAnim = "casting";
	[Export] public string CastAnim = "cast";
	[Export] public string PhysAttackAnim = "throw";

	[ExportCategory("Facing")]
	[Export] public bool FaceRightIsDefault = true;
	[Export] public bool UseFlipH = true;

	// =========================================================
	// Pressure / Stun (novo)
	// =========================================================
	[ExportCategory("Pressure")]
	[Export] public float PressureMax = 100f;
	[Export] public float PressureDecayPerSecond = 0f; // 0 = não decai
	[Export] public float StunSeconds = 2.0f;
	[Export] public float IncomingDamageMultiplierWhileStunned = 1.6f;

	public float Pressure { get; private set; } = 0f;
	public bool IsStunned { get; private set; } = false;

	/// <summary>Multiplicador aplicado ao dano recebido enquanto stunado.</summary>
	public float IncomingDamageMultiplier => IsStunned ? IncomingDamageMultiplierWhileStunned : 1f;

	/// <summary>Dispara quando pressure muda (pressure, max).</summary>
	public event Action<float, float> PressureChanged;

	/// <summary>Dispara quando stun muda (true/false).</summary>
	public event Action<bool> StunChanged;

	/// <summary>Dispara quando a Mage tomou hit (dano FINAL aplicado).</summary>
	public event Action<int> TookHit;

	private bool _stunRunning = false;
	[ExportCategory("Stun Anim (SpriteFrames)")]
	[Export] public string StunSlapAnim = "stun_slap";
	[Export] public string StunLoopAnim = "stun";
	// =========================================================
	// Runtime refs
	// =========================================================
	private Node2D _visual;
	private AnimatedSprite2D _anim;
	private AnimationPlayer _animPlayer;
	private TargetController _targetController;
	private ElementController _elementController;

	private Node2D _weaponSocket;
	private Node2D _weaponInHand;
	private Node2D _thrownRoot;

	// =========================================================
	// State
	// =========================================================
	private bool _hasActiveRunes;
	private bool _playingCastOneShot;
	private bool _playingPhysOneShot;

	// Weapon lock
	private bool _weaponInFlight;
	public bool WeaponInFlight => _weaponInFlight;
	public event Action<bool> WeaponFlightChanged;

	// Pending throw target
	private Vector2 _pendingThrowTarget;
	private Enemy _pendingThrowEnemy;
	
	// =========================================================
	// Godot
	// =========================================================
	public override void _Ready()
	{
		_visual = GetNodeOrNull<Node2D>(VisualPath);
		_anim = GetNodeOrNull<AnimatedSprite2D>(AnimPath);
		_animPlayer = GetNodeOrNull<AnimationPlayer>(AnimPlayerPath);

		_targetController = GetNodeOrNull<TargetController>(TargetControllerPath);
		_elementController = GetNodeOrNull<ElementController>(ElementControllerPath);

		_weaponSocket = GetNodeOrNull<Node2D>(WeaponSocketPath);
		_weaponInHand = GetNodeOrNull<Node2D>(WeaponInHandPath);
		_thrownRoot = GetNodeOrNull<Node2D>(ThrownWeaponRootPath);

		if (_animPlayer == null) GD.PushWarning("Mage: não achei AnimationPlayer (AnimPlayerPath).");
		if (_anim == null) GD.PushWarning("Mage: não achei AnimatedSprite2D (AnimPath) - flip pode não funcionar.");
		if (_weaponSocket == null) GD.PushWarning("Mage: WeaponSocketPath não encontrado.");
		if (_weaponInHand == null) GD.PushWarning("Mage: WeaponInHandPath não encontrado (arma na mão).");
		if (_thrownRoot == null) GD.PushWarning("Mage: ThrownWeaponRootPath não encontrado (onde instanciar).");
		if (ThrownWeaponScene == null) GD.PushWarning("Mage: ThrownWeaponScene não setado.");
		if (_elementController == null && !ElementControllerPath.IsEmpty) GD.PushWarning("Mage: ElementControllerPath setado mas não encontrei ElementController.");

		if (_animPlayer != null)
			_animPlayer.AnimationFinished += OnAnimFinished;

		// liga eventos pra casting/cast funcionar
		if (_elementController != null)
		{
			_elementController.ElementActivated += OnElementActivated;
			_elementController.ElementsCleared += OnElementsCleared;
			_elementController.CastStarted += OnCastStarted;
			_elementController.CastResolved += OnCastResolved;
		}

		PlayIdle();
	}

	public override void _ExitTree()
	{
		if (_animPlayer != null)
			_animPlayer.AnimationFinished -= OnAnimFinished;

		if (_elementController != null)
		{
			_elementController.ElementActivated -= OnElementActivated;
			_elementController.ElementsCleared -= OnElementsCleared;
			_elementController.CastStarted -= OnCastStarted;
			_elementController.CastResolved -= OnCastResolved;
		}
	}

	public override void _Process(double delta)
	{
		// Decay opcional
		if (PressureDecayPerSecond > 0f && !IsStunned && Pressure > 0f)
		{
			float dt = (float)delta;
			float old = Pressure;
			Pressure = Mathf.Max(0f, Pressure - PressureDecayPerSecond * dt);

			if (!Mathf.IsEqualApprox(old, Pressure))
				PressureChanged?.Invoke(Pressure, PressureMax);
		}
	}

	// =========================================================
	// INPUT (throw)
	// =========================================================
	public override void _UnhandledInput(InputEvent e)
	{
		if (e.IsActionPressed("debug_hit"))
		ApplyDamage(10);
		if (!e.IsActionPressed("physattack"))
			return;
		
		if (IsStunned) return;
		if (_weaponInFlight) return;

		var (enemy, pos) = ResolveThrowTarget();
		_pendingThrowEnemy = enemy;
		_pendingThrowTarget = pos;

		FaceWorldPosition(pos);
		PlayPhysAttackOnce(); // ReleaseThrow via call method track
	}

	private (Enemy enemy, Vector2 pos) ResolveThrowTarget()
	{
		var t = _targetController?.CurrentTarget;
		if (t != null && GodotObject.IsInstanceValid(t))
		{
			var m = t.GetNodeOrNull<Marker2D>("VfxCenter");
			return (t, m != null ? m.GlobalPosition : t.GlobalPosition);
		}

		return (null, GetGlobalMousePosition());
	}

	// =========================================================
	// SPELL EVENTS (casting/cast)
	// =========================================================
	private void OnElementActivated(ElementType _)
	{
		_hasActiveRunes = true;
		if (_playingCastOneShot || _playingPhysOneShot) return;
		PlayCasting();
	}

	private void OnElementsCleared()
	{
		_hasActiveRunes = false;
		if (_playingCastOneShot || _playingPhysOneShot) return;
		PlayIdle();
	}

	private void OnCastStarted()
	{
		// se estiver stunado, não deixa “travar” estado
		if (IsStunned) return;

		_playingCastOneShot = true;
		PlayCastOnce();
	}

	private void OnCastResolved(CastOutcome outcome, SpellDefinition spell, Enemy target)
	{
		// aqui você pode acoplar feedback visual depois (shake, flash, etc)
	}

	// =========================================================
	// CALL METHOD TRACK (throw)
	// =========================================================
	private DamagePopupManager GetDamagePopupManager()
	{
		return GetTree().GetFirstNodeInGroup("damage_popup_manager") as DamagePopupManager;
	}

	// Chamado via Call Method track na animação "throw"
	public void ReleaseThrow()
	{
		if (_weaponSocket == null)
		{
			GD.PushWarning("[Mage] ReleaseThrow sem WeaponSocket.");
			return;
		}

		Vector2 start = _weaponSocket.GlobalPosition;
		Vector2 target = _pendingThrowTarget;

		GD.Print($"[Mage] ReleaseThrow from={start} to={target}");

		Enemy hitEnemy = _pendingThrowEnemy; // captura local
		int dmg = PhysDamage;

		_weaponInHand?.SetDeferred("visible", false);

		if (ThrownWeaponScene == null || _thrownRoot == null)
		{
			GD.PushWarning("[Mage] Não dá pra instanciar throw (scene/root faltando).");
			_weaponInHand?.SetDeferred("visible", true);
			SetWeaponInFlight(false);
			return;
		}

		SetWeaponInFlight(true);

		var thrown = ThrownWeaponScene.Instantiate<ThrownWeapon>();
		_thrownRoot.AddChild(thrown);

		thrown.GlobalPosition = start;
		thrown.Scale = Vector2.One * ThrownWeaponScale;
		thrown.ZIndex = 999;
		thrown.Visible = true;

		// aplica dano no hit
		thrown.Hit += () =>
		{
			if (hitEnemy == null || !GodotObject.IsInstanceValid(hitEnemy))
				return;

			hitEnemy.TakeDamage(dmg);

			// ⚠️ Se você ainda não criou ShowPhysicalDamage, comenta essa linha por enquanto
			GetDamagePopupManager()?.ShowPhysicalDamage(hitEnemy, dmg);

			GD.Print($"[Mage] HIT phys -> {hitEnemy.Name} -{dmg}");
		};

		// quando volta: destrava e mostra arma
		thrown.Finished += success =>
		{
			_weaponInHand?.SetDeferred("visible", true);
			SetWeaponInFlight(false);
		};

		// sua assinatura atual: Launch(socket, start, target)
		thrown.Launch(_weaponSocket, start, target);
	}

	private void SetWeaponInFlight(bool v)
	{
		if (_weaponInFlight == v) return;
		_weaponInFlight = v;
		WeaponFlightChanged?.Invoke(_weaponInFlight);
	}

	// =========================================================
	// ANIM FINISH
	// =========================================================
	private void OnAnimFinished(StringName animName)
	{
		// terminou cast spell
		if (animName == CastAnim)
		{
			_playingCastOneShot = false;
			ReturnToState();
			return;
		}

		// terminou throw
		if (animName == PhysAttackAnim)
		{
			_playingPhysOneShot = false;
			ReturnToState();
			return;
		}
	}

	private void ReturnToState()
	{
		if (_playingCastOneShot || _playingPhysOneShot) return;

		if (IsStunned)
		{
			PlayIdle();
			return;
		}

		if (_hasActiveRunes) PlayCasting();
		else PlayIdle();
	}

	// =========================================================
	// PLAY ANIMS
	// =========================================================
	private void PlayIdle()
	{
		if (_animPlayer != null && _animPlayer.HasAnimation(IdleAnim))
		{
			if (_animPlayer.CurrentAnimation != IdleAnim)
				_animPlayer.Play(IdleAnim);
			return;
		}

		// fallback (se ainda usa spriteframes direto)
		if (_anim != null && _anim.SpriteFrames != null && _anim.SpriteFrames.HasAnimation(IdleAnim))
		{
			if (_anim.Animation != IdleAnim)
				_anim.Play(IdleAnim);
		}
	}

	private void PlayCasting()
	{
		if (_animPlayer == null) return;
		if (!_animPlayer.HasAnimation(CastingAnim)) return;

		if (_animPlayer.CurrentAnimation != CastingAnim)
			_animPlayer.Play(CastingAnim);
	}

	private void PlayCastOnce()
	{
		if (_animPlayer == null)
		{
			GD.PushWarning("Mage: sem AnimationPlayer, não dá pra tocar 'cast'.");
			return;
		}

		if (!_animPlayer.HasAnimation(CastAnim))
		{
			GD.PushWarning($"Mage: AnimationPlayer não tem '{CastAnim}'.");
			return;
		}

		_animPlayer.Play(CastAnim);
	}

	private void PlayPhysAttackOnce()
	{
		if (_animPlayer == null)
		{
			GD.PushWarning("Mage: sem AnimationPlayer, não dá pra tocar 'throw'.");
			return;
		}
		if (!_animPlayer.HasAnimation(PhysAttackAnim))
		{
			GD.PushWarning($"Mage: AnimationPlayer não tem '{PhysAttackAnim}'.");
			return;
		}

		_playingPhysOneShot = true;
		_animPlayer.Play(PhysAttackAnim);
	}

	// =========================================================
	// FACING
	// =========================================================
	public void FaceWorldPosition(Vector2 worldPos)
	{
		bool wantRight = worldPos.X >= GlobalPosition.X;
		bool facingRight = FaceRightIsDefault ? wantRight : !wantRight;

		if (UseFlipH && _anim != null)
			_anim.FlipH = !facingRight;
		else
		{
			var node = (Node2D)(_visual ?? this);
			var s = node.Scale;
			s.X = Mathf.Abs(s.X) * (facingRight ? 1f : -1f);
			node.Scale = s;
		}
	}

	// =========================================================
	// PRESSURE API (novo)
	// =========================================================
	//public void AddPressure(float amount, string reason = "")
	//{
		//if (amount <= 0f) return;
//
		//Pressure = Mathf.Clamp(Pressure + amount, 0f, PressureMax);
		//PressureChanged?.Invoke(Pressure, PressureMax);
//
		//if (!string.IsNullOrWhiteSpace(reason))
			//GD.Print($"[Mage][Pressure] +{amount} => {Pressure}/{PressureMax} ({reason})");
//
		//if (Pressure >= PressureMax)
			//_ = TriggerStun();
	//}

	/// <summary>Chame isso quando a Mage tomar dano (futuro: ataques inimigos). Isso dispara TookHit.</summary>
	public void ApplyDamage(int rawDamage)
	{
		// aqui você pode integrar HP do player depois
		int final = Mathf.RoundToInt(rawDamage * IncomingDamageMultiplier);

		TookHit?.Invoke(final);

		GD.Print($"[Mage] TookHit {final} (raw={rawDamage}, mult={IncomingDamageMultiplier:0.00})");
	}
public void ForceStun(float seconds, string reason = "")
{
	
	_ = TriggerStun(seconds, reason);
}
	private async System.Threading.Tasks.Task TriggerStun(float seconds, string reason = "")
{
	if (_stunRunning) return;
	_stunRunning = true;

	IsStunned = true;
	StunChanged?.Invoke(true);
	
	_playingCastOneShot = false;
	_playingPhysOneShot = false;
	PlayIdle();
TryPlayStunAnims();
	float stunDur = Mathf.Max(0.05f, seconds);

	if (!string.IsNullOrWhiteSpace(reason))
		GD.Print($"[Mage][Stun] {stunDur:0.00}s reason={reason}");

	await ToSignal(GetTree().CreateTimer(stunDur), SceneTreeTimer.SignalName.Timeout);

	IsStunned = false;
	StunChanged?.Invoke(false);

	// se você NÃO quiser mais pressure governando stun:
	Pressure = 0f;
	PressureChanged?.Invoke(Pressure, PressureMax);

	_stunRunning = false;
	ReturnToState();
}
	private async void TryPlayStunAnims()
{
	// se não tem AnimatedSprite2D ou não tem frames, ignora
	if (_anim == null || _anim.SpriteFrames == null) return;

	// para o AnimationPlayer pra não brigar com a sprite
	_animPlayer?.Stop();

	// 1) slap rápido
	if (_anim.SpriteFrames.HasAnimation(StunSlapAnim))
	{
		_anim.Play(StunSlapAnim);
		await ToSignal(_anim, AnimatedSprite2D.SignalName.AnimationFinished);
	}

	// 2) loop stun
	if (IsStunned && _anim.SpriteFrames.HasAnimation(StunLoopAnim))
		_anim.Play(StunLoopAnim);
}
}
