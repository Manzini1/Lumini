using Godot;
using System;
using System.Collections.Generic;
using Game.UI;

namespace Game.Battle;

public partial class InputJudge : Node
{
	[Signal] public delegate void DefenseResolvedEventHandler(int beatIndex, bool success);
	[Signal] public delegate void AttackResolvedEventHandler(int beatIndex, bool success);

	// ✅ novos sinais com grade (0=Miss, 1=Good, 2=Perfect) + erro absoluto
	[Signal] public delegate void DefenseJudgedEventHandler(int beatIndex, int gradeId, double absErrorSec);
	[Signal] public delegate void AttackJudgedEventHandler(int beatIndex, int gradeId, double absErrorSec);

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
	private double _perfectWindowSec = 0.045; // default, recalculado no Configure
	private double _songNow;

	private ElementBarController _elementBar;

	// ajuste fino: PERFECT é uma fração da janela total
	private const double PERFECT_FRACTION = 0.35; // 35% da hit window

	public void Configure(double hitWindowSec, ElementBarController elementBar)
	{
		_hitWindowSec = hitWindowSec;
		_perfectWindowSec = Math.Max(0.01, hitWindowSec * PERFECT_FRACTION);
		_elementBar = elementBar;
	}

	// se quiser customizar perfeito separado:
	public void Configure(double hitWindowSec, double perfectWindowSec, ElementBarController elementBar)
	{
		_hitWindowSec = hitWindowSec;
		_perfectWindowSec = Math.Max(0.001, Math.Min(perfectWindowSec, hitWindowSec));
		_elementBar = elementBar;
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
	}

	public override void _UnhandledInput(InputEvent e)
	{
		if (_pending.Count == 0) return;

		int idx = GetNextPendingIndex();
		if (idx < 0) return;

		var p = _pending[idx];

		if (p.Side == TurnSide.Enemy && e.IsActionPressed("defend"))
		{
			var (gradeId, absErr) = EvaluateGrade(p);
			Resolve(idx, gradeId, absErr);
		}
		else if (p.Side == TurnSide.Player && e.IsActionPressed("attack"))
		{
			var (gradeId, absErr) = EvaluateGrade(p);
			Resolve(idx, gradeId, absErr);
		}
	}

	public void UpdateJudge()
	{
		// passou do beat + janela => MISS automático
		for (int i = 0; i < _pending.Count; i++)
		{
			var p = _pending[i];
			if (p.Resolved) continue;

			if (_songNow > p.BeatSec + _hitWindowSec)
			{
				double absErr = Math.Abs(_songNow - p.BeatSec);
				Resolve(i, (int)JudgementGrade.Miss, absErr);
			}
		}
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

	private (int gradeId, double absErrorSec) EvaluateGrade(Pending p)
	{
		double absErr = Math.Abs(_songNow - p.BeatSec);
		bool timingOk = absErr <= _hitWindowSec;

		int selected = _elementBar != null ? _elementBar.SelectedElementId : 1;
		bool elementOk = selected == p.RequiredElementId;

		if (!timingOk || !elementOk)
			return ((int)JudgementGrade.Miss, absErr);

		// timing + elemento ok => PERFECT ou GOOD
		if (absErr <= _perfectWindowSec)
			return ((int)JudgementGrade.Perfect, absErr);

		return ((int)JudgementGrade.Good, absErr);
	}

	private void Resolve(int index, int gradeId, double absErrorSec)
	{
		var p = _pending[index];
		if (p.Resolved) return;

		p.Resolved = true;
		_pending[index] = p;

		bool success = gradeId != (int)JudgementGrade.Miss;

		if (p.Side == TurnSide.Enemy)
		{
			EmitSignal(SignalName.DefenseResolved, p.BeatIndex, success);
			EmitSignal(SignalName.DefenseJudged, p.BeatIndex, gradeId, absErrorSec);
		}
		else
		{
			EmitSignal(SignalName.AttackResolved, p.BeatIndex, success);
			EmitSignal(SignalName.AttackJudged, p.BeatIndex, gradeId, absErrorSec);
		}
	}
}
