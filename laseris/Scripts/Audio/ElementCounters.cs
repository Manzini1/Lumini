public static class ElementCounters
{
	// Se o shield é X, o jogador precisa apertar CounterOf(X).
	public static ElementType CounterOf(ElementType shield)
	{
		return shield switch
		{
			// ciclo 1
			ElementType.Earth => ElementType.Fire,
			ElementType.Fire => ElementType.Ice,
			ElementType.Ice => ElementType.Lightning,
			ElementType.Lightning => ElementType.Earth,

			// ciclo 2
			ElementType.Light => ElementType.Shadow,
			ElementType.Shadow => ElementType.Poison,
			ElementType.Poison => ElementType.Air,
			ElementType.Air => ElementType.Light,

			_ => ElementType.Fire
		};
	}
}
