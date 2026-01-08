using Godot;
using System;

public partial class Mage : CharacterBody2D
{
	[ExportCategory("Refs")]
	[Export] public NodePath VisualPath = "Sprite";                 // opcional
	[Export] public NodePath AnimPath = "Sprite";                   // AnimatedSprite2D (p/ flip)
	[Export] public NodePath AnimPlayerPath = "AnimationPlayer";    // AnimationPlayer
	[Export] public NodePath TargetControllerPath;                  // p/ pegar alvo atual
	[Export] public NodePath ElementControllerPath;                 // ✅ pra casting/cast via eventos

	[ExportCategory("Weapon Refs")]
	[Export] public NodePath WeaponSocketPath = "WeaponSocket";                      // Node2D
	[Export] public NodePath WeaponInHandPath = "WeaponSocket/Weapon";               // Node2D
	[Export] public NodePath ThrownWeaponRootPath = "WeaponSocket/ThrownWeaponsRoot"; // Node2D onde instancia
	[Export] public PackedScene ThrownWeaponScene;                                    // ThrownWeapon.tscn

	[ExportCategory("Weapon Visual")]
	[Export] public float ThrownWeaponScale = 1.0f;

	[ExportCategory("Damage")]
	[Export] public int PhysDamage = 35;

	[ExportCategory("Anim Names")]
	[Export] public string IdleAnim = "idle";
	[Export] public string CastingAnim = "casting";
	[Export] public string CastAnim = "cast";
	[Export] public string PhysAttackAnim = "throw";

	[ExportCategory("Facing")]
	[Export] public bool FaceRightIsDefault = true;
	[Export] public bool UseFlipH = true;

	private Node2D _visual;
	private AnimatedSprite2D _anim;
	private AnimationPlayer _animPlayer;
	private TargetController _targetController;
	private ElementController _elementController;

	private Node2D _weaponSocket;
	private Node2D _weaponInHand;
	private Node2D _thrownRoot;

	// ---------- state ----------
	private bool _hasActiveRunes;
	private bool _playingCastOneShot;
	private bool _playingPhysOneShot;

	// ---------- weapon lock ----------
	private bool _weaponInFlight;
	public bool WeaponInFlight => _weaponInFlight;
	public event Action<bool> WeaponFlightChanged;

	// alvo guardado no “prepare” pra usar no ReleaseThrow (chamado pelo AnimationPlayer)
	private Vector2 _pendingThrowTarget;
	private Enemy _pendingThrowEnemy;

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

		// ✅ re-liga eventos pra casting/cast funcionar
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

	// ---------------- INPUT (throw) ----------------

	public override void _UnhandledInput(InputEvent e)
	{
		if (!e.IsActionPressed("physattack"))
			return;

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

	// ---------------- SPELL EVENTS (casting/cast) ----------------

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
		_playingCastOneShot = true;
		PlayCastOnce();
	}

	private void OnCastResolved(CastOutcome outcome, SpellDefinition spell, Enemy target)
	{
		// por enquanto nada. (depois você pode dar feedback diferente aqui)
	}

	// ---------------- CALL METHOD TRACK (throw) ----------------

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

		thrown.Hit += () =>
		{
			if (hitEnemy == null || !GodotObject.IsInstanceValid(hitEnemy))
				return;

			hitEnemy.TakeDamage(dmg);

			// ⚠️ Se você ainda não criou ShowPhysicalDamage, comenta essa linha por enquanto
			GetDamagePopupManager()?.ShowPhysicalDamage(hitEnemy, dmg);

			GD.Print($"[Mage] HIT phys -> {hitEnemy.Name} -{dmg}");
		};

		thrown.Finished += success =>
		{
			_weaponInHand?.SetDeferred("visible", true);
			SetWeaponInFlight(false);
		};

		thrown.Launch(_weaponSocket, start, target);
	}

	private void SetWeaponInFlight(bool v)
	{
		if (_weaponInFlight == v) return;
		_weaponInFlight = v;
		WeaponFlightChanged?.Invoke(_weaponInFlight);
	}

	// ---------------- ANIM FINISH ----------------

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

		if (_hasActiveRunes) PlayCasting();
		else PlayIdle();
	}

	// ---------------- PLAY ANIMS ----------------

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

	// ---------------- FACING ----------------

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
}
