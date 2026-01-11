using Godot;
using System;
using System.Collections.Generic;

public partial class Enemy : Node2D
{
	[ExportCategory("Data (opcional)")]
	[Export] public EnemyData Data;
public int SlotIndex { get; set; } = -1;

	[ExportCategory("Fallback (se Data = null)")]
	[Export] public int MaxHp = 1000;
	[Export] public bool IsFlying = false;

	public int Hp { get; private set; }
	public bool IsDead => Hp <= 0;

	public bool IsSelected { get; private set; } = false;
	public ShieldController Shield { get; private set; } // esperado existir como filho

	private Sprite2D _sprite;

	public Area2D ClickArea { get; private set; }

	// ✅ bom pra visuais (shield/círculo) ouvirem sem polling
	public event Action<Enemy, bool> SelectedChanged;

	[Signal] public delegate void DiedEventHandler(Enemy who);
	[Signal] public delegate void HpChangedEventHandler(Enemy who, int hp, int maxHp);
// =========================================================
// STUN (para Tug / controle de turnos)
// =========================================================
[ExportCategory("Stun")]
[Export] public float DefaultStunSeconds = 1.5f;

public bool IsStunned { get; private set; } = false;
public event Action<bool> StunChanged;

private bool _stunRunning = false;

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

	float stunDur = Mathf.Max(0.05f, seconds);

	if (!string.IsNullOrWhiteSpace(reason))
		GD.Print($"[Enemy][Stun] {Name} stunned for {stunDur:0.00}s reason={reason}");

	// (por enquanto só trava estado; depois você pode plugar VFX / AI pause aqui)
	await ToSignal(GetTree().CreateTimer(stunDur), SceneTreeTimer.SignalName.Timeout);

	IsStunned = false;
	StunChanged?.Invoke(false);

	_stunRunning = false;
}

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
			GD.PushError($"{Name}: não encontrei 'ShieldController' como filho. Adicione o node na cena Enemy.tscn.");

		ClickArea = GetNodeOrNull<Area2D>("ClickArea");

		ApplyDataIfAny();

		Hp = MaxHp;
		GD.Print($"{Name} spawned with HP {Hp}/{MaxHp}");
		EmitSignal(SignalName.HpChanged, this, Hp, MaxHp);

		UpdateHighlight();
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
		if (IsSelected == selected) return;

		IsSelected = selected;
		UpdateHighlight();

		SelectedChanged?.Invoke(this, selected);
	}

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

		float healRatio = GetShieldHealRatio(spell.Elements);

		if (healRatio > 0f)
		{
			int heal = Mathf.RoundToInt(spell.Damage * healRatio);
			Heal(heal);

			GD.Print($"{Name} ABSORVEU ({FormatElements(spell.Elements)}) e curou {heal} (ratio {healRatio}). HP: {Hp}/{MaxHp}");

			var outcome = (healRatio >= 1.0f) ? CastOutcome.Absorbed100 : CastOutcome.Absorbed50;
			NotifyShieldResolved(spell, outcome);
			return outcome;
		}

		TakeDamage(spell.Damage);
		_ = HitFlashRed();

		GD.Print($"{Name} tomou {spell.Damage} ({spell.PrimaryElement}). HP: {Hp}/{MaxHp}");

		NotifyShieldResolved(spell, CastOutcome.Hit);
		return CastOutcome.Hit;
	}

	private void NotifyShieldResolved(SpellDefinition spell, CastOutcome outcome)
	{
		if (Shield == null) return;
		Shield.NotifySpellResolved(spell, outcome);
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
		return IsSelected ? new Color(1.25f, 1.25f, 1.25f, 1f) : Colors.White;
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
