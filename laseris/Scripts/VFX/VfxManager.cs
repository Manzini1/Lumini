using Godot;

public partial class VfxManager : Node
{
	[Export] public SpellVfxBank Bank;

	[ExportCategory("Refs")]
	[Export] public NodePath MagePath;
	[Export] public NodePath TargetControllerPath;

	// Root para VFX de tela (CanvasLayer)
	[Export] public NodePath ScreenVfxRootPath;

	private Mage _mage;
	private TargetController _targetController;
	private CanvasLayer _screenVfxRoot;

	public override void _Ready()
	{
		_mage = GetNodeOrNull<Mage>(MagePath);
		_targetController = GetNodeOrNull<TargetController>(TargetControllerPath);
		_screenVfxRoot = GetNodeOrNull<CanvasLayer>(ScreenVfxRootPath);

		if (Bank == null)
			GD.PushWarning("VfxManager: Bank não setado.");

		if (_screenVfxRoot == null)
			GD.PushWarning("VfxManager: ScreenVfxRootPath não setado (ScreenTopLeft/Center pode ficar errado).");
	}

	public void PlayForSpell(SpellDefinition spell)
	{
		
		  GD.Print($"[VFX] PlayForSpell chamado: {spell?.Id}");
		
		if (spell == null || Bank == null) return;

		var entry = Bank.Get(spell.Id);
		GD.Print($"[VFX] Entry encontrada? {entry != null}");
		if (entry == null || entry.VfxScene == null)
		{
			GD.Print($"[VFX] Sem entry para '{spell.Id}'.");
			return;
		}

		var target = _targetController?.CurrentTarget;
		var (parent, globalPos, isScreenSpace) = ResolveSpawn(entry, _mage, target);

		var vfx = entry.VfxScene.Instantiate<Node2D>();
		parent.AddChild(vfx);

		if (isScreenSpace)
		{
			// Screen-space: usa Position (local) e não GlobalPosition
			vfx.Position = globalPos + entry.Offset;
			return;
		}

		if (entry.FollowAnchor)
		{
			// se for filho do marker, posição local é só offset
			vfx.Position = entry.Offset;
		}
		else
		{
			// se for no mundo, usa posição global
			vfx.GlobalPosition = globalPos + entry.Offset;
		}
	}

	private (Node parent, Vector2 pos, bool isScreenSpace) ResolveSpawn(SpellVfxEntry entry, Mage mage, Enemy target)
	{
		// default: mundo
		Node parent = GetTree().CurrentScene;
		Vector2 pos = Vector2.Zero;
		bool isScreen = false;

		Marker2D GetMarker(Node n, string name) => n?.GetNodeOrNull<Marker2D>(name);

		switch (entry.SpawnPoint)
		{
			case SpellSpawnPoint.CasterCastPoint:
			{
				var m = GetMarker(mage, "VfxCast");
				if (m != null)
				{
					if (entry.FollowAnchor) return (m, m.GlobalPosition, false);
					pos = m.GlobalPosition;
				}
				break;
			}

			case SpellSpawnPoint.TargetHead:
			{
				var m = GetMarker(target, "VfxHead");
				if (m != null)
				{
					if (entry.FollowAnchor) return (m, m.GlobalPosition, false);
					pos = m.GlobalPosition;
				}
				break;
			}

			case SpellSpawnPoint.TargetGround:
			{
				var m = GetMarker(target, "VfxGround");
				if (m != null)
				{
					if (entry.FollowAnchor) return (m, m.GlobalPosition, false);
					pos = m.GlobalPosition;
				}
				break;
			}

			case SpellSpawnPoint.TargetCenter:
			default:
			{
				var m = GetMarker(target, "VfxCenter");
				if (m != null)
				{
					if (entry.FollowAnchor) return (m, m.GlobalPosition, false);
					pos = m.GlobalPosition;
				}
				break;
			}

			case SpellSpawnPoint.ScreenTopLeft:
			{
				isScreen = true;
				parent = _screenVfxRoot;
				pos = new Vector2(40, 40);
				break;
			}

			case SpellSpawnPoint.ScreenCenter:
			{
				isScreen = true;
				parent = _screenVfxRoot;
				var vp = GetViewport().GetVisibleRect().Size;
				pos = vp * 0.5f;
				break;
			}
		}

		return (parent, pos, isScreen);
	}
}
