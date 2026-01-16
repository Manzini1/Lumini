using Godot;

[GlobalClass]
public partial class ElementCounterTable : Resource
{
	// “Counter de X” = qual elemento DEFENDE/QUEBRA X
	// Ex: se shield do inimigo é Fire, qual tecla o player deve apertar? -> CounterOfFire

	[ExportCategory("Counters")]
	[Export] public ElementType CounterOfFire = ElementType.Ice;        // seu “água”
	[Export] public ElementType CounterOfIce = ElementType.Lightning;
	[Export] public ElementType CounterOfLightning = ElementType.Earth;
	[Export] public ElementType CounterOfEarth = ElementType.Fire;      // seu exemplo: Terra -> Fogo
	[Export] public ElementType CounterOfAir = ElementType.Poison;
	[Export] public ElementType CounterOfPoison = ElementType.Light;
	[Export] public ElementType CounterOfLight = ElementType.Shadow;
	[Export] public ElementType CounterOfShadow = ElementType.Air;

	public ElementType GetCounter(ElementType element)
	{
		return element switch
		{
			ElementType.Fire => CounterOfFire,
			ElementType.Ice => CounterOfIce,
			ElementType.Lightning => CounterOfLightning,
			ElementType.Earth => CounterOfEarth,
			ElementType.Air => CounterOfAir,
			ElementType.Poison => CounterOfPoison,
			ElementType.Light => CounterOfLight,
			ElementType.Shadow => CounterOfShadow,
			_ => ElementType.Fire
		};
	}
}
