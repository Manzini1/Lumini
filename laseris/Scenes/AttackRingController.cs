using Godot;
using System;

namespace Game.UI;

public partial class AttackRingController : Control
{
	// Tempo global (setado 1x por frame pelo BattleController)
	public static double SongNowSec;
	[Export] public float VisualSpeed = 1f; // 1 = normal, 2 = 2x mais rápido, 0.5 = mais lento
	[Export] public float MaxRadius = 90f;
	[Export] public float TargetRadius = 40f;
	[Export] public float LineWidth = 6f;

	[ExportGroup("Lifetime")]
	[Export] public float HoldAfterBeatSeconds = 0.05f;
	[Export] public float FadeOutSeconds = 0.25f;

	private bool _active;
	private double _startSec;
	private double _beatSec;
	private double _hitWindowSec;

	private double _fadeStartSec;
	private double _fadeEndSec;
	private float _alpha = 1f;

	public void Arm(double startSec, double beatSec, double hitWindowSec)
	{
		_active = true;
		_startSec = startSec;
		_beatSec = beatSec;
		_hitWindowSec = hitWindowSec;

		_fadeStartSec = _beatSec + HoldAfterBeatSeconds;
		_fadeEndSec = _fadeStartSec + Math.Max(0.01, FadeOutSeconds);

		_alpha = 1f;
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (!_active) return;

		double now = SongNowSec;

		if (now >= _fadeStartSec)
		{
			float t = (float)((now - _fadeStartSec) / (_fadeEndSec - _fadeStartSec));
			t = Mathf.Clamp(t, 0f, 1f);
			_alpha = 1f - t;
		}

		if (now >= _fadeEndSec)
		{
			QueueFree();
			return;
		}

		QueueRedraw();
	}

	public override void _Draw()
	{
		if (!_active) return;

		Vector2 center = Size * 0.5f;

		// progresso 0..1 (0 no start, 1 no beat)
		double denom = Math.Max(0.0001, (_beatSec - _startSec));
		double t = (SongNowSec - _startSec) / denom;

		// aplica speed visual em torno do start
		t *= VisualSpeed;

		t = Math.Clamp(t, 0.0, 1.2);
		var c = new Color(1f, 1f, 1f, _alpha);

		// círculo alvo
		DrawArc(center, TargetRadius, 0, Mathf.Tau, 96, c, LineWidth);

		// círculo “fechando”
		float radius = Mathf.Lerp(MaxRadius, TargetRadius, (float)Math.Clamp(t, 0.0, 1.0));
		DrawArc(center, radius, 0, Mathf.Tau, 96, c, LineWidth);

		// reforço na janela de hit
		double distToBeat = Math.Abs(SongNowSec - _beatSec);
		if (distToBeat <= _hitWindowSec)
			DrawArc(center, TargetRadius, 0, Mathf.Tau, 96, c, LineWidth + 4f);
	}
}
