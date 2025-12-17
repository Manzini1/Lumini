using Godot;
using System.Collections.Generic;

public partial class ElementController : Node
{
	private const int MAX_ELEMENTS = 3;
	private List<ElementIcon> activeElements = new();

	public bool CanActivate()
	{
		return activeElements.Count < MAX_ELEMENTS;
	}

	public void ActivateElement(ElementIcon element)
	{
		if (activeElements.Contains(element))
			return;

		if (activeElements.Count >= MAX_ELEMENTS)
			return;

		activeElements.Add(element);
		element.SetActive(true);

		GD.Print("Ativado: ", element.Name);
	}

	public void Cast()
	{
		GD.Print("CAST");

		foreach (var element in activeElements)
		{
			element.ResetElement();
		}

		activeElements.Clear();
	}
}
