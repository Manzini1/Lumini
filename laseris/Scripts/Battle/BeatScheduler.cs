using Godot;
using System;
using System.Collections.Generic;

namespace Game.Battle;

public partial class BeatScheduler : Node
{
	[Signal] public delegate void BeatPrepareEventHandler(int beatIndex, double beatSec);
	[Signal] public delegate void BeatFireEventHandler(int beatIndex, double beatSec);

	private float[] _beats = Array.Empty<float>();
	private int _nextIndex = 0;

	private double _turnStart;
	private double _turnEnd;
	private double _prepareLead;
	private readonly HashSet<int> _prepared = new();
	private readonly HashSet<int> _fired = new();

	public void SetBeatmap(float[] beats)
	{
		_beats = beats ?? Array.Empty<float>();
		_nextIndex = 0;
		_prepared.Clear();
		_fired.Clear();
	}

	public void OnTurnWindow(double startSec, double endSec, double prepareLeadSec, double songNowSec)
	{
		_turnStart = startSec;
		_turnEnd = endSec;
		_prepareLead = prepareLeadSec;

		// pula beats que já ficaram pra trás
		while (_nextIndex < _beats.Length && _beats[_nextIndex] < songNowSec - 0.2)
			_nextIndex++;

		_prepared.Clear();
		_fired.Clear();
	}

	public void Update(double songNowSec)
	{
		// Só dispara coisas dentro da janela do turno
		// Prepare acontece em beat - lead, Fire acontece no beat.
		for (int i = _nextIndex; i < _beats.Length; i++)
		{
			double beat = _beats[i];

			if (beat > _turnEnd + 0.2)
				break;

			if (beat >= _turnStart && beat <= _turnEnd)
			{
				double prepareTime = beat - _prepareLead;

				if (!_prepared.Contains(i) && songNowSec >= prepareTime)
				{
					_prepared.Add(i);
					EmitSignal(SignalName.BeatPrepare, i, beat);
				}

				if (!_fired.Contains(i) && songNowSec >= beat)
				{
					_fired.Add(i);
					EmitSignal(SignalName.BeatFire, i, beat);
				}
			}

			// avança o cursor quando o beat já passou bastante
			if (beat < songNowSec - 0.25)
				_nextIndex = i + 1;
		}
	}
}
