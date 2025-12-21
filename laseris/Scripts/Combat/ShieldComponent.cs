using Godot;
using System.Collections.Generic;

public partial class ShieldComponent : Node
{
	[Signal] public delegate void ShieldsChangedEventHandler();

	[Export] public float IntervalSeconds = 2.0f;

	[Export(PropertyHint.Range, "1,8,1")]
	public int ShieldCount = 1;

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
		RollShields();
	}

	public override void _Process(double delta)
	{
		_timer += delta;

		if (_timer >= IntervalSeconds)
		{
			_timer = 0;
			RollShields();
		}
	}

	public void RollShields()
	{
		_active.Clear();

		int count = Mathf.Clamp(ShieldCount, 1, _pool.Length);

		while (_active.Count < count)
		{
			int idx = (int)GD.RandRange(0, _pool.Length - 1);
			_active.Add(_pool[idx]);
		}

		GD.Print($"[{OwnerName}] [Shield] Ativos: {string.Join(", ", _active)}");
		EmitSignal(SignalName.ShieldsChanged);
	}

	public ShieldAbsorbResult EvaluateAbsorb(IReadOnlyList<ElementType> castElements)
	{
		if (_active.Count == 0) return ShieldAbsorbResult.NoAbsorb;
		if (castElements == null || castElements.Count == 0) return ShieldAbsorbResult.NoAbsorb;

		HashSet<ElementType> uniqueCast = new();
		foreach (var e in castElements)
			uniqueCast.Add(e);

		int matches = 0;
		List<ElementType> matched = new();

		foreach (var e in uniqueCast)
		{
			if (_active.Contains(e))
			{
				matches++;
				matched.Add(e);
			}
		}

		if (matches == 0)
			return ShieldAbsorbResult.NoAbsorb;

		// Consome os acertados
		foreach (var m in matched)
			_active.Remove(m);

		GD.Print($"[{OwnerName}] [Shield] ABSORVEU {string.Join(", ", matched)} (matches={matches})");
		EmitSignal(SignalName.ShieldsChanged);

		return (matches >= 2) ? ShieldAbsorbResult.AbsorbHeal100 : ShieldAbsorbResult.AbsorbHeal50;
	}
}

public enum ShieldAbsorbResult
{
	NoAbsorb = 0,
	AbsorbHeal50 = 1,
	AbsorbHeal100 = 2
}
