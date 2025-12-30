using Godot;
using System;
using System.Collections.Generic;

public partial class TrainingController : Node
{
	[ExportCategory("Refs")]
	[Export] public NodePath TargetControllerPath;   // World/TargetController
	[Export] public NodePath ElementControllerPath;  // HUD/ElementController (ajuste)
	[Export] public NodePath EnemiesRootPath;        // World/Enemies (Node2D)

	[ExportCategory("Enemy Scene")]
	[Export(PropertyHint.File, "*.tscn")]
	public string EnemyScenePath = "res://Scenes/enemy.tscn"; // ajuste se precisar

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
	[Export] public bool StartWithShieldOn = true;

	[ExportCategory("Layout")]
	[Export] public Vector2 SpawnCenter = new(960, 540);
	[Export] public float SpawnSpacing = 220f;

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

		if (_targetController == null) GD.PushError("TrainingController: TargetControllerPath inválido.");
		if (_elementController == null) GD.PushError("TrainingController: ElementControllerPath inválido.");
		if (_enemiesRoot == null) GD.PushError("TrainingController: EnemiesRootPath inválido.");

		if (!string.IsNullOrWhiteSpace(EnemyScenePath))
			_enemyPacked = GD.Load<PackedScene>(EnemyScenePath);

		if (_enemyPacked == null)
			GD.PushError($"TrainingController: não consegui carregar EnemyScenePath: '{EnemyScenePath}'");

		WireButtons();

		if (_elementController != null)
			_elementController.CastResolved += OnCastResolved;

		CallDeferred(nameof(RefreshUi));

		// opcional: já spawnar 3 no começo pra treino ficar vivo
		// SpawnCount(3);
	}

	public override void _ExitTree()
	{
		if (_elementController != null)
			_elementController.CastResolved -= OnCastResolved;
	}

	private void WireButtons()
	{
		if (_btnResetHp != null) _btnResetHp.Pressed += ResetHp;
		if (_btnToggleFlying != null) _btnToggleFlying.Pressed += ToggleFlying;
		if (_btnToggleShield != null) _btnToggleShield.Pressed += ToggleShield;

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

		// ✅ se você quiser que o shield reaja ao resultado do cast, é aqui:
		// (isso mantém o shield “oficial” ligado ao combate)
		if (target != null && GodotObject.IsInstanceValid(target))
		{
			var shield = target.Shield;
			if (shield != null)
				shield.NotifySpellResolved(spell, outcome);
		}

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
			else
			{
				string shieldTxt = "-";
				if (t.Shield != null && t.Shield.Active != null && t.Shield.Active.Count > 0)
					shieldTxt = string.Join(", ", t.Shield.Active);

				_hpLabel.Text = $"HP: {t.Hp}/{t.MaxHp} | Flying={t.IsFlying} | ShieldOn={_shieldOn} | Shield=[{shieldTxt}]";
			}
		}
	}

	private void ResetHp()
	{
		var t = _targetController?.CurrentTarget;
		if (t == null || !GodotObject.IsInstanceValid(t)) return;

		// sem mexer no setter privado: cura até o máximo
		t.Heal(int.MaxValue);
		RefreshUi();
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

		foreach (var e in GetAllEnemies())
		{
			var shield = e.Shield;
			if (shield == null) continue;

			if (_shieldOn)
			{
				// liga: gera escudo válido
				shield.RefreshRandom();
			}
			else
			{
				// desliga: limpa
				shield.Active.Clear();
				// (o visual só vai sumir se seu ShieldVisual tratar lista vazia)
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
		if (_enemyPacked == null) return;

		// remove antigos
		foreach (var child in _enemiesRoot.GetChildren())
		{
			if (child is Node n)
				n.QueueFree();
		}

		// spawn novos
		for (int i = 0; i < count; i++)
		{
			var inst = _enemyPacked.Instantiate<Enemy>();
			inst.Name = $"Dummy_{i + 1}";
			_enemiesRoot.AddChild(inst);

			// posição (centralizado)
			float offset = (i - (count - 1) * 0.5f) * SpawnSpacing;
			inst.GlobalPosition = SpawnCenter + new Vector2(offset, 0);

			// stats básicos
			inst.MaxHp = DefaultHp;
			inst.IsFlying = false;
		}

		// espera 1 frame pro TargetController registrar e selecionar
		CallDeferred(nameof(ApplyShieldStateAfterSpawn));
		CallDeferred(nameof(RefreshUi));
	}

	private void ApplyShieldStateAfterSpawn()
	{
		foreach (var e in GetAllEnemies())
		{
			var shield = e.Shield;
			if (shield == null) continue;

			if (_shieldOn) shield.RefreshRandom();
			else shield.Active.Clear();
		}
	}
}
