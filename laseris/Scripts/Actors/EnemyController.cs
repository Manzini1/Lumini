using Godot;
using System;

namespace Game.Characters;

public partial class EnemyController : Node2D
{
	[Signal] public delegate void HealthChangedEventHandler(int current, int max);
	[Signal] public delegate void DiedEventHandler();

	public AnimatedSprite2D Sprite { get; private set; }
	public AnimatedSprite2D AttackHint { get; private set; }
	public Marker2D Muzzle { get; private set; }
	public Sprite2D StanceIcon { get; private set; }

	[ExportGroup("Projectile")]
	[Export] public PackedScene RhythmProjectileScene;
	[Export] public int BaseDamage = 10;

	[ExportGroup("Stats")]
	[Export] public int MaxHp = 80;
	[Export] public bool DebugPrints = false;

	// ======================
	// STANCES (por tempo)
	// ======================
	[ExportGroup("Stances")]
	[Export] public int StanceCount = 1;          // 1 ou 2
	[Export] public int StanceAElementId = 1;     // 1..N
	[Export] public int StanceBElementId = 2;     // 1..N (se StanceCount=2)

	// Fração do turno em stance A (0.5 = metade/metade)
	[Export(PropertyHint.Range, "0.0,1.0,0.01")]
	public float StanceSplit = 0.5f;

	// Texturas por elemento: index = elementId-1
	[ExportGroup("Stances")]
	[Export] public Godot.Collections.Array<Texture2D> ElementTextures = new();

	[ExportGroup("Stances")]
	[Export] public bool PulseIconOnChange = true;

	public int Hp { get; private set; }
	public bool IsDead => Hp <= 0;

	public override void _Ready()
	{
		Sprite = GetNode<AnimatedSprite2D>("Sprite");
		AttackHint = GetNode<AnimatedSprite2D>("AttackHint");
		Muzzle = GetNode<Marker2D>("Muzzle");

		StanceIcon = GetNodeOrNull<Sprite2D>("StanceIcon");
		SetStanceIconVisible(false);

		Hp = MaxHp;
		EmitSignal(SignalName.HealthChanged, Hp, MaxHp);

		PlayIfExists(Sprite, "idle"); // se você usa "default", troque aqui
	}

	// progress01 = 0..1 dentro do turno atual do inimigo
	public int GetStanceElementForTurnProgress(double progress01)
	{
		if (StanceCount <= 1) return StanceAElementId;

		double p = Math.Clamp(progress01, 0.0, 1.0);
		return p < StanceSplit ? StanceAElementId : StanceBElementId;
	}

	public void SetStanceIconVisible(bool visible)
	{
		if (StanceIcon != null) StanceIcon.Visible = visible;
	}

	public void SetStanceElementHint(int elementId, bool pulse)
	{
		if (StanceIcon == null) return;

		int idx = elementId - 1;
		if (idx >= 0 && idx < ElementTextures.Count && ElementTextures[idx] != null)
			StanceIcon.Texture = ElementTextures[idx];

		if (pulse && PulseIconOnChange)
		{
			var t = CreateTween();
			StanceIcon.Scale = Vector2.One * 0.9f;

			t.TweenProperty(StanceIcon, "scale", Vector2.One * 1.15f, 0.06f)
			 .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
			t.TweenProperty(StanceIcon, "scale", Vector2.One, 0.10f)
			 .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
		}
	}

	public void PlayPrepare() => PlayIfExists(AttackHint, "prepare");
	public void PlayShoot() => PlayIfExists(AttackHint, "shoot");

	public void ShootAt(Node2D projectilesParent, Node2D mage, int damage, bool blocked)
	{
		if (RhythmProjectileScene == null || projectilesParent == null || mage == null)
			return;

		var inst = RhythmProjectileScene.Instantiate();
		if (inst is not Node2D projNode)
		{
			inst.QueueFree();
			return;
		}

		projectilesParent.AddChild(projNode);

		if (projNode.HasMethod("Launch"))
			projNode.Call("Launch", Muzzle.GlobalPosition, mage, damage, blocked);
		else
		{
			GD.PushError("Projectile scene não tem método Launch(startWorld, mage, damage, blocked).");
			projNode.QueueFree();
		}

		if (DebugPrints)
			GD.Print($"[Enemy] ShootAt blocked={blocked} dmg={damage}");
	}

	public void ApplyDamage(int amount)
	{
		if (IsDead) return;

		int dmg = Mathf.Max(0, amount);
		Hp = Mathf.Max(0, Hp - dmg);
		EmitSignal(SignalName.HealthChanged, Hp, MaxHp);

		PlayIfExists(Sprite, "hurt"); // se você usa "hit", troque aqui

		if (Hp <= 0)
		{
			PlayIfExists(Sprite, "dead");
			EmitSignal(SignalName.Died);
		}
	}

	private static void PlayIfExists(AnimatedSprite2D spr, string anim)
	{
		if (spr?.SpriteFrames == null) return;
		if (!spr.SpriteFrames.HasAnimation(anim)) return;
		if (spr.Animation == anim && spr.IsPlaying()) return;
		spr.Play(anim);
	}
}
