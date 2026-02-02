using Godot;

namespace Game.UI;

public partial class FlippingStoneController : Sprite2D
{
	[ExportGroup("Spin Feel")]
	[Export] public float MaxSpeedRad = 40f;
	[Export] public float GoodSpeedRad = 18f;
	[Export] public float Decel = 22f;
	[Export] public float PerfectBoostSeconds = 0.08f;

	[ExportGroup("Miss Brake")]
	[Export] public float MissBrake = 120f;

	private float _speed;
	private float _boostTimer;

	public override void _Ready()
	{
		// ✅ garante que process está ligado (evita “parou do nada” por config)
		SetProcess(true);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		Rotation += _speed * dt;

		float targetDecel = (_boostTimer > 0f) ? Decel * 0.35f : Decel;
		_boostTimer = Mathf.Max(0f, _boostTimer - dt);

		_speed = Mathf.MoveToward(_speed, 0f, targetDecel * dt);
	}

	public void OnPerfect()
	{
		_speed = MaxSpeedRad;
		_boostTimer = PerfectBoostSeconds;
	}

	public void OnGood()
	{
		_speed = Mathf.Max(_speed, GoodSpeedRad);
		_boostTimer = Mathf.Max(_boostTimer, PerfectBoostSeconds * 0.35f);
	}

	public void OnMiss()
	{
		// ✅ “freie totalmente”
		_speed = 0f;
		_boostTimer = 0f;
	}
}
