using Godot;
using Game.Combat;

namespace Game.Characters;

public partial class EnemyController : Node2D
{
	[Signal] public delegate void HealthChangedEventHandler(int current, int max);
	[Signal] public delegate void DiedEventHandler();

	[ExportGroup("Stats")]
	[Export] public int MaxHp = 80;
	[Export] public int BaseDamage = 10;

	[ExportGroup("Projectile (fallback)")]
	[Export] public PackedScene RhythmProjectileScene;

	[ExportGroup("Elemental Projectiles")]
	[Export] public PackedScene[] ProjectileByElementId = new PackedScene[8]; // 1..6

	[ExportGroup("Refs")]
	[Export] public NodePath SpritePath = "Sprite";
	[Export] public NodePath MuzzlePath = "WeaponSocket";
	[Export] public NodePath GroundPointPath = "GroundPoint";
	[Export] public NodePath HitPointPath = "HitPoint";

	[ExportGroup("Stance (opcional)")]
	[Export] public NodePath StanceIconPath = "StanceIcon";

	[ExportGroup("VFX (opcional)")]
	[Export] public PackedScene DamagePopupScene;
	[Export] public NodePath DamagePopupParentPath = "../../Vfx";
	[Export] public Vector2 DamagePopupOffset = new Vector2(0, -48);

	private AnimatedSprite2D _sprite;
	private Marker2D _muzzle;
	private Marker2D _groundPoint;
	private Marker2D _hitPoint;

	private Node _stanceIcon;
	private Node2D _damagePopupParent;

	public int Hp { get; private set; }
	public bool IsDead => Hp <= 0;

	public int CurrentStanceElement { get; private set; } = 1;

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<AnimatedSprite2D>(SpritePath);
		_muzzle = GetNodeOrNull<Marker2D>(MuzzlePath);
		_groundPoint = GetNodeOrNull<Marker2D>(GroundPointPath);
		_hitPoint = GetNodeOrNull<Marker2D>(HitPointPath);

		_stanceIcon = GetNodeOrNull<Node>(StanceIconPath);
		_damagePopupParent = GetNodeOrNull<Node2D>(DamagePopupParentPath);

		Hp = MaxHp;
		EmitSignal(SignalName.HealthChanged, Hp, MaxHp);

		PlayIdle();
	}

	public Vector2 GetMuzzleGlobal() => _muzzle != null ? _muzzle.GlobalPosition : GlobalPosition;
	public Vector2 GetGroundPointGlobal() => _groundPoint != null ? _groundPoint.GlobalPosition : GlobalPosition;
	public Vector2 GetHitPointGlobal() => _hitPoint != null ? _hitPoint.GlobalPosition : GlobalPosition;

	public void PlayPrepare() => PlayAny(_sprite, "prepare");
	public void PlayShoot() => PlayAny(_sprite, "shoot");
	public void PlayHit() => PlayAny(_sprite, "hit");
	private void PlayIdle() => PlayAny(_sprite, "default", "idle");

	private static bool PlayAny(AnimatedSprite2D sprite, params string[] anims)
	{
		if (sprite?.SpriteFrames == null) return false;

		foreach (var a in anims)
		{
			if (sprite.SpriteFrames.HasAnimation(a))
			{
				if (sprite.Animation == a && sprite.IsPlaying()) return true;
				sprite.Play(a);
				return true;
			}
		}
		return false;
	}

	public PackedScene GetProjectileSceneForElement(int elementId)
	{
		if (ProjectileByElementId == null) return null;
		if (elementId < 1 || elementId >= ProjectileByElementId.Length) return null;
		return ProjectileByElementId[elementId];
	}

	public void ApplyDamage(int amount)
	{
		if (IsDead) return;

		int dmg = Mathf.Max(0, amount);
		Hp = Mathf.Max(0, Hp - dmg);

		EmitSignal(SignalName.HealthChanged, Hp, MaxHp);

		if (dmg > 0)
			ShowDamagePopupText(dmg.ToString(), Colors.OrangeRed, 1.7f);

		PlayHit();

		if (Hp <= 0)
			EmitSignal(SignalName.Died);
	}

	public void ShowDamagePopupText(string text, Color color, float scaleMult = 1.0f)
	{
		if (DamagePopupScene == null || _damagePopupParent == null) return;

		var inst = DamagePopupScene.Instantiate();
		if (inst is not Node2D popupNode) { inst.QueueFree(); return; }

		_damagePopupParent.AddChild(popupNode);
		popupNode.GlobalPosition = GlobalPosition + DamagePopupOffset;

		if (inst.HasMethod("ShowText"))
			inst.Call("ShowText", text, color, scaleMult);
	}

	public void SetStanceElementHint(int elementId, bool pulse)
	{
		CurrentStanceElement = elementId;

		if (_stanceIcon is AnimatedSprite2D stanceSprite && stanceSprite.SpriteFrames != null)
		{
			string anim = $"e{elementId}";
			if (stanceSprite.SpriteFrames.HasAnimation(anim))
			{
				stanceSprite.Visible = true;
				stanceSprite.Play(anim);
			}

			if (pulse)
			{
				stanceSprite.Scale = Vector2.One * 1.1f;
				var tw = CreateTween();
				tw.TweenProperty(stanceSprite, "scale", Vector2.One, 0.08f);
			}
		}
	}

	public RhythmProjectile ShootAt(Node2D projectilesParent, MageController mage, int damageOnHit)
	{
		if (RhythmProjectileScene == null) { GD.PushWarning("EnemyController: RhythmProjectileScene null."); return null; }
		if (projectilesParent == null || mage == null) return null;

		var inst = RhythmProjectileScene.Instantiate();
		if (inst is not RhythmProjectile proj) { inst.QueueFree(); return null; }

		projectilesParent.AddChild(proj);

		Vector2 start = GetMuzzleGlobal();
		Vector2 block = mage.GetBlockPointGlobal();
		Vector2 hit = mage.GetHitPointGlobal();

		proj.Launch(start, mage, block, hit, damageOnHit);
		return proj;
	}
}
