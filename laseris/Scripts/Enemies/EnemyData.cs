using Godot;

[GlobalClass]
public partial class EnemyData : Resource
{
	[ExportCategory("Core")]
	[Export] public string DisplayName = "Enemy";
	[Export] public int MaxHp = 100;
	[Export] public bool IsFlying = false;

	// ✅ Offset onde o VFX deve nascer (ex: cabeça)
	// Normalmente algo tipo (0, -30) ou (0, -50)
	[Export] public Vector2 VfxOffset = new Vector2(0, -30);

	[ExportCategory("Element Multipliers (1 = normal)")]
	[Export] public float MultFire = 1f;
	[Export] public float MultIce = 1f;
	[Export] public float MultLightning = 1f;
	[Export] public float MultPoison = 1f;
	[Export] public float MultEarth = 1f;
	[Export] public float MultAir = 1f;
	[Export] public float MultLight = 1f;
	[Export] public float MultShadow = 1f;

	public float GetElementMultiplier(ElementType element)
	{
		return element switch
		{
			ElementType.Fire => MultFire,
			ElementType.Ice => MultIce,
			ElementType.Lightning => MultLightning,
			ElementType.Poison => MultPoison,
			ElementType.Earth => MultEarth,
			ElementType.Air => MultAir,
			ElementType.Light => MultLight,
			ElementType.Shadow => MultShadow,
			_ => 1f
		};
	}
}
