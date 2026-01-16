//using Godot;
//
//public partial class VfxManager : Node
//{
	//[Export] public SpellVfxBank Bank;
//
	//[ExportCategory("Roots")]
	//[Export] public string VfxRootGroup = "vfx_root";
//
	//public override void _Ready()
	//{
		//GD.Print($"[VfxManager] Ready. bank={(Bank != null)}");
	//}
//
	//public IVfxPlayable PlaySpell(SpellDefinition spell, Mage mage, Enemy target)
	//{
		//if (spell == null || Bank == null) return null;
//
		//var entry = Bank.Get(spell.Id);
		//if (entry == null || entry.VfxScene == null)
		//{
			//GD.PushWarning($"[VfxManager] entry null ou sem VfxScene p/ spell '{spell?.Id}'");
			//return null;
		//}
//
		//var vfxNode = entry.VfxScene.Instantiate<Node2D>();
//
		//// parent + posição base (SEM offset aqui)
		//var (parent, baseGlobalPos, anchor) = ResolveSpawn(entry, mage, target);
		//parent ??= GetTree().CurrentScene;
//
		//parent.AddChild(vfxNode);
//
		//// ✅ transform aplicado UMA vez, sempre aqui
		//vfxNode.ZIndex = entry.ZIndex;
		//vfxNode.Scale = entry.Scale;
		//vfxNode.Rotation = Mathf.DegToRad(entry.RotationDegrees);
//
		//if (anchor != null && entry.FollowAnchor)
		//{
			//// sendo filho do marker: Position local
			//vfxNode.Position = entry.Offset;
		//}
		//else
		//{
			//// não seguindo: global pos
			//vfxNode.GlobalPosition = baseGlobalPos + entry.Offset;
		//}
//
		//// ✅ configure depois de AddChild: _Ready já rodou, mas o nosso Instant/Projectile
		//// é seguro com qualquer ordem (e o Instant vai disparar Impacted no próximo frame).
		//if (vfxNode is ISpellVfxConfigurable cfg)
			//cfg.Configure(entry, mage, target);
//
		//return vfxNode as IVfxPlayable;
	//}
//
	//private (Node parent, Vector2 baseGlobalPos, Marker2D anchor) ResolveSpawn(SpellVfxEntry entry, Mage mage, Enemy target)
	//{
		//Node root = GetVfxRoot() ?? GetTree().CurrentScene;
//
		//Marker2D GetMarker(Node n, string name) => n?.GetNodeOrNull<Marker2D>(name);
//
		//Marker2D anchor = null;
		//Vector2 pos = Vector2.Zero;
//
		//switch (entry.SpawnPoint)
		//{
			//case SpellSpawnPoint.CasterCastPoint:
				//anchor = GetMarker(mage, "VfxCast");
				//pos = anchor != null ? anchor.GlobalPosition : (mage != null ? mage.GlobalPosition : Vector2.Zero);
				//break;
//
			//case SpellSpawnPoint.TargetHead:
				//anchor = GetMarker(target, "VfxHead");
				//pos = anchor != null ? anchor.GlobalPosition : (target != null ? target.GlobalPosition : Vector2.Zero);
				//break;
//
			//case SpellSpawnPoint.TargetGround:
				//anchor = GetMarker(target, "VfxGround");
				//pos = anchor != null ? anchor.GlobalPosition : (target != null ? target.GlobalPosition : Vector2.Zero);
				//break;
//
			//case SpellSpawnPoint.TargetCenter:
			//default:
				//anchor = GetMarker(target, "VfxCenter");
				//pos = anchor != null ? anchor.GlobalPosition : (target != null ? target.GlobalPosition : Vector2.Zero);
				//break;
		//}
//
		//if (entry.FollowAnchor && anchor != null)
			//return (anchor, anchor.GlobalPosition, anchor);
//
		//return (root, pos, null);
	//}
//
	//private Node GetVfxRoot()
	//{
		//var roots = GetTree().GetNodesInGroup(VfxRootGroup);
		//return (roots != null && roots.Count > 0) ? roots[0] as Node : null;
	//}
//}
