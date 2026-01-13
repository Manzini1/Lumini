using System.Collections.Generic;

public static class SpellResolver
{
	public static SpellDefinition Resolve(IReadOnlyList<ElementType> castElements)
	{
		// Segurança
		if (castElements == null || castElements.Count == 0)
			return new SpellDefinition("none", "No Spell", new List<ElementType> { ElementType.Fire }, 0, SpellTargeting.None);

		// Se vier mais de 2 (futuro), por enquanto usamos só os 2 primeiros
		if (castElements.Count >= 2)
		{
			var a = castElements[0];
			var b = castElements[1];

			(a, b) = NormalizePair(a, b);

			// repetição (se você decidir permitir no futuro)
			if (a == b)
			{
				string id = $"combo_{ToId(a)}_{ToId(b)}";
				string name = $"Combo {a}+{b}";
				return new SpellDefinition(id, name, new List<ElementType> { a, b }, 35, PickDualTargeting(a, b));
			}

			// 28 combos únicos (id estável)
			string comboId = $"combo_{ToId(a)}_{ToId(b)}";
			string comboName = $"Combo {a}+{b}";

			return new SpellDefinition(comboId, comboName, new List<ElementType> { a, b }, 30, PickDualTargeting(a, b));
		}

		// 1 elemento
		{
			var e = castElements[0];
			return e switch
			{
				ElementType.Earth => new SpellDefinition("earth", "Stone Spike", castElements, 20, SpellTargeting.Ground),
				ElementType.Air => new SpellDefinition("air", "Gust Shot", castElements, 20, SpellTargeting.Air),
				ElementType.Fire => new SpellDefinition("fireball", "Fire Bolt", castElements, 20, SpellTargeting.Both),
				ElementType.Ice => new SpellDefinition("ice", "Ice Shard", castElements, 20, SpellTargeting.Both),
				ElementType.Lightning => new SpellDefinition("lightning", "Arc Zap", castElements, 20, SpellTargeting.Both),
				ElementType.Poison => new SpellDefinition("poison", "Toxic Dart", castElements, 20, SpellTargeting.Both),
				ElementType.Light => new SpellDefinition("light", "Radiant Ray", castElements, 20, SpellTargeting.Both),
				ElementType.Shadow => new SpellDefinition("shadow", "Shadow Needle", castElements, 20, SpellTargeting.Both),
				_ => new SpellDefinition("spell_unknown", "Mystic Hit", castElements, 20, SpellTargeting.Both)
			};
		}
	}

	// ---------- Helpers ----------

	private static (ElementType, ElementType) NormalizePair(ElementType a, ElementType b)
	{
		return (int)a <= (int)b ? (a, b) : (b, a);
	}

	private static SpellTargeting PickDualTargeting(ElementType a, ElementType b)
	{
		bool hasEarth = (a == ElementType.Earth || b == ElementType.Earth);
		bool hasAir = (a == ElementType.Air || b == ElementType.Air);

		if (hasEarth && !hasAir) return SpellTargeting.Ground;
		if (hasAir && !hasEarth) return SpellTargeting.Air;
		return SpellTargeting.Both;
	}

	private static string ToId(ElementType e)
	{
		// mantém ids pequenos e consistentes
		return e switch
		{
			ElementType.Fire => "fire",
			ElementType.Ice => "ice",
			ElementType.Lightning => "lightning",
			ElementType.Poison => "poison",
			ElementType.Earth => "earth",
			ElementType.Air => "air",
			ElementType.Light => "light",
			ElementType.Shadow => "shadow",
			_ => "unknown"
		};
	}
}
