using Godot;
using System;
using System.Collections.Generic;

public partial class Enemy : Node2D
{
	[ExportCategory("Data (opcional)")]
	[Export] public EnemyData Data;

	[ExportCategory("Fallback (se Data = null)")]
	[Export] public int MaxHp = 1000;
	[Export] public bool IsFlying = false;

	public int Hp { get; private set; }
	public bool IsDead => Hp <= 0;
	public bool IsSelected { get; private set; } = false;

	public ShieldController Shield { get; private set; }

	private Sprite2D _sprite;
	private bool _selected = false;

	[Signal] public delegate void DiedEventHandler(Enemy who);
	[Signal] public delegate void HpChangedEventHandler(Enemy who, int hp, int maxHp);

	public override void _Ready()
	{
		AddToGroup("Enemies");

		_sprite = FindFirstChildOfType<Sprite2D>(this);
		if (_sprite == null)
		{
			GD.PushError($"{Name}: Enemy precisa ter um Sprite2D dentro da cena.");
			return;
		}

		Shield = GetNodeOrNull<ShieldController>("ShieldController");
		if (Shield == null)
			GD.PushError($"{Name}: não encontrei 'ShieldController' como filho.");

		ApplyDataIfAny();

		Hp = MaxHp;
		GD.Print($"{Name} spawned with HP {Hp}/{MaxHp}");
		EmitSignal(SignalName.HpChanged, this, Hp, MaxHp);
	}

	public void ApplyData(EnemyData data)
	{
		Data = data;
		ApplyDataIfAny();
	}

	private void ApplyDataIfAny()
	{
		if (Data == null) return;

		MaxHp = Data.MaxHp;
		IsFlying = Data.IsFlying;

		if (Data.SpriteTexture != null && _sprite != null)
			_sprite.Texture = Data.SpriteTexture;
	}

	public void SetSelected(bool selected)
	{
		IsSelected = selected;
		_selected = selected;
		UpdateHighlight();
	}

	// =========================================================
	// ✅ REINTRODUZIDO: usado pelo ElementController (compilação)
	// =========================================================
	public CastOutcome TakeSpellHit(SpellDefinition spell)
	{
		if (spell == null)
		{
			NotifyShieldResolved(spell, CastOutcome.CancelledNoElements);
			return CastOutcome.CancelledNoElements;
		}

		if (IsDead)
		{
			NotifyShieldResolved(spell, CastOutcome.Blocked);
			return CastOutcome.Blocked;
		}

		if (!DoesSpellHitThisEnemy(spell.Targeting))
		{
			GD.Print($"{Name} MISS ({spell.Id}) - targeting {spell.Targeting} vs IsFlying={IsFlying}");
			NotifyShieldResolved(spell, CastOutcome.Miss);
			return CastOutcome.Miss;
		}

		// Shield absorb/heal
		float healRatio = GetShieldHealRatio(spell.Elements);

		if (healRatio > 0f)
		{
			int heal = Mathf.RoundToInt(spell.Damage * healRatio);
			Heal(heal);

			var outcome = (healRatio >= 1.0f) ? CastOutcome.Absorbed100 : CastOutcome.Absorbed50;
			NotifyShieldResolved(spell, outcome);
			return outcome;
		}

		// Hit normal
		TakeDamage(spell.Damage);
		_ = HitFlashRed();

		NotifyShieldResolved(spell, CastOutcome.Hit);
		return CastOutcome.Hit;
	}

	private void NotifyShieldResolved(SpellDefinition spell, CastOutcome outcome)
	{
		if (Shield == null) return;
		Shield.NotifySpellResolved(spell, outcome);
	}

	// helper pro ritmo (pega o elemento ativo do shield)
	public ElementType ShieldPrimaryOrFallback()
	{
		if (Shield == null || Shield.Active == null || Shield.Active.Count == 0)
			return ElementType.Fire;

		foreach (var e in Shield.Active)
			return e;

		return ElementType.Fire;
	}

	public void TakeDamage(int amount)
	{
		if (amount <= 0 || IsDead) return;

		Hp = Mathf.Max(Hp - amount, 0);
		EmitSignal(SignalName.HpChanged, this, Hp, MaxHp);

		if (Hp <= 0)
			Die();
	}

	public void Heal(int amount)
	{
		if (amount <= 0 || IsDead) return;

		Hp = Mathf.Min(Hp + amount, MaxHp);
		EmitSignal(SignalName.HpChanged, this, Hp, MaxHp);
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
		if (Shield == null) return 0f;
		if (Shield.Active == null || Shield.Active.Count == 0) return 0f;
		if (castElements == null || castElements.Count == 0) return 0f;

		int matches = 0;

		for (int i = 0; i < castElements.Count; i++)
		{
			if (Shield.Active.Contains(castElements[i]))
				matches++;
		}

		if (matches == 0) return 0f;

		if (castElements.Count >= 2 && matches >= 2) return 1.0f;
		return 0.5f;
	}

	private async System.Threading.Tasks.Task HitFlashRed()
	{
		if (_sprite == null) return;

		var baseMod = GetBaseModulateForCurrentState();

		for (int i = 0; i < 2; i++)
		{
			_sprite.Modulate = new Color(1.6f, 0.4f, 0.4f, 1f);
			await ToSignal(GetTree().CreateTimer(0.06f), SceneTreeTimer.SignalName.Timeout);
			_sprite.Modulate = baseMod;
			await ToSignal(GetTree().CreateTimer(0.06f), SceneTreeTimer.SignalName.Timeout);
		}

		UpdateHighlight();
	}

	private Color GetBaseModulateForCurrentState()
	{
		if (_selected) return new Color(1.25f, 1.25f, 1.25f, 1f);
		return Colors.White;
	}

	private void UpdateHighlight()
	{
		if (_sprite == null) return;
		_sprite.Modulate = GetBaseModulateForCurrentState();
	}

	private void Die()
	{
		if (IsDead == false) return;

		GD.Print($"{Name} morreu!");
		EmitSignal(SignalName.Died, this);
		QueueFree();
	}

	private static T FindFirstChildOfType<T>(Node root) where T : Node
	{
		foreach (var childObj in root.GetChildren())
		{
			if (childObj is Node child)
			{
				if (child is T typed) return typed;

				var deeper = FindFirstChildOfType<T>(child);
				if (deeper != null) return deeper;
			}
		}
		return null;
	}
}
