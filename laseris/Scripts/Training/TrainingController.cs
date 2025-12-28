using Godot;
using System;
using System.Collections.Generic;

public partial class TrainingController : Node
{
	[ExportCategory("Refs")]
	[Export] public NodePath TargetControllerPath;   // World/TargetController
	[Export] public NodePath ElementControllerPath;  // HUD/ElementHUD/ElementController (ajuste conforme seu node)
	[Export] public NodePath EnemiesRootPath;        // World/Enemies
	[Export] public NodePath EnemyScene;             // PackedScene opcional (Enemy.tscn)

	[ExportCategory("UI")]
	[Export] public NodePath TargetLabelPath;
	[Export] public NodePath HpLabelPath;
	[Export] public NodePath LastSpellLabelPath;
	[Export] public NodePath OutcomeLabelPath;

	[Export] public NodePath BtnResetHpPath;
	[Export] public NodePath BtnToggleFlyingPath;
	[Export] public NodePath BtnToggleShieldPath;
	[Export] public NodePath BtnSpawn1Path;
	[Export] public NodePath BtnSpawn2Path;
	[Export] public NodePath BtnSpawn3Path;
	[Export] public NodePath BtnBackPath;

	[ExportCategory("Training Settings")]
	[Export] public int DefaultHp = 1000;
	[Export] public bool StartWithShieldOn = false;

	private TargetController _targetController;
	private ElementController _elementController;
	private Node2D _enemiesRoot;
	private PackedScene _enemyPacked;

	private Label _targetLabel;
	private Label _hpLabel;
	private Label _lastSpellLabel;
	private Label _outcomeLabel;

	private Button _btnResetHp;
	private Button _btnToggleFlying;
	private Button _btnToggleShield;
	private Button _btnSpawn1;
	private Button _btnSpawn2;
	private Button _btnSpawn3;
	private Button _btnBack;

	private bool _shieldOn;

	public override void _Ready()
	{
		_shieldOn = StartWithShieldOn;

		_targetController = GetNodeOrNull<TargetController>(TargetControllerPath);
		_elementController = GetNodeOrNull<ElementController>(ElementControllerPath);
		_enemiesRoot = GetNodeOrNull<Node2D>(EnemiesRootPath);

		_targetLabel = GetNodeOrNull<Label>(TargetLabelPath);
		_hpLabel = GetNodeOrNull<Label>(HpLabelPath);
		_lastSpellLabel = GetNodeOrNull<Label>(LastSpellLabelPath);
		_outcomeLabel = GetNodeOrNull<Label>(OutcomeLabelPath);

		_btnResetHp = GetNodeOrNull<Button>(BtnResetHpPath);
		_btnToggleFlying = GetNodeOrNull<Button>(BtnToggleFlyingPath);
		_btnToggleShield = GetNodeOrNull<Button>(BtnToggleShieldPath);
		_btnSpawn1 = GetNodeOrNull<Button>(BtnSpawn1Path);
		_btnSpawn2 = GetNodeOrNull<Button>(BtnSpawn2Path);
		_btnSpawn3 = GetNodeOrNull<Button>(BtnSpawn3Path);
		_btnBack = GetNodeOrNull<Button>(BtnBackPath);

		if (EnemyScene != null && !EnemyScene.IsEmpty)
			_enemyPacked = GD.Load<PackedScene>(EnemyScene);

		if (_targetController == null) GD.PushError("TrainingController: TargetControllerPath inválido.");
		if (_elementController == null) GD.PushError("TrainingController: ElementControllerPath inválido.");
		if (_enemiesRoot == null) GD.PushError("TrainingController: EnemiesRootPath inválido.");

		WireButtons();

		// Escuta casts resolvidos (pra atualizar UI com HIT/MISS/ABSORB etc)
		if (_elementController != null)
			_elementController.CastResolved += OnCastResolved;

		// Atualiza UI inicial
		CallDeferred(nameof(RefreshUi));
	}

	public override void _ExitTree()
	{
		if (_elementController != null)
			_elementController.CastResolved -= OnCastResolved;
	}

	private void WireButtons()
	{
		if (_btnResetHp != null) _btnResetHp.Pressed += () => ResetHp();
		if (_btnToggleFlying != null) _btnToggleFlying.Pressed += () => ToggleFlying();
		if (_btnToggleShield != null) _btnToggleShield.Pressed += () => ToggleShield();
		if (_btnSpawn1 != null) _btnSpawn1.Pressed += () => SpawnCount(1);
		if (_btnSpawn2 != null) _btnSpawn2.Pressed += () => SpawnCount(2);
		if (_btnSpawn3 != null) _btnSpawn3.Pressed += () => SpawnCount(3);

		if (_btnBack != null)
		{
			_btnBack.Pressed += () =>
			{
				GetTree().ChangeSceneToFile("res://Scenes/UI/MainMenu.tscn");
			};
		}
	}

	private void OnCastResolved(CastOutcome outcome, SpellDefinition spell, Enemy target)
	{
		if (_lastSpellLabel != null)
		{
			if (spell == null) _lastSpellLabel.Text = "Last: -";
			else _lastSpellLabel.Text = $"Last: {spell.Id} ({spell.Name})";
		}

		if (_outcomeLabel != null)
			_outcomeLabel.Text = $"Outcome: {outcome}";

		RefreshUi();
	}

	private void RefreshUi()
	{
		var t = _targetController != null ? _targetController.CurrentTarget : null;

		if (_targetLabel != null)
			_targetLabel.Text = t != null ? $"Target: {t.Name}" : "Target: -";

		if (_hpLabel != null)
		{
			if (t == null) _hpLabel.Text = "HP: -";
			else _hpLabel.Text = $"HP: {t.Hp}/{t.MaxHp} | Flying={t.IsFlying} | ShieldOn={_shieldOn}";
		}
	}

	private void ResetHp()
	{
		var t = _targetController?.CurrentTarget;
		if (t == null || !GodotObject.IsInstanceValid(t)) return;

		// Forma simples e limpa: recria o alvo com HP default (evita mexer em setter privado do Hp)
		// Então, no training, a forma “oficial” é: respawn do boneco.
		// (Sem gambiarra de reflection / set privado)
		SpawnCount(_enemiesRoot != null ? _enemiesRoot.GetChildCount() : 1);
	}

	private void ToggleFlying()
	{
		var t = _targetController?.CurrentTarget;
		if (t == null || !GodotObject.IsInstanceValid(t)) return;

		t.IsFlying = !t.IsFlying;
		RefreshUi();
	}

	private void ToggleShield()
	{
		_shieldOn = !_shieldOn;

		// aqui a gente “liga/desliga” shield de forma central:
		// por enquanto, só alterna se ShieldActive terá algo ou ficará vazio.
		// Depois você pluga no seu ShieldController real.
		foreach (var e in GetAllEnemies())
		{
			if (e == null) continue;
			e.ShieldActive.Clear();
			if (_shieldOn)
			{
				// exemplo: shield básico pra testar
				e.ShieldActive.Add(ElementType.Fire);
			}
		}

		RefreshUi();
	}

	private List<Enemy> GetAllEnemies()
	{
		var list = new List<Enemy>();
		if (_enemiesRoot == null) return list;

		foreach (var child in _enemiesRoot.GetChildren())
		{
			if (child is Enemy e && GodotObject.IsInstanceValid(e))
				list.Add(e);
		}
		return list;
	}

	private void SpawnCount(int count)
	{
		if (_enemiesRoot == null) return;

		// Remove existentes
		foreach (var child in _enemiesRoot.GetChildren())
		{
			if (child is Node n)
				n.QueueFree();
		}

		// Se não tiver PackedScene setado, tenta achar Enemy.tscn no caminho padrão
		if (_enemyPacked == null)
		{
			// Ajuste esse caminho se seu Enemy.tscn estiver em outro lugar
			_enemyPacked = GD.Load<PackedScene>("res://Scenes/Enemies/Enemy.tscn");
		}

		if (_enemyPacked == null)
		{
			GD.PushError("TrainingController: EnemyScene não setado e não encontrei res://Scenes/Enemies/Enemy.tscn");
			return;
		}

		// Spawns
		for (int i = 0; i < count; i++)
		{
			var inst = _enemyPacked.Instantiate<Enemy>();
			inst.Name = $"Dummy_{i + 1}";
			_enemiesRoot.AddChild(inst);

			// posições simples e simétricas
			inst.GlobalPosition = new Vector2(960 + (i - (count - 1) * 0.5f) * 220f, 540);

			// HP default
			inst.MaxHp = DefaultHp;

			// shield on/off
			inst.ShieldActive.Clear();
			if (_shieldOn) inst.ShieldActive.Add(ElementType.Fire);
		}

		// dá 1 frame pro TargetController registrar e selecionar
		CallDeferred(nameof(RefreshUi));
	}
}
