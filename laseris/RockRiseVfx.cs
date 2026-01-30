using Godot;

namespace Game.Combat;

public partial class RockRiseVfx : Node2D
{
	[ExportGroup("Refs")]
	[Export] public NodePath RockSpritePath = "Sprite2D";

	[ExportGroup("Timings")]
	[Export] public float RiseSeconds = 0.18f;
	[Export] public float HoldAtHitSeconds = 0.06f;
	[Export] public float FallSeconds = 0.16f;
	[Export] public float RollSeconds = 0.35f;

	[ExportGroup("Roll")]
	[Export] public float RollDistanceMin = 18f;
	[Export] public float RollDistanceMax = 55f;
	[Export] public float RollRotationMin = 2.0f;  // rad
	[Export] public float RollRotationMax = 7.0f;  // rad

	private Node2D _rock;
	private RandomNumberGenerator _rng;

	public override void _Ready()
	{
		_rock = GetNodeOrNull<Node2D>(RockSpritePath);
		_rng = new RandomNumberGenerator();
		_rng.Randomize();
	}

	// ground = onde nasce e onde cai; hit = onde bate
	public void Play(Vector2 ground, Vector2 hit)
	{
		GlobalPosition = ground;

		// se tiver sprite, “enterrar” e subir
		if (_rock != null)
		{
			_rock.Position = Vector2.Zero;
			_rock.Scale = new Vector2(0.85f, 0.85f);
			_rock.Rotation = _rng.RandfRange(0, Mathf.Tau);
		}

		var tw = CreateTween();

		// RISE: ground -> hit
		tw.TweenProperty(this, "global_position", hit, RiseSeconds)
		  .SetTrans(Tween.TransitionType.Back)
		  .SetEase(Tween.EaseType.Out);

		// impact micro “punch”
		tw.TweenInterval(HoldAtHitSeconds);

		// FALL: hit -> ground
		tw.TweenProperty(this, "global_position", ground, FallSeconds)
		  .SetTrans(Tween.TransitionType.Quad)
		  .SetEase(Tween.EaseType.In);

		// ROLL: do ground para um offset aleatório + gira
		Vector2 dir = new Vector2(_rng.RandfRange(-1f, 1f), _rng.RandfRange(-0.4f, 0.4f)).Normalized();
		float dist = _rng.RandfRange(RollDistanceMin, RollDistanceMax);
		Vector2 rollTo = ground + dir * dist;

		if (_rock != null)
		{
			float rotAdd = _rng.RandfRange(RollRotationMin, RollRotationMax) * (_rng.Randf() < 0.5f ? -1f : 1f);
			tw.TweenProperty(this, "global_position", rollTo, RollSeconds)
			  .SetTrans(Tween.TransitionType.Quad)
			  .SetEase(Tween.EaseType.Out);

			tw.Parallel().TweenProperty(_rock, "rotation", _rock.Rotation + rotAdd, RollSeconds);
			tw.Parallel().TweenProperty(_rock, "scale", new Vector2(0.75f, 0.75f), RollSeconds);
		}
		else
		{
			tw.TweenProperty(this, "global_position", rollTo, RollSeconds);
		}

		// sumir
		tw.TweenInterval(0.15f);
		tw.TweenCallback(Callable.From(() => QueueFree()));
	}
}
