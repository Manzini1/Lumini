using System.Collections.Generic;

public sealed class SpellDefinition
{
	public string Name { get; }
	public IReadOnlyList<ElementType> Elements { get; }
	public int Damage { get; }
	public SpellTargeting Targeting { get; }

	// Para resistência elemental: por enquanto usamos o 1º elemento como "principal"
	public ElementType PrimaryElement => Elements[0];

	public SpellDefinition(string name, IReadOnlyList<ElementType> elements, int damage, SpellTargeting targeting)
	{
		Name = name;
		Elements = elements;
		Damage = damage;
		Targeting = targeting;
	}
}
