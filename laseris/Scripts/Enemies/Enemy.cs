using Godot;
using System.Collections.Generic;

public partial class Enemy : Node2D
{
	[Export] public int MaxHp = 1000;
	[Export] public bool IsFlying = false;

	public int Hp { get; private set; }
	public bool IsDead => Hp <= 0;

	public HashSet<ElementType> ShieldActive = new();

	private Sprite2D _sprite;

	// ✅ Área de clique (opcional)
	public Area2D ClickArea { get; private set; }

	private bool _selected = false;

	[Signal] public delegate void DiedEventHandler(Enemy who);
	[Signal] public delegate void HpChangedEventHandler(Enemy who, int hp, int maxHp);

	public override void _Ready()
	{
		Hp = MaxHp;

		_sprite = FindFirstChildOfType<Sprite2D>(this);
		if (_sprite == null)
		{
			GD.PushError($"{Name}: Enemy precisa ter um Sprite2D dentro da cena.");
			return;
		}

		ClickArea = GetNodeOrNull<Area2D>("ClickArea");
		if (ClickArea == null)
			GD.PushWarning($"{Name}: não encontrei 'ClickArea' (Area2D). Mouse selection não vai funcionar nesse inimigo.");

		GD.Print($"{Name} spawned with HP {Hp}/{MaxHp}");
		EmitSignal(SignalName.HpChanged, this, Hp, MaxHp);
	}

	public void SetSelected(bool selected)
	{
		_selected = selected;
		UpdateHighlight();
	}

	public CastOutcome TakeSpellHit(SpellDefinition spell)
	{
		if (spell == null)
			return CastOutcome.CancelledNoElements;

		if (IsDead)
			return CastOutcome.Blocked;

		// MISS por ar/chão
		if (!DoesSpellHitThisEnemy(spell.Targeting))
		{
			GD.Print($"{Name} MISS ({spell.Id}) - targeting {spell.Targeting} vs IsFlying={IsFlying}");
			return CastOutcome.Miss;
		}

		// ABSORVE (escudo cura)
		float healRatio = GetShieldHealRatio(spell.Elements);
		if (healRatio > 0f)
		{
			int heal = Mathf.RoundToInt(spell.Damage * healRatio);
			Heal(heal);

			GD.Print($"{Name} ABSORVEU ({FormatElements(spell.Elements)}) e curou {heal} (ratio {healRatio}). HP: {Hp}/{MaxHp}");

			RefreshShieldImmediately();

			return healRatio >= 1.0f ? CastOutcome.Absorbed100 : CastOutcome.Absorbed50;
		}

		// HIT normal
		TakeDamage(spell.Damage);
		_ = HitFlashRed();
		GD.Print($"{Name} tomou {spell.Damage} ({spell.PrimaryElement}). HP: {Hp}/{MaxHp}");

		return CastOutcome.Hit;
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
		if (ShieldActive == null || ShieldActive.Count == 0) return 0f;
		if (castElements == null || castElements.Count == 0) return 0f;

		int matches = 0;
		for (int i = 0; i < castElements.Count; i++)
			if (ShieldActive.Contains(castElements[i])) matches++;

		if (matches == 0) return 0f;

		// regra que você pediu:
		// se 2 elementos do cast baterem -> 100%
		// se 1 bater -> 50%
		if (castElements.Count >= 2 && matches >= 2) return 1.0f;
		return 0.5f;
	}

	private void RefreshShieldImmediately()
	{
		// aqui você liga com sua lógica real do ShieldController
		GD.Print($"[{Name}] Shield refresh imediato (placeholder).");
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
		if (!IsDead) return;

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

	private static string FormatElements(IReadOnlyList<ElementType> elements)
	{
		if (elements == null || elements.Count == 0) return "None";
		return string.Join(", ", elements);
	}
}
