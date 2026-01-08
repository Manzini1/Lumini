using Godot;
using System;

public partial class ThrownWeapon : Node2D
{
	[ExportCategory("Refs")]
	[Export] public NodePath VisualPath = "Sprite2D"; // Sprite2D / AnimatedSprite2D / Node2D

	[ExportCategory("Draw")]
	[Export] public int Z = 999;
	[Export] public bool TopLevel = true;

	[ExportCategory("Motion")]
	[Export] public float OutDuration = 0.25f;
	[Export] public float ReturnSpeed = 1400f;
	[Export] public float ArcHeight = 80f;
	[Export] public float ArriveDistance = 18f;

	[ExportCategory("Spin")]
	[Export] public float SpinSpeed = 18f;

	private Node2D _visual;   // ✅ agora é Node2D (tem Rotation)
	private Node2D _returnTo;
	private Vector2 _start;
	private Vector2 _target;
	private float _t;
	private bool _returning;

	private bool _finished;
	private bool _hitEmitted;

	public event Action Hit;
	public event Action<bool> Finished;

	public override void _Ready()
	{
		_visual = GetNodeOrNull<Node2D>(VisualPath)
				  ?? GetNodeOrNull<Node2D>("Sprite")
				  ?? GetNodeOrNull<Node2D>("Sprite2D")
				  ?? GetNodeOrNull<Node2D>("AnimatedSprite2D")
				  ?? GetNodeOrNull<Node2D>("Anim");

		ZIndex = Z;

		if (TopLevel)
			SetAsTopLevel(true);

		if (_visual == null)
			GD.PushWarning($"[ThrownWeapon] Visual não encontrado. Ajuste VisualPath (atual='{VisualPath}').");

		Visible = true;
	}

public void Launch(Node2D returnToSocket, Vector2 startGlobal, Vector2 targetGlobal)
{
	_returnTo = returnToSocket;
	GlobalPosition = startGlobal;
	_start = startGlobal;
	_target = targetGlobal;
	_t = 0f;
	_returning = false;
}

	public override void _ExitTree()
	{
		if (!_finished)
			Finish(false);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		// gira o visual
		if (_visual != null)
			_visual.Rotation += SpinSpeed * dt;

		// perdeu o socket -> aborta
		if (_returnTo == null || !GodotObject.IsInstanceValid(_returnTo))
		{
			Finish(false);
			QueueFree();
			return;
		}

		// ida
		if (!_returning)
		{
			_t += dt / Mathf.Max(OutDuration, 0.01f);
			float u = Mathf.Clamp(_t, 0f, 1f);

			Vector2 pos = _start.Lerp(_target, u);
			float arc = Mathf.Sin(u * Mathf.Pi) * ArcHeight;
			pos.Y -= arc;

			GlobalPosition = pos;

			if (u >= 1f)
			{
				_returning = true;

				if (!_hitEmitted)
				{
					_hitEmitted = true;
					Hit?.Invoke();
				}
			}
			return;
		}

		// volta
		Vector2 goal = _returnTo.GlobalPosition;
		Vector2 toGoal = goal - GlobalPosition;
		float dist = toGoal.Length();

		if (dist <= ArriveDistance)
		{
			GlobalPosition = goal;
			Finish(true);
			QueueFree();
			return;
		}

		Vector2 step = toGoal.Normalized() * ReturnSpeed * dt;
		if (step.Length() > dist) GlobalPosition = goal;
		else GlobalPosition += step;
	}

	private void Finish(bool success)
	{
		if (_finished) return;
		_finished = true;
		Finished?.Invoke(success);
	}
}
