using Godot;
using System.Collections.Generic;

public partial class UltimateController : Node
{
	[ExportCategory("Refs")]
	[Export] public NodePath UltimateHudPath;
	[Export] public NodePath ElementControllerPath;
	[Export] public NodePath TargetControllerPath;
	[Export] public NodePath SfxPlayerPath;
	[Export] public NodePath VfxPlayerPath;

	[ExportCategory("Ultimate Settings")]
	[Export] public float TimeLimitSeconds = 10.0f;
	[Export] public int SequenceLength = 5;

	[Export] public string UltimateSpellId = "ult_test";
	[Export] public string UltimateSpellName = "ULTIMATE TEST";
	[Export] public int UltimateDamage = 250;
	[Export] public SpellTargeting UltimateTargeting = SpellTargeting.Both;

	private UltimateHud _hud;
	private ElementController _elementController;
	private TargetController _targetController;
	private SfxPlayer _sfxPlayer;
	private VfxPlayer _vfxPlayer;

	private bool _active = false;
	private float _timeLeft = 0f;

	private List<ElementType> _sequence = new();
	private int _index = 0;

	public bool UltimateUnlocked { get; set; } = false;

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
				GD.Print("=== VFX DEBUG ===");
				GD.Print("Node:", Name);
				GD.Print("Path:", GetPath());
				GD.Print("VfxPlayerPath:", VfxPlayerPath);

				var vfx = GetNodeOrNull<Node>(VfxPlayerPath);
				GD.Print("Resolved node:", vfx);

		_hud = GetNodeOrNull<UltimateHud>(UltimateHudPath);
		_elementController = GetNodeOrNull<ElementController>(ElementControllerPath);
		_targetController = GetNodeOrNull<TargetController>(TargetControllerPath);
		_sfxPlayer = GetNodeOrNull<SfxPlayer>(SfxPlayerPath);
		_vfxPlayer = GetNodeOrNull<VfxPlayer>(VfxPlayerPath);

		if (_hud == null) GD.PushWarning("UltimateController: UltimateHudPath não setado.");
		if (_elementController == null) GD.PushError("UltimateController: ElementControllerPath não setado.");
		if (_targetController == null) GD.PushWarning("UltimateController: TargetControllerPath não setado.");
		if (_sfxPlayer == null) GD.PushWarning("UltimateController: SfxPlayerPath não setado.");
		if (_vfxPlayer == null) GD.PushWarning("UltimateController: VfxPlayerPath não setado.");

		foreach (var action in _actionMap.Keys)
			if (!InputMap.HasAction(action))
				GD.PushWarning($"UltimateController: action '{action}' não existe no InputMap.");

		_hud?.HideHud();
	}

	public override void _Process(double delta)
	{
		if (!_active && Input.IsActionJustPressed("ultimate"))
		{
			if (!UltimateUnlocked)
			{
				GD.Print("[ULT] ainda não destravada.");
				return;
			}

			StartUltimate();
			return;
		}

		if (!_active)
			return;

		_timeLeft -= (float)delta;
		_hud?.SetTimer(_timeLeft);

		if (_timeLeft <= 0f)
		{
			Fail("tempo acabou");
			return;
		}

		HandleRuneInput();
	}

	private void StartUltimate()
	{
		if (_hud == null || _elementController == null)
			return;

		_active = true;
		_timeLeft = TimeLimitSeconds;
		_index = 0;

		_sequence = GenerateSequence(SequenceLength);

		_hud.ShowHud();
		_hud.SetSequence(_sequence);
		_hud.SetTimer(_timeLeft);

		_elementController.SetInputEnabled(false);

		GD.Print($"[ULT] START -> {string.Join(", ", _sequence)}");
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

	private void HandleRuneInput()
	{
		foreach (var kv in _actionMap)
		{
			if (Input.IsActionJustPressed(kv.Key))
			{
				OnRunePressed(kv.Value);
				return;
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
		var target = _targetController?.CurrentTarget;
		if (target == null || !GodotObject.IsInstanceValid(target))
		{
			GD.Print("[ULT] sem alvo selecionado.");
			return;
		}

		var elements = new List<ElementType> { ElementType.Ice, ElementType.Fire, ElementType.Lightning };

		var spell = new SpellDefinition(
			id: UltimateSpellId,
			name: UltimateSpellName,
			elements: elements,
			damage: UltimateDamage,
			targeting: UltimateTargeting
		);

		_sfxPlayer?.PlaySpell(spell);
		_vfxPlayer?.PlaySpell(spell);

		// ✅ FIX DEFINITIVO
		var outcome = target.TakeSpellHit(spell);
		GD.Print($"[ULT] outcome = {outcome}");
	}
}
