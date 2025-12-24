using Godot;

[GlobalClass]
public partial class SpellVfxEntry : Resource
{
	[ExportCategory("Identity")]
	[Export] public string SpellId = ""; // tem que bater com SpellDefinition.Id

	[ExportCategory("Prefab")]
	[Export] public PackedScene VfxScene; // vai ser SEMPRE o GenericSpellVfx.tscn

	[ExportCategory("Animation")]
	[Export] public SpriteFrames Frames;     // frames específicos dessa magia
	[Export] public string Animation = "play";
	[Export] public float SpeedScale = 1.0f;

	[ExportCategory("Spawn")]
	[Export] public SpellSpawnPoint SpawnPoint = SpellSpawnPoint.TargetCenter;
	[Export] public bool FollowAnchor = true; // se true, vira filho do Marker (segue alvo)
	[Export] public Vector2 Offset = Vector2.Zero;

	// ✅ Isso aqui resolve seu erro do CS1061
	[Export] public Vector2 ScreenMargin = new Vector2(40, 40);
}
