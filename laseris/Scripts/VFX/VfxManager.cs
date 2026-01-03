using Godot;

public partial class VfxManager : Node
{
	[Export] public SpellVfxBank Bank;

	public override void _Ready()
	{
		GD.Print($"[VfxManager] Ready. bank={(Bank != null)}");
	}

	public IVfxPlayable PlaySpell(SpellDefinition spell, Node2D caster, Node2D target)
	{
		GD.Print($"[VfxManager] PlaySpell called. spell={spell?.Id ?? "NULL"}");

		if (spell == null || Bank == null) return null;

		var entry = Bank.Get(spell.Id);
		if (entry == null || entry.VfxScene == null)
		{
			GD.PushWarning($"[VfxManager] Sem entry ou VfxScene para '{spell.Id}'.");
			return null;
		}

		var roots = GetTree().GetNodesInGroup("vfx_root");
		var parent = (roots.Count > 0) ? roots[0] as Node : GetTree().CurrentScene;

		var vfxNode = entry.VfxScene.Instantiate<Node2D>();
		parent.AddChild(vfxNode);

		vfxNode.GlobalPosition = ResolveSpawnPos(entry, caster, target);
		vfxNode.ZIndex = entry.ZIndex;

		if (vfxNode is IVfxPlayable playable)
		{
			playable.Configure(entry, caster, target);
			return playable;
		}

		// fallback: se algum VFX específico não implementar interface ainda
		GD.PushWarning($"[VfxManager] VFX '{vfxNode.Name}' não implementa IVfxPlayable. Dano será instantâneo.");
		return null;
	}

	private Vector2 ResolveSpawnPos(SpellVfxEntry entry, Node2D caster, Node2D target)
	{
		switch (entry.SpawnPoint)
		{
			case SpellSpawnPoint.CasterCastPoint:
			{
				if (caster != null)
				{
					var m = caster.GetNodeOrNull<Marker2D>("VfxCast");
					if (m != null) return m.GlobalPosition + entry.Offset;
					return caster.GlobalPosition + entry.Offset;
				}
				break;
			}
			default:
			{
				if (target != null)
				{
					var m = target.GetNodeOrNull<Marker2D>("VfxCenter");
					if (m != null) return m.GlobalPosition + entry.Offset;
					return target.GlobalPosition + entry.Offset;
				}
				break;
			}
		}

		return entry.Offset;
	}
}
