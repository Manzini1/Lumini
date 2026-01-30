using Godot;
using System;
using System.Collections.Generic;
using Game.UI;

namespace Game.Battle;

public partial class InputJudge : Node
{
	[Signal] public delegate void DefenseResolvedEventHandler(int beatIndex, bool success);
	[Signal] public delegate void AttackResolvedEventHandler(int beatIndex, bool success);

	[Signal] public delegate void DefenseJudgedEventHandler(int beatIndex, int gradeId, double absErrorSec);
	[Signal] public delegate void AttackJudgedEventHandler(int beatIndex, int gradeId, double absErrorSec);

	[Signal] public delegate void ElementPressedEventHandler(int elementId);

	[ExportGroup("Debug")]
	[Export] public bool DebugLogs = true;

	private struct Pending
	{
		public int BeatIndex;
		public double BeatSec;
		public int RequiredElementId;
		public TurnSide Side;
		public bool Resolved;
	}

	private readonly List<Pending> _pending = new();

	private double _hitWindowSec = 0.12;
	private double _songNow;

	[Export] public float PerfectWindowFraction = 0.35f;

	private ElementBarController _elementBar;

	public void Configure(double hitWindowSec, ElementBarController elementBar)
	{
		_hitWindowSec = Math.Max(0.001, hitWindowSec);
		_elementBar = elementBar;
		GD.Print($"[InputJudge] Config hitWindow={_hitWindowSec:0.000} perfectFrac={PerfectWindowFraction:0.00}");

	}

	public void SetSongTime(double songNowSec) => _songNow = songNowSec;

	public void ClearPending() => _pending.Clear();

	public void QueueDefense(int beatIndex, double beatSec, int requiredElementId)
	{
		_pending.Add(new Pending
		{
			BeatIndex = beatIndex,
			BeatSec = beatSec,
			RequiredElementId = requiredElementId,
			Side = TurnSide.Enemy,
			Resolved = false
		});

		if (DebugLogs)
			GD.Print($"[InputJudge] Queue DEF  beat={beatIndex} beatSec={beatSec:0.000} reqE={requiredElementId}");
	}

	public void QueueAttack(int beatIndex, double beatSec, int requiredElementId)
	{
		_pending.Add(new Pending
		{
			BeatIndex = beatIndex,
			BeatSec = beatSec,
			RequiredElementId = requiredElementId,
			Side = TurnSide.Player,
			Resolved = false
		});

		if (DebugLogs)
			GD.Print($"[InputJudge] Queue ATK  beat={beatIndex} beatSec={beatSec:0.000} reqE={requiredElementId}");
	}

	public override void _UnhandledInput(InputEvent e)
	{
		int pressedElement = GetPressedElementId(e);
		if (pressedElement > 0)
		{
			EmitSignal(SignalName.ElementPressed, pressedElement);
			_elementBar?.SetSelectedElement(pressedElement);

			if (_pending.Count == 0)
			{
				if (DebugLogs)
					GD.Print($"[InputJudge] Press e{pressedElement} (no pending) now={_songNow:0.000}");
				return;
			}

			int idx = GetNextPendingIndex();
			if (idx < 0) return;

			var p = _pending[idx];
			var (grade, absErr) = EvaluateGrade(p, pressedElement);

			if (DebugLogs)
			{
				string side = p.Side == TurnSide.Enemy ? "DEF" : "ATK";
				GD.Print($"[InputJudge] Press e{pressedElement} -> {side} beat={p.BeatIndex} reqE={p.RequiredElementId} now={_songNow:0.000} beatSec={p.BeatSec:0.000} absErr={absErr:0.0000} grade={grade}");
			}

			Resolve(idx, grade, absErr);
			return;
		}
	}

	public void UpdateJudge()
	{
		for (int i = 0; i < _pending.Count; i++)
		{
			var p = _pending[i];
			if (p.Resolved) continue;

			if (_songNow > p.BeatSec + _hitWindowSec)
			{
				double absErr = Math.Abs(_songNow - p.BeatSec);

				if (DebugLogs)
				{
					string side = p.Side == TurnSide.Enemy ? "DEF" : "ATK";
					GD.Print($"[InputJudge] AUTO MISS {side} beat={p.BeatIndex} now={_songNow:0.000} beatSec={p.BeatSec:0.000} absErr={absErr:0.0000}");
				}

				Resolve(i, JudgementGrade.Miss, absErr);
			}
		}
	}

	private int GetPressedElementId(InputEvent e)
	{
		for (int id = 1; id <= 6; id++)
			if (e.IsActionPressed($"e{id}"))
				return id;

		return -1;
	}

	private int GetNextPendingIndex()
	{
		double bestBeat = double.MaxValue;
		int bestIdx = -1;

		for (int i = 0; i < _pending.Count; i++)
		{
			if (_pending[i].Resolved) continue;
			if (_pending[i].BeatSec < bestBeat)
			{
				bestBeat = _pending[i].BeatSec;
				bestIdx = i;
			}
		}

		return bestIdx;
	}

	private (JudgementGrade grade, double absErr) EvaluateGrade(Pending p, int pressedElementId)
	{
		double absErr = Math.Abs(_songNow - p.BeatSec);

		if (pressedElementId != p.RequiredElementId)
			return (JudgementGrade.Miss, absErr);

		if (absErr > _hitWindowSec)
			return (JudgementGrade.Miss, absErr);

		double frac = Math.Clamp(PerfectWindowFraction, 0.01f, 0.99f);
		double perfectWindow = _hitWindowSec * frac;

		if (absErr <= perfectWindow)
			return (JudgementGrade.Perfect, absErr);

		return (JudgementGrade.Good, absErr);
	}

	private void Resolve(int index, JudgementGrade grade, double absErr)
	{
		var p = _pending[index];
		if (p.Resolved) return;

		p.Resolved = true;
		_pending[index] = p;

		bool success = grade != JudgementGrade.Miss;

		if (p.Side == TurnSide.Enemy)
		{
			EmitSignal(SignalName.DefenseJudged, p.BeatIndex, (int)grade, absErr);
			EmitSignal(SignalName.DefenseResolved, p.BeatIndex, success);
		}
		else
		{
			EmitSignal(SignalName.AttackJudged, p.BeatIndex, (int)grade, absErr);
			EmitSignal(SignalName.AttackResolved, p.BeatIndex, success);
		}
	}
}
