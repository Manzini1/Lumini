using Godot;
using System;

public partial class CameraShake2D : Camera2D
{
	[ExportCategory("Defaults")]
	[Export] public float DefaultDuration = 0.25f;
	[Export] public float DefaultStrength = 10f; // pixels
	[Export] public float DefaultDecay = 45f;    // quão rápido volta pro 0

	private readonly RandomNumberGenerator _rng = new();
	private float _timeLeft = 0f;
	private float _strength = 0f;
	private float _decay = 45f;

	public void Shake(float strength, float duration, float decay = 45f)
	{
		_strength = Mathf.Max(_strength, strength);
		_timeLeft = Mathf.Max(_timeLeft, duration);
		_decay = Mathf.Max(1f, decay);
	}

	public override void _Process(double delta)
	{
		if (_timeLeft <= 0f)
		{
			Offset = Vector2.Zero;
			return;
		}

		float dt = (float)delta;
		_timeLeft -= dt;

		// jitter
		float x = _rng.RandfRange(-1f, 1f) * _strength;
		float y = _rng.RandfRange(-1f, 1f) * _strength;
		Offset = new Vector2(x, y);

		// decay
		_strength = Mathf.Max(0f, _strength - _decay * dt);

		if (_timeLeft <= 0f)
			Offset = Vector2.Zero;
	}
}
