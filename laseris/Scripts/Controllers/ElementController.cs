using Godot;
using System.Collections.Generic;

public partial class ElementController : Node
{
	[Export] private NodePath TargetControllerPath;
	private TargetController _targetController;

	private const int MAX_ELEMENTS = 2;
	private List<ElementIcon> activeElements = new();

	[Export] public int TestDamage = 20;

	public override void _Ready()
	{
		if (TargetControllerPath != null)
			_targetController = GetNode<TargetController>(TargetControllerPath);
		else
			GD.PushWarning("TargetControllerPath não configurado no Inspector.");
	}

	public override void _Process(double delta)
	{
		// ✅ cast só dispara uma vez por apertada
		if (Input.IsActionJustPressed("cast"))
		{
			Cast();
		}
	}

	// =========================
	// ATIVAÇÃO DE ELEMENTOS
	// =========================
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

		GD.Print($"Ativado: {element.Name}");
	}

	// =========================
	// CAST
	// =========================
	public void Cast()
	{
		// ✅ Não castar se não tem nada selecionado (limpa spam)
		if (activeElements.Count == 0)
			return;

		if (_targetController == null)
		{
			GD.Print("CAST cancelado: TargetController não encontrado.");
			return;
		}

		var target = _targetController.CurrentTarget;

		if (target == null || !GodotObject.IsInstanceValid(target))
		{
			GD.Print("CAST cancelado: nenhum alvo válido selecionado.");
			ResetActiveElements();
			return;
		}

		// 🔮 Log simples do combo (pra você não ter outro script imprimindo isso)
		GD.Print($"CAST! → {GetComboText()}");

		// 💥 aplica dano uma única vez
		target.TakeDamage(TestDamage);

		// 🔄 reseta runas
		ResetActiveElements();
	}

	private void ResetActiveElements()
	{
		foreach (var element in activeElements)
			element.ResetElement();

		activeElements.Clear();
	}

	private string GetComboText()
	{
		if (activeElements.Count == 1) return activeElements[0].Name;
		if (activeElements.Count == 2) return $"{activeElements[0].Name} + {activeElements[1].Name}";
		return $"{activeElements[0].Name} + {activeElements[1].Name} + {activeElements[2].Name}";
	}
}
