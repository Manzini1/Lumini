using Godot;
using System.Collections.Generic;

public partial class VfxPlayer : Node
{
	[Export] public SpellVfxBank Bank;

	[ExportCategory("Refs")]
	[Export] public NodePath MagePath;
	[Export] public NodePath TargetControllerPath;

	private Mage _mage;
	private TargetController _targetController;

	public override void _Ready()
	{
		_mage = GetNodeOrNull<Mage>(MagePath);
		_targetController = GetNodeOrNull<TargetController>(TargetControllerPath);

		if (Bank == null) GD.PushWarning("VfxPlayer: Bank não setado.");
		if (_mage == null) GD.PushWarning("VfxPlayer: MagePath não setado/encontrado.");
		if (_targetController == null) GD.PushWarning("VfxPlayer: TargetControllerPath não setado/encontrado.");
	}

public void PlaySpell(SpellDefinition spell)
{
	if (spell == null || Bank == null)
		return;

	GD.Print($"[VFX] SpellId recebido: {spell.Id}");

	var entry = Bank.Get(spell.Id);
	GD.Print($"[VFX] Entry encontrada? {entry != null}");

	if (entry == null || entry.VfxScene == null)
		return;

	var target = _targetController?.CurrentTarget;

	// instancia o VFX
	var vfx = entry.VfxScene.Instantiate<Node2D>();

	// resolve onde spawnar
	var (parent, globalPos) = ResolveSpawn(entry, _mage, target);
	parent.AddChild(vfx);

	// posiciona
	vfx.GlobalPosition = globalPos + entry.Offset;

	// escolhe animação pelo ID
	var sprite = vfx.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
	if (sprite == null)
	{
		GD.PushWarning("[VFX] AnimatedSprite2D não encontrado no VFX.");
		return;
	}

	if (sprite.SpriteFrames.HasAnimation(spell.Id))
	{
		sprite.Play(spell.Id);
	}
	else
	{
		GD.PushWarning($"[VFX] Animação '{spell.Id}' não existe. Usando default.");
		sprite.Play("default");
	}
}


	private (Node parent, Vector2 globalPos) ResolveSpawn(SpellVfxEntry entry, Mage mage, Enemy target)
	{
		Node parent = GetTree().CurrentScene;
		Vector2 pos = Vector2.Zero;
			GD.Print($"[VFX SPAWN] {entry.SpawnPoint} → pos={pos}");
		Marker2D GetMarker(Node n, string name) => n?.GetNodeOrNull<Marker2D>(name);
		GD.Print($"[VFX DEBUG] Target = {target?.Name ?? "NULL"}");
		if (target != null)
			GD.Print($"[VFX DEBUG] Target.GlobalPosition = {target.GlobalPosition}");
		switch (entry.SpawnPoint)
		{
			case SpellSpawnPoint.ScreenTopAlignedToTarget:
			{
				if (target == null)
				{
					GD.PushWarning("[VFX] ScreenTopAlignedToTarget sem target válido.");
					break;
				}

				float x = target.GlobalPosition.X;

				// ⚠️ coordenada de mundo, não viewport
				var cam = GetViewport().GetCamera2D();
				float y = cam != null
					? cam.GlobalPosition.Y - GetViewport().GetVisibleRect().Size.Y * 0.5f
					: 0f;

				pos = new Vector2(x, y);
				break;
			}

			case SpellSpawnPoint.CasterCastPoint:
			{
				var m = GetMarker(mage, "VfxCast");
				if (m != null)
				{
					if (entry.FollowAnchor) return (m, m.GlobalPosition);
					pos = m.GlobalPosition;
				}
				break;
			}

			case SpellSpawnPoint.TargetHead:
			{
				var m = GetMarker(target, "VfxHead");
				if (m != null)
				{
					if (entry.FollowAnchor) return (m, m.GlobalPosition);
					pos = m.GlobalPosition;
				}
				break;
			}

			case SpellSpawnPoint.TargetGround:
			{
				var m = GetMarker(target, "VfxGround");
				if (m != null)
				{
					if (entry.FollowAnchor) return (m, m.GlobalPosition);
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
					if (entry.FollowAnchor) return (m, m.GlobalPosition);
					pos = m.GlobalPosition;
				}
				break;
			}

			case SpellSpawnPoint.ScreenTopLeft:
			case SpellSpawnPoint.ScreenTopRight:
			case SpellSpawnPoint.ScreenBottomLeft:
			case SpellSpawnPoint.ScreenBottomRight:
			case SpellSpawnPoint.ScreenCenter:
			{
				var rect = GetViewport().GetVisibleRect();
				var size = rect.Size;
				var origin = rect.Position;
				var m = entry.ScreenMargin;

				pos = entry.SpawnPoint switch
				{
					SpellSpawnPoint.ScreenTopLeft => origin + new Vector2(m.X, m.Y),
					SpellSpawnPoint.ScreenTopRight => origin + new Vector2(size.X - m.X, m.Y),
					SpellSpawnPoint.ScreenBottomLeft => origin + new Vector2(m.X, size.Y - m.Y),
					SpellSpawnPoint.ScreenBottomRight => origin + new Vector2(size.X - m.X, size.Y - m.Y),
					_ => origin + (size * 0.5f),
				};
				break;
			}
		}

		return (parent, pos);
	}
}
