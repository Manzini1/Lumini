using System.Collections.Generic;

public static class SpellResolver
{
	public static SpellDefinition Resolve(IReadOnlyList<ElementType> castElements)
	{
		// Segurança
		if (castElements == null || castElements.Count == 0)
			return new SpellDefinition("No Spell", new List<ElementType> { ElementType.Fire }, 0, SpellTargeting.None);

		// 1 elemento
		if (castElements.Count == 1)
		{
			var e = castElements[0];
			return e switch
			{
				ElementType.Earth => new SpellDefinition("Stone Spike", castElements, 20, SpellTargeting.Ground),
				ElementType.Air => new SpellDefinition("Gust Shot", castElements, 20, SpellTargeting.Air),
				ElementType.Fire => new SpellDefinition("Fire Bolt", castElements, 20, SpellTargeting.Both),
				ElementType.Ice => new SpellDefinition("Ice Shard", castElements, 20, SpellTargeting.Both),
				ElementType.Lightning => new SpellDefinition("Arc Zap", castElements, 20, SpellTargeting.Both),
				ElementType.Poison => new SpellDefinition("Toxic Dart", castElements, 20, SpellTargeting.Both),
				ElementType.Light => new SpellDefinition("Radiant Ray", castElements, 20, SpellTargeting.Both),
				ElementType.Shadow => new SpellDefinition("Shadow Needle", castElements, 20, SpellTargeting.Both),
				_ => new SpellDefinition("Mystic Hit", castElements, 20, SpellTargeting.Both)
			};
		}

		// 2 elementos (base por enquanto)
		// Você vai customizar depois: name, damage e targeting por combinação
		// Exemplo: Earth+Air poderia virar "Dust Cyclone" (Both), etc.
		return new SpellDefinition("Dual Spell", castElements, 30, SpellTargeting.Both);
	}
}
