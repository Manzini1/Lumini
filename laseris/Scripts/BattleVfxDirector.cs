using Godot;
using Game.Combat;

namespace Game.Battle;

public partial class BattleVfxDirector : Node
{
	[ExportGroup("Refs")]
	[Export] public NodePath ElementVfxLibraryPath = "Systems/ElementVfxLibrary";
	[Export] public NodePath WorldVfxParentPath = "World/Vfx";
	[Export] public NodePath ProjectilesParentPath = "World/Projectiles";

	[ExportGroup("Sockets")]
	[Export] public NodePath MageCastPath = "World/Characters/Mage/VfxCast";     // Marker2D
	[Export] public NodePath EnemyHitPath = "World/Characters/Enemy/HitPoint";  // Marker2D (ou VfxCenter)

	[ExportGroup("Timing")]
	[Export] public float DefaultProjectileTravel = 0.06f;

	private ElementVfxLibrary _lib;
	private Node _worldVfxParent;
	private Node _projectilesParent;
	private Marker2D _mageCast;
	private Node2D _enemyHit;

	public override void _Ready()
	{
		_lib = GetNodeOrNull<ElementVfxLibrary>(ElementVfxLibraryPath);
		_worldVfxParent = GetNodeOrNull<Node>(WorldVfxParentPath);
		_projectilesParent = GetNodeOrNull<Node>(ProjectilesParentPath);

		_mageCast = GetNodeOrNull<Marker2D>(MageCastPath);
		_enemyHit = GetNodeOrNull<Node2D>(EnemyHitPath);

		if (_lib == null) GD.PushWarning("[VfxDirector] ElementVfxLibrary not found.");
		if (_worldVfxParent == null) GD.PushWarning("[VfxDirector] WorldVfxParent not found.");
		if (_projectilesParent == null) GD.PushWarning("[VfxDirector] ProjectilesParent not found.");
	}

	private Vector2 MageCastPos => _mageCast != null ? _mageCast.GlobalPosition : Vector2.Zero;
	private Vector2 EnemyHitPos => _enemyHit != null ? _enemyHit.GlobalPosition : Vector2.Zero;

	public void PlayPlayerCast(int elementId, bool flowFull, float travelSec = -1f)
	{
		if (_lib == null || _projectilesParent == null) return;

		float t = travelSec > 0 ? travelSec : DefaultProjectileTravel;
		Vector2 from = MageCastPos;
		Vector2 to = EnemyHitPos;

		// ✅ tenta advanced (se flowFull), senão cai no normal automaticamente
		bool spawned = _lib.SpawnPlayerCast(elementId, flowFull, _projectilesParent, from, to, t);

		// fallback final (caso o SpawnPlayerCast retorne false por algum motivo)
		if (!spawned)
			_lib.SpawnCastProjectile(elementId, _projectilesParent, from, to, t);
	}

	public void PlayImpactOnEnemy(int elementId)
	{
		if (_lib == null || _worldVfxParent == null) return;
		_lib.SpawnAttackImpactRandom(elementId, _worldVfxParent, EnemyHitPos);
	}

	public void PlayDeflectImpactOnEnemy(int elementId)
	{
		PlayImpactOnEnemy(elementId);
	}
}
