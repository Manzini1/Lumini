using Godot;

[GlobalClass]
public partial class SpellVfxEntry : Resource
{
	// ============================================================
	// Identity / Prefab
	// ============================================================
	[ExportCategory("Identity")]
	[Export] public string SpellId = "";

	[ExportCategory("Prefab")]
	[Export] public PackedScene VfxScene;

	// ============================================================
	// Animation (GenericSpellVfx / TargetInstant)
	// ============================================================
	[ExportCategory("Animation (GenericSpellVfx / TargetInstant)")]
	[Export] public SpriteFrames Frames;
	[Export] public string AnimationName = "play";
	[Export(PropertyHint.Range, "0.1,4.0,0.05")]
	public float SpeedScale = 1.0f;

	// ============================================================
	// Spawn
	// ============================================================
	[ExportCategory("Spawn")]
	[Export] public SpellSpawnPoint SpawnPoint = SpellSpawnPoint.TargetCenter;
	[Export] public bool FollowAnchor = true;
	[Export] public Vector2 Offset = Vector2.Zero;

	// (se você tiver lógica de clamp na tela no VfxPlayer/VfxManager, mantém isso aqui)
	[Export] public Vector2 ScreenMargin = new Vector2(40, 40);

	// ============================================================
	// Render
	// ============================================================
	[ExportCategory("Render")]
	[Export] public int ZIndex = 50;
	[Export] public Vector2 Scale = Vector2.One;
	[Export] public float RotationDegrees = 0f;

	// ============================================================
	// Playback
	// ============================================================
	[ExportCategory("Playback")]
	[Export] public bool AutoFreeOnFinish = true;
	[Export(PropertyHint.Range, "0.0,10.0,0.1")]
	public float FallbackLifetime = 1.2f;

	// ============================================================
	// TargetInstant Timing (quando o dano acontece)
	// ============================================================
	[ExportCategory("TargetInstant Timing")]
	[Export(PropertyHint.Range, "-1,10,0.05")]
	public float DamageDelaySeconds = 0.0f;
	//  0  => dano quase imediato (seguro via deferred/frame no script)
	// >0  => dano depois desse tempo
	// -1  => dano no fim da animação (AnimationFinished)

	// ============================================================
	// Impact Override (ProjectileSpellVfx)
	// ============================================================
	[ExportCategory("Impact Override (ProjectileSpellVfx)")]
	[Export] public bool UseCustomImpact = false;

	[Export] public PackedScene ImpactScene;       // se null, cai no default do script
	[Export] public SpriteFrames ImpactFrames;     // opcional (pra GenericSpellVfx)
	[Export] public string ImpactAnimName = "play";
	[Export(PropertyHint.Range, "0.1,4.0,0.05")]
	public float ImpactSpeedScale = 1.0f;

	[Export] public Vector2 ImpactOffset = Vector2.Zero;
	[Export] public Vector2 ImpactScale = Vector2.One;
	[Export] public int ImpactZIndex = 999;

	// ============================================================
	// Secondary Impact (TargetInstant extra hit/flash por entry)
	// ============================================================
	[ExportCategory("TargetInstant Secondary Impact")]
	[Export] public bool UseSecondaryImpact = false;

	[Export] public PackedScene SecondaryImpactScene;   // se null, não spawna
	[Export] public SpriteFrames SecondaryImpactFrames; // opcional (se a cena for GenericSpellVfx)
	[Export] public string SecondaryImpactAnimName = "play";
	[Export(PropertyHint.Range, "0.1,4.0,0.05")]
	public float SecondaryImpactSpeedScale = 1.0f;

	[Export(PropertyHint.Range, "0,10,0.05")]
	public float SecondaryImpactDelaySeconds = 0.0f;

	// ajuste fino pra “descer um pouco” só nessa skill
	[Export] public Vector2 SecondaryImpactOffset = Vector2.Zero;

	[Export] public Vector2 SecondaryImpactScale = Vector2.One;
	[Export] public int SecondaryImpactZIndex = 999;
}
