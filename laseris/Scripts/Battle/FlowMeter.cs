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

	public void OnAttackHit()
	{
		if (MaxStacks <= 0) return;
		Stacks = Mathf.Min(MaxStacks, Stacks + 1);
	}

	public void OnAttackMiss()
	{
		Stacks = 0;
	}

	// multiplicador total (ex: 0 stacks = 1.0, 5 stacks com 0.08 = 1.40)
	public float GetDamageMultiplier()
	{
		return 1f + (Stacks * DamagePerStack);
	}
}
