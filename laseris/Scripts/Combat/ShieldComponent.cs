using Godot;
using System.Collections.Generic;

public partial class ShieldComponent : Node
{
	[Signal] public delegate void ShieldsChangedEventHandler();

	public enum ShieldBehavior
	{
		TimedRotate = 0,        // Tipo 1
		OnDamagedRotate = 1,    // Tipo 2
		OnDamagedMirrorCast = 2 // Tipo 3
	}

	[Export] public ShieldBehavior BehaviorType = ShieldBehavior.TimedRotate;

	// Só usado no TimedRotate
	[Export] public float IntervalSeconds = 2.0f;

	// Quantos elementos o escudo pode ter (1 ou 2 normalmente)
	[Export(PropertyHint.Range, "1,8,1")]
	public int ShieldCount = 1;

	// Cura base (% do MaxHP) quando for "100% heal"
	// matches=2 -> 100%, matches=1 -> 50%
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float HealPercentOfMaxHp = 0.20f;

	private double _timer = 0;

	private readonly HashSet<ElementType> _active = new();

	private static readonly ElementType[] _pool = new[]
	{
		ElementType.Fire,
		ElementType.Ice,
		ElementType.Lightning,
		ElementType.Poison,
		ElementType.Earth,
		ElementType.Air,
		ElementType.Light,
		ElementType.Shadow
	};

	public IReadOnlyCollection<ElementType> ActiveShields => _active;

	private string OwnerName => GetParent()?.Name ?? Name;

	public override void _Ready()
	{
		RollRandomShields();
	}

	public override void _Process(double delta)
	{
		if (BehaviorType != ShieldBehavior.TimedRotate) return;

		_timer += delta;
		if (_timer >= IntervalSeconds)
		{
			_timer = 0;
			RollRandomShields();
		}
	}

	private void LogActive()
	{
		GD.Print($"[{OwnerName}] [Shield] Ativos: {string.Join(", ", _active)} (Mode={BehaviorType})");
	}

	public void RollRandomShields()
	{
		_active.Clear();

		int count = Mathf.Clamp(ShieldCount, 1, _pool.Length);

		while (_active.Count < count)
		{
			int idx = (int)GD.RandRange(0, _pool.Length - 1);
			_active.Add(_pool[idx]);
		}

		_timer = 0; // reseta timer no roll
		LogActive();
		EmitSignal(SignalName.ShieldsChanged);
	}

	public void SetShieldsToCast(IReadOnlyList<ElementType> castElements)
	{
		_active.Clear();

		// pega únicos e mantém ordem simples
		HashSet<ElementType> unique = new();
		foreach (var e in castElements)
		{
			if (!unique.Contains(e))
				unique.Add(e);
		}

		// no seu escopo atual (1 ou 2 elementos), isso fica perfeito:
		foreach (var e in unique)
		{
			_active.Add(e);
			if (_active.Count >= 2) break; // você pode mudar pra ShieldCount se quiser
		}

		_timer = 0;
		LogActive();
		EmitSignal(SignalName.ShieldsChanged);
	}

	/// <summary>
	/// Regra:
	/// - Se qualquer elemento do cast bater com qualquer ativo -> absorve (nega dano)
	/// - 1 match -> Heal50
	/// - 2 matches -> Heal100
	/// </summary>
	public ShieldAbsorbResult EvaluateAbsorb(IReadOnlyList<ElementType> castElements)
	{
		if (_active.Count == 0) return ShieldAbsorbResult.NoAbsorb;
		if (castElements == null || castElements.Count == 0) return ShieldAbsorbResult.NoAbsorb;

		HashSet<ElementType> uniqueCast = new();
		foreach (var e in castElements)
			uniqueCast.Add(e);

		int matches = 0;
		foreach (var e in uniqueCast)
		{
			if (_active.Contains(e))
				matches++;
		}

		if (matches == 0) return ShieldAbsorbResult.NoAbsorb;

		// ✅ Sempre que ABSORVER: troca imediatamente (sem esperar o tick)
		GD.Print($"[{OwnerName}] [Shield] ABSORVEU cast (matches={matches}) -> trocando escudo AGORA");
		RollRandomShields();

		return (matches >= 2) ? ShieldAbsorbResult.AbsorbHeal100 : ShieldAbsorbResult.AbsorbHeal50;
	}

	/// <summary>
	/// Chamado quando o inimigo tomou DANO (hit passou).
	/// </summary>
	public void OnTookDamage(IReadOnlyList<ElementType> castElements)
	{
		if (castElements == null || castElements.Count == 0) return;

		switch (BehaviorType)
		{
			case ShieldBehavior.TimedRotate:
				// Tipo 1: não muda ao tomar dano (só no tempo / absorção)
				break;

			case ShieldBehavior.OnDamagedRotate:
				// Tipo 2: ao tomar dano, troca imediatamente
				GD.Print($"[{OwnerName}] [Shield] TOMOU DANO -> trocando escudo AGORA (Mode=OnDamagedRotate)");
				RollRandomShields();
				break;

			case ShieldBehavior.OnDamagedMirrorCast:
				// Tipo 3: ao tomar dano, escudo vira os elementos do cast
				GD.Print($"[{OwnerName}] [Shield] TOMOU DANO -> escudo vira elementos do cast (Mode=OnDamagedMirrorCast)");
				SetShieldsToCast(castElements);
				break;
		}
	}
}

public enum ShieldAbsorbResult
{
	NoAbsorb = 0,
	AbsorbHeal50 = 1,
	AbsorbHeal100 = 2
}
