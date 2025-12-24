using Godot;

public partial class UltimateGaugeController : Node
{
	[ExportCategory("Refs")]
	[Export] public NodePath ElementControllerPath;
	[Export] public NodePath UltimateControllerPath;

	[ExportCategory("Config")]
	[Export] public int BaseHitsToFill = 5;          // em “ritmo normal”, ~5 hits enchem
	[Export] public float StreakBonusPerHit = 0.0f;  // deixa 0.0 pra não adiantar; depois você aumenta (ex: 0.08)

	private ElementController _elementController;
	private UltimateController _ultimateController;

	private int _streak = 0;
	private float _gauge01 = 0f; // 0..1

	public override void _Ready()
	{
		_elementController = GetNodeOrNull<ElementController>(ElementControllerPath);
		_ultimateController = GetNodeOrNull<UltimateController>(UltimateControllerPath);

		if (_elementController == null)
		{
			GD.PushError("UltimateGaugeController: ElementControllerPath não setado ou node não encontrado.");
			return;
		}

		if (_ultimateController == null)
			GD.PushWarning("UltimateGaugeController: UltimateControllerPath não setado (gauge vai funcionar, mas não vai destravar ultimate).");

		// ✅ Aqui é onde estava seu erro: assinatura tem que bater com Action<CastOutcome, SpellDefinition, Enemy>
		_elementController.CastResolved += OnCastResolved;

		// começa travado
		if (_ultimateController != null)
			_ultimateController.UltimateUnlocked = false;

		GD.Print("[ULT-GAUGE] pronto.");
	}

	public override void _ExitTree()
	{
		if (_elementController != null)
			_elementController.CastResolved -= OnCastResolved;
	}

	// ✅ Assinatura CORRETA
	private void OnCastResolved(CastOutcome outcome, SpellDefinition spell, Enemy target)
	{
		// se não foi um cast “real”, ignora
		if (outcome == CastOutcome.CancelledInputDisabled ||
			outcome == CastOutcome.CancelledNoElements ||
			outcome == CastOutcome.CancelledNoTarget)
			return;

		if (outcome == CastOutcome.Hit)
		{
			_streak++;

			float baseGain = (BaseHitsToFill <= 0) ? 1f : (1f / BaseHitsToFill);

			// quanto mais streak, mais rápido (configurável)
			float mult = 1f + ((_streak - 1) * StreakBonusPerHit);
			float gain = baseGain * mult;

			_gauge01 = Mathf.Clamp(_gauge01 + gain, 0f, 1f);

			GD.Print($"[ULT-GAUGE] HIT | streak={_streak} | gauge={_gauge01:0.00}");
		}
		else
		{
			// Miss / Blocked / Absorb -> não zera gauge, mas zera velocidade (streak)
			_streak = 0;
			GD.Print($"[ULT-GAUGE] {outcome} | streak reset | gauge={_gauge01:0.00}");
		}

		// destrava ultimate quando gauge enche
		if (_ultimateController != null)
		{
			bool unlockedNow = _gauge01 >= 1f;
			_ultimateController.UltimateUnlocked = unlockedNow;

			if (unlockedNow)
				GD.Print("[ULT-GAUGE] Ultimate destravada! (pressione a tecla ultimate)");
		}
	}

	// (Opcional) se quiser consumir a ultimate e resetar gauge quando usar:
	public void ConsumeUltimate()
	{
		_gauge01 = 0f;
		_streak = 0;

		if (_ultimateController != null)
			_ultimateController.UltimateUnlocked = false;

		GD.Print("[ULT-GAUGE] consumiu ultimate -> gauge reset.");
	}
}
