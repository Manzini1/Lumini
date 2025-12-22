using Godot;
using System.Collections.Generic;

public partial class ElementController : Node
{
	[Export] private NodePath TargetControllerPath;
	private TargetController _targetController;

	private const int MAX_ELEMENTS = 2;
	private readonly List<ElementIcon> activeElements = new();

	private bool _inputEnabled = true;

	public override void _Ready()
	{
		if (TargetControllerPath != null)
			_targetController = GetNode<TargetController>(TargetControllerPath);
		else
			GD.PushWarning("TargetControllerPath não configurado no Inspector.");
	}

	public override void _Process(double delta)
	{
		if (!_inputEnabled) return;

		if (Input.IsActionJustPressed("cast"))
			Cast();
	}

	public void SetInputEnabled(bool enabled)
	{
		_inputEnabled = enabled;
	}

	public bool CanActivate()
	{
		if (!_inputEnabled) return false;
		return activeElements.Count < MAX_ELEMENTS;
	}

	public void ActivateElement(ElementIcon element)
	{
		if (!_inputEnabled) return;

		if (activeElements.Contains(element)) return;
		if (activeElements.Count >= MAX_ELEMENTS) return;

		activeElements.Add(element);
		element.SetActive(true);

		GD.Print($"Ativado: {element.Name} ({element.ElementType})");
	}

	public void Cast()
	{
		if (!_inputEnabled) return;
		if (activeElements.Count == 0) return;

		var target = _targetController?.CurrentTarget;
		if (target == null || !GodotObject.IsInstanceValid(target))
		{
			GD.Print("CAST cancelado: nenhum alvo válido selecionado.");
			ResetActiveElements();
			return;
		}

		List<ElementType> castElements = new();
		foreach (var icon in activeElements)
			castElements.Add(icon.ElementType);

		var spell = SpellResolver.Resolve(castElements);

		GD.Print($"CAST! -> {spell.Name} ({string.Join(" + ", spell.Elements)})");

		target.TakeSpellHit(spell);

		ResetActiveElements();
	}

	private void ResetActiveElements()
	{
		foreach (var element in activeElements)
			element.ResetElement();

		activeElements.Clear();
	}
}
