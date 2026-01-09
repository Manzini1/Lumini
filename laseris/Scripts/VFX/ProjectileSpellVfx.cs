using Godot;
using System;

public partial class ProjectileSpellVfx : Node2D, IVfxPlayable, ISpellVfxConfigurable
{
	public event Action Impacted;

	[ExportCategory("Projectile")]
	[Export] public float Speed = 1800f;
	[Export] public bool RotateToDirection = false;

	[Export(PropertyHint.Range, "-180,180,1")]
	public float RotationOffsetDegrees = 0f;

	[ExportCategory("Tuning")]
	[Export] public float HitDistance = 24f;

	[ExportCategory("Impact")]
	[Export] public PackedScene ImpactScene;

	private AnimatedSprite2D _sprite;

	private Node2D _target;
	private Vector2 _targetPos;
	private bool _hasTarget;

	private SpellVfxEntry _entry;

	public override void _Ready()
	{
		_sprite ??= GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (_sprite == null)
		{
			GD.PushError("[ProjectileSpellVfx] AnimatedSprite2D não encontrado.");
			QueueFree();
		}
	}

	public void Configure(SpellVfxEntry entry, Node2D caster, Node2D target)
	{
		_entry = entry;
		_target = target;
		_hasTarget = target != null && GodotObject.IsInstanceValid(target);

		_sprite ??= GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (_sprite == null)
		{
			GD.PushError("[ProjectileSpellVfx] Configure: AnimatedSprite2D não encontrado.");
			return;
		}

		// injeta frames se vier do entry
		if (_entry?.Frames != null)
			_sprite.SpriteFrames = _entry.Frames;

		if (_sprite.SpriteFrames == null)
		{
			GD.PushError("[ProjectileSpellVfx] SpriteFrames null. Sete na cena OU no SpellVfxEntry.Frames.");
			return;
		}

		string anim = string.IsNullOrWhiteSpace(_entry?.AnimationName) ? "" : _entry.AnimationName;

		if (!string.IsNullOrEmpty(anim) && _sprite.SpriteFrames.HasAnimation(anim))
			_sprite.Play(anim, _entry.SpeedScale);
		else
		{
			var names = _sprite.SpriteFrames.GetAnimationNames();
			if (names != null && names.Length > 0)
				_sprite.Play(names[0], _entry.SpeedScale);
			else
				GD.PushError("[ProjectileSpellVfx] SpriteFrames não tem nenhuma animação.");
		}

		_targetPos = ResolveTargetPos(target) + (_entry?.Offset ?? Vector2.Zero);
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
			Impacted?.Invoke();
			SpawnImpact();
			QueueFree();
			return;
		}

		var step = (float)(Speed * delta);
		var move = dir.Normalized() * Mathf.Min(step, dist);
		GlobalPosition += move;

		if (RotateToDirection)
			Rotation = dir.Angle() + Mathf.DegToRad(RotationOffsetDegrees);
	}

	private Vector2 ResolveTargetPos(Node2D target)
	{
		if (target == null) return GlobalPosition;
		var m = target.GetNodeOrNull<Marker2D>("VfxCenter");
		return m != null ? m.GlobalPosition : target.GlobalPosition;
	}

	private void SpawnImpact()
	{
		if (ImpactScene == null) return;

		var roots = GetTree().GetNodesInGroup("vfx_root");
		var parent = (roots != null && roots.Count > 0) ? roots[0] as Node : GetTree().CurrentScene;

		var impact = ImpactScene.Instantiate<Node2D>();
		parent.AddChild(impact);
		impact.GlobalPosition = _targetPos;
		impact.ZIndex = _entry?.ZIndex ?? 50;
	}
}
