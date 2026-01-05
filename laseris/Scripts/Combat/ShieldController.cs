using Godot;
using System;
using System.Collections.Generic;

public enum ShieldMode
{
	TimedRotate = 0,
	OnAnyHitRotate = 1,
	MirrorSpellElements = 2
}

public partial class ShieldController : Node
{
	[Export] public ShieldMode Mode = ShieldMode.TimedRotate;
	[Export] public float RotateEverySeconds = 3f;

	[Export] public int MinActive = 1;
	[Export] public int MaxActive = 2;

	public readonly HashSet<ElementType> Active = new();

	// Lista read-only (pra bater com handlers)
	public event Action<IReadOnlyList<ElementType>> Changed;

	private float _t = 0f;

	private readonly RandomNumberGenerator _rng = new();

	private static readonly ElementType[] Pool = new[]
	{
		ElementType.Fire, ElementType.Ice, ElementType.Lightning, ElementType.Poison,
		ElementType.Earth, ElementType.Air, ElementType.Light, ElementType.Shadow
	};

	public override void _Ready()
	{
		_rng.Randomize();
		RefreshRandom();
	}

	public override void _Process(double delta)
	{
		if (Mode != ShieldMode.TimedRotate) return;

		_t += (float)delta;
		if (_t >= RotateEverySeconds)
		{
			_t = 0f;
			RefreshRandom();
		}
	}

	public void NotifySpellResolved(SpellDefinition spell, CastOutcome outcome)
	{
		if (spell == null) return;

		bool absorbed = (outcome == CastOutcome.Absorbed50 || outcome == CastOutcome.Absorbed100);

		if (Mode == ShieldMode.OnAnyHitRotate)
		{
			RefreshRandom();
			return;
		}

		if (Mode == ShieldMode.MirrorSpellElements)
		{
			Active.Clear();

			int limit = Mathf.Clamp(MaxActive, 1, 2);
			for (int i = 0; i < spell.Elements.Count && Active.Count < limit; i++)
				Active.Add(spell.Elements[i]);

			_t = 0f;
			EmitChanged();
			return;
		}

		if (absorbed)
		{
			RefreshRandom();
			return;
		}
	}

	public void RefreshRandom()
	{
		Active.Clear();

		int min = Mathf.Clamp(MinActive, 1, 2);
		int max = Mathf.Clamp(MaxActive, 1, 2);
		if (max < min) max = min;

		int count = _rng.RandiRange(min, max);

		while (Active.Count < count)
		{
			int idx = _rng.RandiRange(0, Pool.Length - 1);
			Active.Add(Pool[idx]);
		}

		_t = 0f;
		EmitChanged();
	}

	private void EmitChanged()
	{
		var snapshot = new List<ElementType>(Active);
		Changed?.Invoke(snapshot);
	}
}
