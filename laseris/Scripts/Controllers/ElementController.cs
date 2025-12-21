using Godot;
using System.Collections.Generic;

public partial class ElementController : Node
{
	[Export] private NodePath TargetControllerPath;
	private TargetController _targetController;

	private const int MAX_ELEMENTS = 2;
	private readonly List<ElementIcon> activeElements = new();

	public override void _Ready()
	{
		if (TargetControllerPath != null)
			_targetController = GetNode<TargetController>(TargetControllerPath);
		else
			GD.PushWarning("TargetControllerPath não configurado no Inspector.");
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("cast"))
			Cast();
	}

	public bool CanActivate() => activeElements.Count < MAX_ELEMENTS;

	public void ActivateElement(ElementIcon element)
	{
		if (activeElements.Contains(element)) return;
		if (activeElements.Count >= MAX_ELEMENTS) return;

		activeElements.Add(element);
		element.SetActive(true);

		GD.Print($"Ativado: {element.Name} ({element.ElementType})");
	}

	public void Cast()
	{
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

		GD.Print($"CAST! -> {spell.Name} ({string.Join(" + ", spell.Elements)}) Targeting={spell.Targeting} Dmg={spell.Damage}");

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
