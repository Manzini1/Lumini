using Godot;

[GlobalClass]
public partial class SpellVfxEntry : Resource
{
	[ExportCategory("Identity")]
	[Export] public string SpellId = "";

	[ExportCategory("Prefab")]
	[Export] public PackedScene VfxScene;

	[ExportCategory("Animation (GenericSpellVfx)")]
	[Export] public SpriteFrames Frames;
	[Export] public string AnimationName = "play";
	[Export(PropertyHint.Range, "0.1,4.0,0.05")]
	public float SpeedScale = 1.0f;

	[ExportCategory("Spawn")]
	[Export] public SpellSpawnPoint SpawnPoint = SpellSpawnPoint.TargetCenter;
	[Export] public bool FollowAnchor = true;
	[Export] public Vector2 Offset = Vector2.Zero;
	[Export] public Vector2 ScreenMargin = new Vector2(40, 40);

	[ExportCategory("Playback")]
	[Export] public bool AutoFreeOnFinish = true;
	[Export(PropertyHint.Range, "0.0,10.0,0.1")]
	public float FallbackLifetime = 1.2f;

	[ExportCategory("Render")]
	[Export] public int ZIndex = 50; // ✅ default alto pra não sumir atrás de nada
}
