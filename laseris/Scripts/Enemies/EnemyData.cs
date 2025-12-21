using Godot;

[GlobalClass]
public partial class EnemyData : Resource
{
	[Export] public string DisplayName = "Enemy";

	[Export] public int MaxHp = 100;

	[Export] public bool IsFlying = false;

	// Resistência a status (0 = não resiste, 1 = imune)
	[Export(PropertyHint.Range, "0,1,0.05")]
	public float FreezeResist = 0.0f;

	[Export(PropertyHint.Range, "0,1,0.05")]
	public float PetrifyResist = 0.0f;

	// Multiplicadores elementais (1 = normal, 0.5 = resiste, 1.5 = fraco)
	[Export] public float FireMultiplier = 1.0f;
	[Export] public float IceMultiplier = 1.0f;
	[Export] public float LightningMultiplier = 1.0f;
	[Export] public float PoisonMultiplier = 1.0f;
	[Export] public float EarthMultiplier = 1.0f;
	[Export] public float AirMultiplier = 1.0f;
	[Export] public float LightMultiplier = 1.0f;
	[Export] public float ShadowMultiplier = 1.0f;

	public float GetElementMultiplier(ElementType element)
	{
		return element switch
		{
			ElementType.Fire => FireMultiplier,
			ElementType.Ice => IceMultiplier,
			ElementType.Lightning => LightningMultiplier,
			ElementType.Poison => PoisonMultiplier,
			ElementType.Earth => EarthMultiplier,
			ElementType.Air => AirMultiplier,
			ElementType.Light => LightMultiplier,
			ElementType.Shadow => ShadowMultiplier,
			_ => 1.0f
		};
	}
}
