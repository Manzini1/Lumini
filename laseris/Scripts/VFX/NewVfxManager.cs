//using Godot;
//
//public partial class NewVfxManager : Node
//{
	//[Export] public SpellVfxBank Bank;
//
	//public override void _Ready()
	//{
		//if (Bank == null)
			//GD.PushWarning("[VfxManager] Bank não setado.");
	//}
//
	///// <summary>
	/// Toca um VFX de spell no mundo.
	///// </summary>
	//public void PlaySpell(SpellDefinition spell, Node2D caster, Node2D target = null)
	//{
		//if (spell == null || Bank == null) return;
//
		//var entry = Bank.Get(spell.Id);
		//if (entry == null || entry.VfxScene == null) return;
//
		//// 1) resolve root (onde colocar VFX)
		//var vfxRoot = FindVfxRoot();
		//if (vfxRoot == null)
		//{
			//GD.PushWarning("[VfxManager] Não achei VfxRoot (grupo 'vfx_root').");
			//return;
		//}
//
		//// 2) instancia
		//var vfx = entry.VfxScene.Instantiate<Node2D>();
//
		//// 3) resolve spawn
		//var (parent, pos) = ResolveSpawn(entry, caster, target, vfxRoot);
		//parent.AddChild(vfx);
		//vfx.GlobalPosition = pos + entry.Offset;
//
		//// 4) tenta tocar e auto-destruir (fallback simples)
		//TryPlayAndAutoFree(vfx, spell.Id);
	//}
//
	//private Node2D FindVfxRoot()
	//{
		//var nodes = GetTree().GetNodesInGroup("vfx_root");
		//if (nodes.Count == 0) return null;
		//return nodes[0] as Node2D;
	//}
//
	//private (Node parent, Vector2 globalPos) ResolveSpawn(
		//SpellVfxEntry entry,
		//Node2D caster,
		//Node2D target,
		//Node2D vfxRoot)
	//{
		//Node parent = vfxRoot;
		//Vector2 pos = caster?.GlobalPosition ?? Vector2.Zero;
//
		//switch (entry.SpawnPoint)
		//{
			//case SpellSpawnPoint.CasterCastPoint:
				//pos = caster?.GlobalPosition ?? Vector2.Zero; // por enquanto, simples
				//break;
//
			//case SpellSpawnPoint.TargetCenter:
				//pos = target?.GlobalPosition ?? pos;
				//break;
//
			//// por enquanto vamos suportar só esses 2, depois a gente evolui
		//}
//
		//return (parent, pos);
	//}
//
	//private void TryPlayAndAutoFree(Node2D vfx, string animName)
	//{
		//// padrão: AnimatedSprite2D no root ou em filho
		//var sprite = vfx.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D")
				  //?? vfx.GetNodeOrNull<AnimatedSprite2D>("%AnimatedSprite2D");
//
		//if (sprite == null)
		//{
			//// sem sprite, só destrói depois de 1s pra não vazar
			//var t = GetTree().CreateTimer(1.0);
			//t.Timeout += () => { if (IsInstanceValid(vfx)) vfx.QueueFree(); };
			//return;
		//}
//
		//// toca animação ou default
		//if (sprite.SpriteFrames != null && sprite.SpriteFrames.HasAnimation(animName))
			//sprite.Play(animName);
		//else
			//sprite.Play("default");
//
		//sprite.AnimationFinished += () =>
		//{
			//if (IsInstanceValid(vfx)) vfx.QueueFree();
		//};
	//}
//}
