using Godot;
using System.Collections.Generic;

public partial class Enemy : Node2D
{
	[ExportCategory("Data")]
	[Export] public EnemyData Data;

	[ExportCategory("Refs")]
	[Export] public NodePath SpritePath = "Sprite";
	[Export] public NodePath ClickAreaPath = "ClickArea";
	[Export] public NodePath VfxHeadPath = "VfxHead";
	[Export] public NodePath VfxCenterPath = "VfxCenter";
	[Export] public NodePath VfxGroundPath = "VfxGround";

	// fallback (se não setar Data)
	[ExportCategory("Fallback")]
	[Export] public int MaxHp = 1000;
	[Export] public bool IsFlying = false;

	public int Hp { get; private set; }
	public bool IsDead => Hp <= 0;

	// escudo atual (sua mecânica já usa isso)
	public HashSet<ElementType> ShieldActive = new();

	private Sprite2D _sprite;
	private bool _selected;

	// Mouse selection
	public Area2D ClickArea { get; private set; }

	// Anchors (para VFX spawn)
	public Marker2D VfxHead { get; private set; }
	public Marker2D VfxCenter { get; private set; }
	public Marker2D VfxGround { get; private set; }

	[Signal] public delegate void DiedEventHandler(Enemy who);
	[Signal] public delegate void HpChangedEventHandler(Enemy who, int hp, int maxHp);

	public override void _Ready()
	{
		AddToGroup("Enemies");
		if (Data != null)
		{
			MaxHp = Data.MaxHp;
			IsFlying = Data.IsFlying;

			if (_sprite != null)
			{
				if (Data.Texture != null)
					_sprite.Texture = Data.Texture;

				_sprite.Scale = Data.SpriteScale;
				_sprite.Position = Data.SpriteOffset;
			}
		}
		_sprite = GetNodeOrNull<Sprite2D>(SpritePath);
		ClickArea = GetNodeOrNull<Area2D>(ClickAreaPath);

		VfxHead = GetNodeOrNull<Marker2D>(VfxHeadPath);
		VfxCenter = GetNodeOrNull<Marker2D>(VfxCenterPath);
		VfxGround = GetNodeOrNull<Marker2D>(VfxGroundPath);

		// aplica Data (se existir)
		if (Data != null)
		{
			MaxHp = Data.MaxHp;
			IsFlying = Data.IsFlying;

			if (_sprite != null && Data.Texture != null)
				_sprite.Texture = Data.Texture;
		}

		Hp = MaxHp;

		if (_sprite == null)
			GD.PushError($"{Name}: Enemy precisa ter Sprite2D em '{SpritePath}'.");

		if (ClickArea == null)
			GD.PushWarning($"{Name}: não encontrei ClickArea em '{ClickAreaPath}' (mouse target não vai funcionar).");

		UpdateHighlight();

		GD.Print($"{GetDisplayName()} spawned with HP {Hp}/{MaxHp}");
		EmitSignal(SignalName.HpChanged, this, Hp, MaxHp);
	}

	public string GetDisplayName() => Data?.DisplayName ?? Name;

	public void SetSelected(bool selected)
	{
		_selected = selected;
		UpdateHighlight();
	}

	private void UpdateHighlight()
	{
		if (_sprite == null) return;
		_sprite.Modulate = _selected ? new Color(1.25f, 1.25f, 1.25f, 1f) : Colors.White;
	}

	// Mantém sua assinatura atual (ElementController usa isso)
	public CastOutcome TakeSpellHit(SpellDefinition spell)
	{
		if (spell == null) return CastOutcome.CancelledNoElements;
		if (IsDead) return CastOutcome.Blocked;

		// MISS por ar/chão
		if (!DoesSpellHitThisEnemy(spell.Targeting))
		{
			GD.Print($"{GetDisplayName()} MISS ({spell.Id}) - targeting {spell.Targeting} vs IsFlying={IsFlying}");
			return CastOutcome.Miss;
		}

		// ABSORVE (escudo cura)
		float healRatio = GetShieldHealRatio(spell.Elements);
		if (healRatio > 0f)
		{
			int heal = Mathf.RoundToInt(spell.Damage * healRatio);
			Heal(heal);
			GD.Print($"{GetDisplayName()} ABSORVEU e curou {heal} (ratio {healRatio}). HP: {Hp}/{MaxHp}");

			RefreshShieldImmediately();
			return healRatio >= 1.0f ? CastOutcome.Absorbed100 : CastOutcome.Absorbed50;
		}

		// HIT normal
		TakeDamage(spell.Damage);
		_ = HitFlashRed();
		GD.Print($"{GetDisplayName()} tomou {spell.Damage} ({spell.PrimaryElement}). HP: {Hp}/{MaxHp}");

		return CastOutcome.Hit;
	}

	private bool DoesSpellHitThisEnemy(SpellTargeting targeting)
	{
		return targeting switch
		{
			SpellTargeting.None => false,
			SpellTargeting.Both => true,
			SpellTargeting.Air => IsFlying,
			SpellTargeting.Ground => !IsFlying,
			_ => true
		};
	}

	private float GetShieldHealRatio(IReadOnlyList<ElementType> castElements)
	{
		if (ShieldActive == null || ShieldActive.Count == 0) return 0f;
		if (castElements == null || castElements.Count == 0) return 0f;

		int matches = 0;
		for (int i = 0; i < castElements.Count; i++)
			if (ShieldActive.Contains(castElements[i])) matches++;

		if (matches == 0) return 0f;
		if (castElements.Count >= 2 && matches >= 2) return 1.0f;
		return 0.5f;
	}

	private void RefreshShieldImmediately()
	{
		// aqui você pluga seu ShieldController real depois
	}

	public void TakeDamage(int amount)
	{
		if (amount <= 0 || IsDead) return;

		Hp = Mathf.Max(Hp - amount, 0);
		EmitSignal(SignalName.HpChanged, this, Hp, MaxHp);

		if (Hp <= 0) Die();
	}

	public void Heal(int amount)
	{
		if (amount <= 0 || IsDead) return;

		Hp = Mathf.Min(Hp + amount, MaxHp);
		EmitSignal(SignalName.HpChanged, this, Hp, MaxHp);
	}

	private async System.Threading.Tasks.Task HitFlashRed()
	{
		if (_sprite == null) return;

		var baseMod = _selected ? new Color(1.25f, 1.25f, 1.25f, 1f) : Colors.White;

		for (int i = 0; i < 2; i++)
		{
			_sprite.Modulate = new Color(1.6f, 0.4f, 0.4f, 1f);
			await ToSignal(GetTree().CreateTimer(0.06f), SceneTreeTimer.SignalName.Timeout);
			_sprite.Modulate = baseMod;
			await ToSignal(GetTree().CreateTimer(0.06f), SceneTreeTimer.SignalName.Timeout);
		}

		UpdateHighlight();
	}

	private void Die()
	{
		if (!IsDead) return;

		GD.Print($"{GetDisplayName()} morreu!");
		EmitSignal(SignalName.Died, this);
		QueueFree();
	}
}
