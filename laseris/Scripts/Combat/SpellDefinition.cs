using System.Collections.Generic;

public sealed class SpellDefinition
{
	// ID estável (para áudio/VFX/lookup). Ex: "combo_fire_ice"
	public string Id { get; }

	// Nome que aparece pro jogador. Você pode mudar sem quebrar nada.
	public string Name { get; }

	public IReadOnlyList<ElementType> Elements { get; }
	public int Damage { get; }
	public SpellTargeting Targeting { get; }

	// Para resistência elemental: por enquanto usamos o 1º elemento como "principal"
	public ElementType PrimaryElement => Elements[0];

	public SpellDefinition(string id, string name, IReadOnlyList<ElementType> elements, int damage, SpellTargeting targeting)
	{
		Id = id;
		Name = name;
		Elements = elements;
		Damage = damage;
		Targeting = targeting;
	}
}
