using Godot;

public partial class Enemy : Node2D
{
	[Export] public EnemyData Data;

	private Sprite2D _sprite;
	private HealthComponent _health;
	private ShieldComponent _shield;
	private Sprite2D _selectionring;
	private bool _selected = false;

	public override void _Ready()
	{
		if (!IsInGroup("Enemies"))
			AddToGroup("Enemies");

		_sprite = GetNode<Sprite2D>("Sprite");
		_health = GetNode<HealthComponent>("Health");
		_shield = GetNodeOrNull<ShieldComponent>("Shield");
		_selectionring = GetNodeOrNull<Sprite2D>("Selected");
		if (_shield == null)
			GD.PushWarning($"{Name}: NÃO tem node 'Shield' com ShieldComponent. Escudo nunca vai absorver.");

		if (Data == null)
		{
			GD.PushWarning($"{Name}: EnemyData não atribuído. Usando defaults.");
			Data = new EnemyData();
		}

		_health.Init(Data.MaxHp);
		_health.Died += OnDied;

		UpdateSelectedVisual();
		GD.Print($"{Name} spawned with HP {_health.CurrentHp}/{_health.MaxHp}");
	}

	private bool IsFlying() => Data != null && Data.IsFlying;

	private bool CanSpellHit(SpellTargeting targeting)
	{
		if (targeting == SpellTargeting.None) return false;

		bool flying = IsFlying();

		if (flying)
			return (targeting & SpellTargeting.Air) != 0;
		else
			return (targeting & SpellTargeting.Ground) != 0;
	}

	public void TakeSpellHit(SpellDefinition spell)
	{
		if (!GodotObject.IsInstanceValid(this)) return;
		if (!_health.IsAlive()) return;
		if (spell == null || spell.Damage <= 0) return;

		// 0) HIT / MISS
		if (!CanSpellHit(spell.Targeting))
		{
			string state = IsFlying() ? "NO AR" : "NO CHÃO";
			GD.Print($"[{Name}] MISS! '{spell.Name}' não acerta alvo {state} (Targeting={spell.Targeting}).");
			return;
		}

		GD.Print($"[{Name}] Hit recebido: {spell.Name} -> {string.Join(" + ", spell.Elements)} (Targeting={spell.Targeting})");

		// 1) Escudo (absorve/nega dano)
		if (_shield != null)
		{
			var result = _shield.EvaluateAbsorb(spell.Elements);

			if (result != ShieldAbsorbResult.NoAbsorb)
			{
				float healFactor = (result == ShieldAbsorbResult.AbsorbHeal100) ? 1.0f : 0.5f;

				int healAmount = Mathf.RoundToInt(_health.MaxHp * _shield.HealPercentOfMaxHp * healFactor);
				healAmount = Mathf.Max(1, healAmount);

				_health.Heal(healAmount);

				GD.Print($"[{Name}] Escudo ABSORVEU ({result}) e curou {healAmount}. HP: {_health.CurrentHp}/{_health.MaxHp}");
				return;
			}
		}

		// 2) Dano normal com resistência (usa elemento principal por enquanto)
		float mult = Data.GetElementMultiplier(spell.PrimaryElement);
		int finalDamage = Mathf.RoundToInt(spell.Damage * mult);

		_health.ApplyDamage(finalDamage);
		GD.Print($"{Name} tomou {finalDamage} ({spell.PrimaryElement}, x{mult}). HP: {_health.CurrentHp}/{_health.MaxHp}");
	}

	public void SetSelected(bool selected)
	{
		_selected = selected;
		UpdateSelectedVisual();
	}

	private void UpdateSelectedVisual()
	{
		if (_sprite != null)
		_sprite.Modulate = _selected ? new Color(1.8f, 1.6f, 1.1f, 1f) : Colors.White;

		if (_selectionring != null)
		_selectionring.Visible = _selected;
	}

	private void OnDied()
	{
		GD.Print($"{Name} morreu!");
		SetSelected(false);
		QueueFree();
	}
}
