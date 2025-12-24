using Godot;
using System;
using System.Collections.Generic;

public partial class ElementController : Node
{
	[ExportCategory("Refs")]
	[Export] public NodePath TargetControllerPath;
	[Export] public NodePath SfxPlayerPath;
	[Export] public NodePath VfxPlayerPath;

	[ExportCategory("Config")]
	[Export] public int MaxElements = 2;

	private TargetController _targetController;
	private SfxPlayer _sfxPlayer;
	private VfxPlayer _vfxPlayer;

	private bool _inputEnabled = true;
	private readonly List<ElementIcon> _activeElements = new();

	public event Action<CastOutcome, SpellDefinition, Enemy> CastResolved;

	public override void _Ready()
	{
		
		
		_targetController = GetNodeOrNull<TargetController>(TargetControllerPath);
		_sfxPlayer = GetNodeOrNull<SfxPlayer>(SfxPlayerPath);
		_vfxPlayer = GetNodeOrNull<VfxPlayer>(VfxPlayerPath);

		GD.Print("=== VFX DEBUG ===");
		GD.Print("Node:", Name);
		GD.Print("Path:", GetPath());
		GD.Print("VfxPlayerPath:", VfxPlayerPath);

		var vfx = GetNodeOrNull<Node>(VfxPlayerPath);
		GD.Print("Resolved node:", vfx);

		if (_targetController == null)
			GD.PushWarning("ElementController: TargetControllerPath não setado ou node não encontrado.");
		if (_sfxPlayer == null)
			GD.PushWarning("ElementController: SfxPlayerPath não setado ou node não encontrado.");
		if (_vfxPlayer == null)
			GD.PushWarning("ElementController: VfxPlayerPath não setado ou node não encontrado.");
	}

	public void SetInputEnabled(bool enabled)
	{
		_inputEnabled = enabled;
		if (!enabled)
			ResetActiveElements();
	}

	public bool CanActivate() => _inputEnabled && _activeElements.Count < MaxElements;

	public void ActivateElement(ElementIcon element)
	{
		if (!_inputEnabled) return;
		if (element == null) return;
		if (_activeElements.Contains(element)) return;
		if (_activeElements.Count >= MaxElements) return;

		_activeElements.Add(element);
		element.SetActive(true);
		GD.Print($"Ativado: {element.Name} ({element.ElementType})");
	}

	public void Cast()
	{
		if (!_inputEnabled)
		{
			EmitResolved(CastOutcome.CancelledInputDisabled, null, null);
			return;
		}

		if (_activeElements.Count == 0)
		{
			EmitResolved(CastOutcome.CancelledNoElements, null, null);
			return;
		}

		var target = _targetController?.CurrentTarget;
		if (target == null || !GodotObject.IsInstanceValid(target))
		{
			GD.Print("CAST cancelado: nenhum alvo válido selecionado.");
			EmitResolved(CastOutcome.CancelledNoTarget, null, null);
			ResetActiveElements();
			return;
		}

		var castElements = new List<ElementType>();
		foreach (var icon in _activeElements)
			castElements.Add(icon.ElementType);

		var spell = SpellResolver.Resolve(castElements);

		_sfxPlayer?.PlaySpell(spell);
		_vfxPlayer?.PlaySpell(spell);

		var outcome = target.TakeSpellHit(spell);
		EmitResolved(outcome, spell, target);

		ResetActiveElements();
	}

	private void ResetActiveElements()
	{
		foreach (var element in _activeElements)
			element.ResetElement();
		_activeElements.Clear();
	}

	private void EmitResolved(CastOutcome outcome, SpellDefinition spell, Enemy target)
	{
		CastResolved?.Invoke(outcome, spell, target);
	}
}
