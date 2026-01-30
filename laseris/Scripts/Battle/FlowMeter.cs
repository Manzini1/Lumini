using Godot;

namespace Game.Battle;

public partial class FlowMeter : Node
{
	public int Stacks { get; private set; }
	public int MaxStacks { get; private set; } = 10;
	public float DamagePerStack { get; private set; } = 0.08f;

	public void Configure(int maxStacks, float damagePerStack)
	{
		MaxStacks = Mathf.Max(0, maxStacks);
		DamagePerStack = Mathf.Max(0f, damagePerStack);
		Stacks = 0;
	}

	public void Add(int amount)
	{
		if (MaxStacks <= 0) return;
		Stacks = Mathf.Clamp(Stacks + amount, 0, MaxStacks);
	}

	public void OnAttackHit()
	{
		Add(1);
	}

	public void OnAttackMiss()
	{
		Stacks = 0;
	}

	public float GetDamageMultiplier()
	{
		return 1f + (Stacks * DamagePerStack);
	}
}
