using Godot;
using System;

public partial class ProjectileSpellVfx : Node2D, IVfxPlayable
{
	public event Action Impacted;

	[ExportCategory("Projectile")]
	[Export] public float Speed = 900f;
	[Export] public bool RotateToDirection = false;

	[ExportCategory("Impact")]
	[Export] public PackedScene ImpactScene;        // ex: GenericSpellVfx.tscn
	[Export] public SpriteFrames ImpactFrames;      // frames do impacto
	[Export] public string ImpactAnimName = "play";
	[Export] public float ImpactSpeedScale = 1.0f;

	[ExportCategory("Tuning")]
	[Export] public float HitDistance = 24f;

	private AnimatedSprite2D _sprite;

	private Node2D _caster;
	private Node2D _target;
	private Vector2 _targetPos;
	private bool _hasTarget;

	private SpellVfxEntry _entry; // ✅ guarda entry pra usar ZIndex/Offset

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (_sprite == null)
		{
			GD.PushError("[ProjectileSpellVfx] AnimatedSprite2D não encontrado.");
			QueueFree();
			return;
		}
	}

	public void Configure(SpellVfxEntry entry, Node2D caster, Node2D target)
	{
		_entry = entry;
		_caster = caster;
		_target = target;

		_hasTarget = target != null && GodotObject.IsInstanceValid(target);

		// ✅ garante que o projétil não fique atrás
		if (_entry != null)
			ZIndex = _entry.ZIndex;

		// toca animação do projétil (SpriteFrames do AnimatedSprite2D na cena)
		_sprite?.Play();

		_targetPos = ResolveTargetPos(target) + (_entry?.Offset ?? Vector2.Zero);

		GD.Print($"[ProjectileSpellVfx] Configure target={target?.Name ?? "NULL"}");
		GD.Print($"[ProjectileSpellVfx] resolvedTargetPos={_targetPos} myPos={GlobalPosition}");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_sprite == null) return;

		if (_hasTarget && _target != null && GodotObject.IsInstanceValid(_target))
			_targetPos = ResolveTargetPos(_target) + (_entry?.Offset ?? Vector2.Zero);

		var dir = (_targetPos - GlobalPosition);
		var dist = dir.Length();

		if (dist <= HitDistance)
		{
			GlobalPosition = _targetPos;

			GD.Print("[ProjectileSpellVfx] HIT -> spawning impact");

			// ✅ primeiro dispara evento (sincroniza dano)
			Impacted?.Invoke();

			// depois efeito visual do impacto
			SpawnImpact();

			QueueFree();
			return;
		}

		var step = (float)(Speed * delta);
		var move = dir.Normalized() * Mathf.Min(step, dist);
		GlobalPosition += move;

		if (RotateToDirection)
			Rotation = dir.Angle();
	}

	private Vector2 ResolveTargetPos(Node2D target)
	{
		if (target == null) return GlobalPosition;

		var m = target.GetNodeOrNull<Marker2D>("VfxCenter");
		return m != null ? m.GlobalPosition : target.GlobalPosition;
	}

	private void SpawnImpact()
	{
		if (ImpactScene == null)
		{
			GD.PushWarning("[ProjectileSpellVfx] ImpactScene null (não vai mostrar impacto).");
			return;
		}

		var roots = GetTree().GetNodesInGroup("vfx_root");
		var parent = (roots.Count > 0) ? roots[0] as Node : GetTree().CurrentScene;

		var impact = ImpactScene.Instantiate<Node2D>();
		parent.AddChild(impact);

		impact.GlobalPosition = _targetPos;

		// ✅ coloca impacto na frente também
		if (_entry != null)
			impact.ZIndex = _entry.ZIndex;

		// se for genérico, injeta frames do impacto
		if (impact is GenericSpellVfx g)
		{
			var tmp = new SpellVfxEntry
			{
				Frames = ImpactFrames,
				AnimationName = ImpactAnimName,
				SpeedScale = ImpactSpeedScale,
				ZIndex = _entry?.ZIndex ?? 50,
				FallbackLifetime = 1.2f
			};

			g.Configure(tmp, _caster, _target);
		}

		GD.Print($"[ProjectileSpellVfx] impact spawned at {_targetPos} parent={parent.GetPath()} z={impact.ZIndex}");
	}
}
