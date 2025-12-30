using Godot;
using System;

public partial class CombatController : Node
{
	[ExportCategory("Refs")]
	[Export] public NodePath MagePath;
	[Export] public NodePath ElementControllerPath;
	[Export] public NodePath TargetControllerPath;

	[ExportCategory("Input")]
	[Export] public string CastAction = "cast"; // action do InputMap

	private Mage _mage;
	private ElementController _elementController;
	private TargetController _targetController;

	public override void _Ready()
	{
		_mage = GetNodeOrNull<Mage>(MagePath);
		_elementController = GetNodeOrNull<ElementController>(ElementControllerPath);
		_targetController = GetNodeOrNull<TargetController>(TargetControllerPath);
			GD.Print($"[CombatController] NodePath={GetPath()} Owner={Owner?.Name} Scene={GetTree().CurrentScene?.Name}");
			GD.Print($"MagePath empty? {MagePath.IsEmpty} val='{MagePath}'");
		GD.Print($"[Combat] Self={GetPath()} Owner={Owner?.GetPath()} MagePath='{MagePath}' empty={MagePath.IsEmpty}");
GD.Print($"[Combat] Mage raw = {GetNodeOrNull<Node>(MagePath)}");
		if (_mage == null) GD.PushError("CombatController: MagePath não setado ou node não encontrado.");
		if (_elementController == null) GD.PushError("CombatController: ElementControllerPath não setado ou node não encontrado.");
		if (_targetController == null) GD.PushWarning("CombatController: TargetControllerPath não setado (ok, mas sem alvo).");

		// Escuta o resultado do cast (HIT / MISS / ABSORB / CANCEL)
		if (_elementController != null)
			_elementController.CastResolved += OnCastResolved;

		// Checagem amigável do InputMap
		if (!InputMap.HasAction(CastAction))
			GD.PushWarning($"CombatController: action '{CastAction}' não existe no Input Map.");
	}

	public override void _ExitTree()
	{
		if (_elementController != null)
			_elementController.CastResolved -= OnCastResolved;
	}

	public override void _Process(double delta)
	{
		// 1) Cast no input
		if (Input.IsActionJustPressed(CastAction))
		{
			// (Opcional) virar a mage pro alvo antes de castar
			var target = _targetController?.CurrentTarget;
			if (_mage != null && target != null && GodotObject.IsInstanceValid(target))
				_mage.FaceWorldPosition(target.GlobalPosition);

			_elementController?.Cast();
		}
	}

	private void OnCastResolved(CastOutcome outcome, SpellDefinition spell, Enemy target)
	{
		// Aqui entra feedback do player (e no futuro: camera shake, hitstop, etc)
		if (_mage == null) return;

		switch (outcome)
		{
			case CastOutcome.Hit:
				_mage.PlayCastFeedback();
				break;

			case CastOutcome.Absorbed50:
			case CastOutcome.Absorbed100:
				// Feedback “mais fraco” (por enquanto só reutiliza o mesmo)
				_mage.PlayCastFeedback();
				break;

			case CastOutcome.Miss:
				// Você pode fazer um feedback diferente depois (som de miss, etc)
				break;

			default:
				// Cancelados / bloqueados: não faz nada
				break;
		}
	}
}
