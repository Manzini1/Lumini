using Godot;
using System;
using System.Collections.Generic;

public partial class ShieldVfxManager : Node
{
	[Export] public ShieldOrbBank Bank;

	[ExportCategory("Aura Scene")]
	[Export] public PackedScene AuraScene; // ShieldAuraVfx.tscn
	[Export] public string AnchorMarkerName = "VfxCenter";
	[Export] public Vector2 AuraOffset = Vector2.Zero;
	[ExportCategory("Aura")]
	[Export] public int AuraZIndex = 50;

	[ExportCategory("Dual Layout")]
	[Export] public Vector2 DualOffsetA = new Vector2(-20, -10);
	[Export] public Vector2 DualOffsetB = new Vector2( 20, -10);

	[ExportCategory("Reactions")]
	[Export] public PackedScene ReactionScene; // pode ser GenericSpellVfx.tscn
	[Export] public SpriteFrames AbsorbFrames;
	[Export] public string AbsorbAnimName = "play";
	[Export] public float AbsorbSpeedScale = 1.0f;

	[Export] public SpriteFrames BlockFrames;
	[Export] public string BlockAnimName = "play";
	[Export] public float BlockSpeedScale = 1.0f;

	// enemy instance id -> auras
	private readonly Dictionary<ulong, List<Node2D>> _aurasByEnemy = new();

	public override void _Ready()
	{
		AddToGroup("shield_vfx_manager");
		GD.Print($"[ShieldVfxManager] Ready. bank={(Bank != null)} auraScene={(AuraScene != null)}");
	}

	public void SetShieldElements(Node2D enemy, IReadOnlyList<ElementType> elements)
	{
		if (enemy == null || !GodotObject.IsInstanceValid(enemy)) return;

		ClearAuras(enemy);

		if (elements == null || elements.Count == 0) return;
		if (AuraScene == null || Bank == null) return;

		var anchor = GetAnchor(enemy);
		var list = new List<Node2D>();

		// 1 elemento
		if (elements.Count == 1)
		{
			var aura = SpawnAura(anchor, elements[0], AuraOffset);
			if (aura != null) list.Add(aura);
		}
		// 2 elementos
		else
		{
			var auraA = SpawnAura(anchor, elements[0], DualOffsetA);
			var auraB = SpawnAura(anchor, elements[1], DualOffsetB);
			if (auraA != null) list.Add(auraA);
			if (auraB != null) list.Add(auraB);
		}

		_aurasByEnemy[enemy.GetInstanceId()] = list;
	}

	public void PlayReaction(Node2D enemy, CastOutcome outcome)
	{
		if (enemy == null || !GodotObject.IsInstanceValid(enemy)) return;

		bool isAbsorb = outcome == CastOutcome.Absorbed50 || outcome == CastOutcome.Absorbed100;
		bool isBlock  = outcome == CastOutcome.Blocked;

		if (!isAbsorb && !isBlock) return;
		if (ReactionScene == null) return;

		var roots = GetTree().GetNodesInGroup("vfx_root");
		var parent = (roots.Count > 0) ? roots[0] as Node : GetTree().CurrentScene;

		var fx = ReactionScene.Instantiate<Node2D>();
		parent.AddChild(fx);

		var anchor = GetAnchor(enemy);
		fx.GlobalPosition = anchor.GlobalPosition;

		// tenta configurar como GenericSpellVfx (se você estiver usando ele)
		if (fx is GenericSpellVfx g)
		{
			var frames = isAbsorb ? AbsorbFrames : BlockFrames;
			var anim   = isAbsorb ? AbsorbAnimName : BlockAnimName;
			var speed  = isAbsorb ? AbsorbSpeedScale : BlockSpeedScale;

			var tmp = new SpellVfxEntry
			{
				Frames = frames,
				AnimationName = anim,
				SpeedScale = speed,
				ZIndex = 80
			};

			g.Configure(tmp, null, null);
		}
	}

	private Node2D GetAnchor(Node2D enemy)
	{
		var m = enemy.GetNodeOrNull<Marker2D>(AnchorMarkerName);
		return (Node2D)(m ?? enemy);
	}

	private Node2D SpawnAura(Node2D anchor, ElementType element, Vector2 localOffset)
	{
		var entry = Bank.Get(element);
		if (entry == null || entry.Frames == null)
		{
			GD.PushWarning($"[ShieldVFX] Sem entry/frames para elemento '{element}'.");
			return null;
		}

		var auraNode = AuraScene.Instantiate<Node2D>();
		anchor.AddChild(auraNode);
		auraNode.Position = localOffset; // local ao anchor

		if (auraNode is ShieldAuraVfx aura)
		{
			aura.Configure(entry.Frames, entry.AnimationName, entry.SpeedScale, AuraZIndex);
		}

		return auraNode;
	}

	private void ClearAuras(Node2D enemy)
	{
		var id = enemy.GetInstanceId();
		if (!_aurasByEnemy.TryGetValue(id, out var list)) return;

		foreach (var n in list)
			if (n != null && GodotObject.IsInstanceValid(n))
				n.QueueFree();

		_aurasByEnemy.Remove(id);
	}
}
