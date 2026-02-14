using Godot;

namespace Game.Battle;

public partial class FlowMeter : Node
{
	public int Stacks { get; private set; }
	public int MaxStacks { get; private set; } = 10;

	// mantém por compatibilidade com seu PhaseDefinition (mesmo que não usemos direto na regra nova)
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

	public void OnAttackHit() => Add(1);

	public void OnAttackMiss() => Stacks = 0;

	/// <summary>
	/// Regra nova:
	/// - Se stacks == MaxStacks => 2.0
	/// - Se stacks < MaxStacks => escala 1.0 .. 1.5 (até MaxStacks-1)
	/// </summary>
	public float GetSkillDamageMultiplier(int stacksAfterHit)
	{
		if (MaxStacks <= 0) return 1f;

		int s = Mathf.Clamp(stacksAfterHit, 0, MaxStacks);

		if (s >= MaxStacks) return 2.0f;
		if (MaxStacks == 1) return 1.0f; // só existe s=0 antes do full

		float denom = MaxStacks - 1;
		float t = s / denom;         // 0..1 (quando s = MaxStacks-1)
		return 1.0f + 0.5f * t;      // 1.0 .. 1.5
	}

	// (Opcional) se alguém ainda usa o modelo antigo
	public float GetDamageMultiplierLegacy()
	{
		return 1f + (Stacks * DamagePerStack);
	}
}
