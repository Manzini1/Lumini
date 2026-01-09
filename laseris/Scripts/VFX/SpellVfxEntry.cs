using Godot;

[GlobalClass]
public partial class SpellVfxEntry : Resource
{
	[ExportCategory("Identity")]
	[Export] public string SpellId = "";

	[ExportCategory("Prefab")]
	[Export] public PackedScene VfxScene;

	[ExportCategory("Animation (optional injection)")]
	[Export] public SpriteFrames Frames;
	[Export] public string AnimationName = "play";
	[Export(PropertyHint.Range, "0.1,4.0,0.05")]
	public float SpeedScale = 1.0f;

	[ExportCategory("Spawn")]
	[Export] public SpellSpawnPoint SpawnPoint = SpellSpawnPoint.TargetCenter;
	[Export] public bool FollowAnchor = false;
	[Export] public Vector2 Offset = Vector2.Zero;

	// ✅ voltou pra compatibilidade com seu VfxPlayer.cs
	[Export] public Vector2 ScreenMargin = new Vector2(40, 40);
	[ExportCategory("Hit Timing (TargetInstantSpellVfx)")]
	[Export(PropertyHint.Range, "0.0,5.0,0.01")]
	public float DamageDelaySeconds = 0.0f; // 0 = imediato (ou no final, depende do script)
	[ExportCategory("Render")]
	[Export] public Vector2 Scale = Vector2.One;
	[Export(PropertyHint.Range, "-180,180,1")]
	public float RotationDegrees = 0f;
	[Export] public int ZIndex = 50;

	[ExportCategory("Playback")]
	[Export] public bool AutoFreeOnFinish = true;
	[Export(PropertyHint.Range, "0.0,10.0,0.1")]
	public float FallbackLifetime = 1.2f;
}
