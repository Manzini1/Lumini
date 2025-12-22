using Godot;
using System.Collections.Generic;

public partial class UltimateController : Node
{
	[ExportCategory("Refs")]
	[Export] public NodePath UltimateHudPath;
	[Export] public NodePath ElementControllerPath;
	[Export] public NodePath TargetControllerPath;

	[ExportCategory("Ultimate Settings")]
	[Export] public float AutoTriggerEverySeconds = 1000000f;  // teste automático
	[Export] public float TimeLimitSeconds = 10.0f;        // tempo total do QTE
	[Export] public int SequenceLength = 5;              // ex: ice fire lightning fire ice

	private UltimateHud _hud;
	private ElementController _elementController;
	private TargetController _targetController;

	private bool _active = false;
	private float _cooldown = 0f;
	private float _timeLeft = 0f;

	private List<ElementType> _sequence = new();
	private int _index = 0;

	// ✅ Mapa: ACTION NAME -> ElementType
	private static readonly Dictionary<string, ElementType> _actionMap = new()
	{
		{ "Fire", ElementType.Fire },
		{ "Ice", ElementType.Ice },
		{ "Lightning", ElementType.Lightning },
		{ "Venom", ElementType.Poison },

		{ "Earth", ElementType.Earth },
		{ "Light", ElementType.Light },
		{ "Shadow", ElementType.Shadow },
		{ "Air", ElementType.Air },
	};

	private static readonly ElementType[] _pool = new[]
	{
		ElementType.Fire, ElementType.Ice, ElementType.Lightning, ElementType.Poison,
		ElementType.Earth, ElementType.Light, ElementType.Shadow, ElementType.Air
	};

	public override void _Ready()
	{
		_hud = GetNodeOrNull<UltimateHud>(UltimateHudPath);
		_elementController = GetNodeOrNull<ElementController>(ElementControllerPath);
		_targetController = GetNodeOrNull<TargetController>(TargetControllerPath);

		if (_hud == null)
			GD.PushError("UltimateController: UltimateHudPath não está setado ou não encontrei o node.");

		if (_elementController == null)
			GD.PushError("UltimateController: ElementControllerPath não está setado ou não encontrei o node.");

		if (_targetController == null)
			GD.PushWarning("UltimateController: TargetControllerPath não setado (ok por enquanto, mas a ultimate não vai dar dano).");

		// Checagem opcional: avisa se alguma action não existe
		foreach (var action in _actionMap.Keys)
		{
			if (!InputMap.HasAction(action))
				GD.PushWarning($"UltimateController: action '{action}' não existe no Input Map.");
		}

		_hud?.HideHud();
	}

	public override void _Process(double delta)
	{
		// Manual (sua action já existe)
		if (!_active && Input.IsActionJustPressed("ultimate"))
		{
			StartUltimate("manual");
			return;
		}

		// Auto trigger (teste)
		if (!_active)
		{
			_cooldown += (float)delta;
			if (_cooldown >= AutoTriggerEverySeconds)
			{
				StartUltimate("auto");
				return;
			}
			return;
		}

		// QTE ativo: tempo + input
		_timeLeft -= (float)delta;
		_hud?.SetTimer(_timeLeft);

		if (_timeLeft <= 0f)
		{
			Fail("tempo acabou");
			return;
		}

		HandleRuneInput();
	}

	private void StartUltimate(string reason)
	{
		if (_hud == null || _elementController == null)
			return;

		_active = true;
		_cooldown = 0f;

		_timeLeft = TimeLimitSeconds;
		_index = 0;

		_sequence = GenerateSequence(SequenceLength);

		_hud.ShowHud();
		_hud.SetSequence(_sequence);
		_hud.SetTimer(_timeLeft);

		// trava runas normais
		_elementController.SetInputEnabled(false);

		GD.Print($"[ULT] START ({reason}) -> {string.Join(", ", _sequence)}");
	}

	private List<ElementType> GenerateSequence(int length)
	{
		var seq = new List<ElementType>(length);
		for (int i = 0; i < length; i++)
		{
			int idx = (int)GD.RandRange(0, _pool.Length - 1);
			seq.Add(_pool[idx]);
		}
		return seq;
	}

	// ✅ agora lê ações do InputMap (configurável)
	private void HandleRuneInput()
	{
		foreach (var kv in _actionMap)
		{
			string action = kv.Key;
			ElementType element = kv.Value;

			if (Input.IsActionJustPressed(action))
			{
				OnRunePressed(element);
				return; // 1 input por frame, evita “duplo”
			}
		}
	}

	private void OnRunePressed(ElementType pressed)
	{
		if (!_active) return;

		ElementType expected = _sequence[_index];

		if (pressed == expected)
		{
			_hud?.MarkCorrect(_index);
			_index++;

			if (_index >= _sequence.Count)
				Success();
		}
		else
		{
			_hud?.MarkFail(_index);
			Fail($"errou: {pressed} (esperado {expected})");
		}
	}

	private void Success()
	{
		GD.Print("[ULT] SUCCESS!");
		CastUltimate();
		EndUltimate();
	}

	private void Fail(string reason)
	{
		GD.Print($"[ULT] FAIL -> {reason}");
		EndUltimate();
	}

	private void EndUltimate()
	{
		_active = false;
		_hud?.HideHud();
		_elementController?.SetInputEnabled(true);
	}

	private void CastUltimate()
	{
		if (_targetController == null)
		{
			GD.Print("[ULT] sem TargetController (não vai aplicar dano ainda).");
			return;
		}

		var target = _targetController.CurrentTarget;
		if (target == null || !GodotObject.IsInstanceValid(target))
		{
			GD.Print("[ULT] sem alvo selecionado.");
			return;
		}

		var elements = new List<ElementType> { ElementType.Ice, ElementType.Fire, ElementType.Lightning };
		var targeting = SpellTargeting.Ground | SpellTargeting.Air;

		var spell = new SpellDefinition(
			name: "ULTIMATE TEST",
			elements: elements,
			damage: 250,
			targeting: targeting
		);

		target.TakeSpellHit(spell);
	}
}
