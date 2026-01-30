using Godot;
using System;

namespace Game.Combat;

public partial class EnemyProtectionController : Node
{
	[Signal] public delegate void ProtectionChangedEventHandler(int elementId);

	[ExportGroup("Protection")]
	[Export(PropertyHint.Range, "1,7,1")]
	public int StartElement = 1;

	[Export] public bool RandomizeOnStart = true;

	// Troca “lenta” (o que você quer)
	[Export(PropertyHint.Range, "0.5,10.0,0.05")]
	public float ChangeEverySeconds = 2.0f;

	[Export] public bool Enabled = true;

	public int CurrentElement { get; private set; } = 1;

	private RandomNumberGenerator _rng;
	private double _nextChangeAtSec = -1;

	public override void _Ready()
	{
		_rng = new RandomNumberGenerator();
		_rng.Randomize();

		CurrentElement = RandomizeOnStart ? _rng.RandiRange(1, 7) : Mathf.Clamp(StartElement, 1, 7);
		EmitSignal(SignalName.ProtectionChanged, CurrentElement);
	}

	public void Start(double nowSec)
	{
		Enabled = true;
		_nextChangeAtSec = nowSec + ChangeEverySeconds;
	}

	public void Stop()
	{
		Enabled = false;
		_nextChangeAtSec = -1;
	}

	public void UpdateNow(double nowSec)
	{
		if (!Enabled) return;
		if (ChangeEverySeconds <= 0.01f) return;

		if (_nextChangeAtSec < 0)
			_nextChangeAtSec = nowSec + ChangeEverySeconds;

		if (nowSec >= _nextChangeAtSec)
		{
			NextElement();
			_nextChangeAtSec = nowSec + ChangeEverySeconds;
		}
	}

	public void NextElement()
	{
		int prev = CurrentElement;
		int next = _rng.RandiRange(1, 7);
		if (next == prev) next = (prev % 7) + 1;

		SetElement(next);
	}

	public void SetElement(int elementId)
	{
		elementId = Mathf.Clamp(elementId, 1, 7);
		if (CurrentElement == elementId) return;

		CurrentElement = elementId;
		EmitSignal(SignalName.ProtectionChanged, CurrentElement);
	}

	public double GetTimeToNextChange(double nowSec)
	{
		if (_nextChangeAtSec < 0) return ChangeEverySeconds;
		return Math.Max(0.0, _nextChangeAtSec - nowSec);
	}

	public float GetProgressToNextChange01(double nowSec)
	{
		if (ChangeEverySeconds <= 0.01f) return 1f;
		double left = GetTimeToNextChange(nowSec);
		return Mathf.Clamp((float)(1.0 - (left / ChangeEverySeconds)), 0f, 1f);
	}
}
