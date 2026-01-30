using Godot;
using System;

namespace Game.Combat;

public partial class EnemyElementCycleController : Node
{
	[Signal] public delegate void ElementChangedEventHandler(int elementId);

	[ExportGroup("Cycle")]
	[Export(PropertyHint.Range, "1,7,1")] public int StartElement = 1;
	[Export] public bool RandomizeOnStart = true;

	// Fácil: fica mais tempo no mesmo elemento | Difícil: troca mais rápido
	[Export(PropertyHint.Range, "0.2,10.0,0.05")] public float ChangeEverySeconds = 2.0f;

	// se true: escolhe aleatório (evitando repetir); se false: rotaciona dentro da lista AllowedElements
	[Export] public bool RandomPick = true;

	[Export] public bool Enabled = true;

	[ExportGroup("Allowed Elements")]
	// Se vazio: assume 1..6. Se preencher: só usa os ids aqui.
	[Export] public Godot.Collections.Array<int> AllowedElements = new() { 1, 2, 3, 4, 5, 6 };

	// Evitar repetir o mesmo elemento (principalmente no random)
	[Export] public bool AvoidRepeat = true;

	public int CurrentElement { get; private set; } = 1;

	private RandomNumberGenerator _rng = new();
	private double _nextChangeAtSec = -1;

	public override void _Ready()
	{
		_rng.Randomize();

		int start = RandomizeOnStart ? PickRandomAllowed(except: -1) : Mathf.Clamp(StartElement, 1, 7);
		start = EnsureAllowed(start);

		CurrentElement = start;
		EmitSignal(SignalName.ElementChanged, CurrentElement);
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
		int next;

		var list = GetAllowedNormalized();
		if (list.Count <= 0)
		{
			// fallback absoluto
			next = (prev % 7) + 1;
			SetElement(next);
			return;
		}

		if (list.Count == 1)
		{
			// “travado” em um elemento só
			SetElement(list[0]);
			return;
		}

		if (RandomPick)
		{
			next = PickRandomAllowed(except: (AvoidRepeat ? prev : -1));
		}
		else
		{
			// rotaciona DENTRO da lista AllowedElements
			int idx = list.IndexOf(prev);
			if (idx < 0) idx = 0;
			next = list[(idx + 1) % list.Count];
		}

		SetElement(next);
	}

	public void SetElement(int elementId)
	{
		elementId = Mathf.Clamp(elementId, 1, 7);
		elementId = EnsureAllowed(elementId);

		if (CurrentElement == elementId) return;

		CurrentElement = elementId;
		EmitSignal(SignalName.ElementChanged, CurrentElement);
	}

	// =========================
	// Helpers
	// =========================
	private Godot.Collections.Array<int> GetAllowedNormalized()
	{
		// se o user apagar tudo no inspector, volta pra 1..6
		if (AllowedElements == null || AllowedElements.Count == 0)
			return new Godot.Collections.Array<int> { 1, 2, 3, 4, 5, 6 };

		// normaliza: clamp 1..6 e remove duplicados (mantendo ordem)
		var norm = new Godot.Collections.Array<int>();
		for (int i = 0; i < AllowedElements.Count; i++)
		{
			int v = Mathf.Clamp(AllowedElements[i], 1, 7);
			if (!norm.Contains(v)) norm.Add(v);
		}

		if (norm.Count == 0)
			return new Godot.Collections.Array<int> { 1, 2, 3, 4, 5, 6 ,7};

		return norm;
	}

	private int EnsureAllowed(int elementId)
	{
		var list = GetAllowedNormalized();
		if (list.Contains(elementId)) return elementId;

		// se StartElement não estiver na lista, cai pro primeiro permitido
		return list[0];
	}

	private int PickRandomAllowed(int except)
	{
		var list = GetAllowedNormalized();
		if (list.Count == 1) return list[0];

		// tenta algumas vezes evitar repetição, sem loop infinito
		for (int tries = 0; tries < 10; tries++)
		{
			int idx = _rng.RandiRange(0, list.Count - 1);
			int pick = list[idx];
			if (except < 0 || pick != except) return pick;
		}

		// fallback: pega o próximo na lista
		int curIdx = list.IndexOf(except);
		if (curIdx < 0) curIdx = 0;
		return list[(curIdx + 1) % list.Count];
	}
}
