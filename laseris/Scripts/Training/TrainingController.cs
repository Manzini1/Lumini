//using Godot;
//using System;
//using System.Collections.Generic;
//
//public partial class TrainingController : Node
//{
	//[ExportCategory("Refs")]
	//[Export] public NodePath TargetControllerPath;   // World/TargetController
	//[Export] public NodePath ElementControllerPath;  // HUD/ElementController (ajuste)
	//[Export] public NodePath EnemiesRootPath;        // World/Enemies (Node2D)
//
	//[ExportCategory("Enemy Scene")]
	//[Export(PropertyHint.File, "*.tscn")]
	//public string EnemyScenePath = "res://Scenes/enemy.tscn"; // ajuste se precisar
//
	//[ExportCategory("UI")]
	//[Export] public NodePath TargetLabelPath;
	//[Export] public NodePath HpLabelPath;
	//[Export] public NodePath LastSpellLabelPath;
	//[Export] public NodePath OutcomeLabelPath;
//
	//[Export] public NodePath BtnResetHpPath;
	//[Export] public NodePath BtnToggleFlyingPath;
	//[Export] public NodePath BtnToggleShieldPath;
	//[Export] public NodePath BtnSpawn1Path;
	//[Export] public NodePath BtnSpawn2Path;
	//[Export] public NodePath BtnSpawn3Path;
	//[Export] public NodePath BtnBackPath;
//
	//[ExportCategory("Training Settings")]
	//[Export] public int DefaultHp = 1000;
	//[Export] public bool StartWithShieldOn = true;
//
	//[ExportCategory("Layout")]
	//[Export] public Vector2 SpawnCenter = new(960, 540);
	//[Export] public float SpawnSpacing = 220f;
//
	//private TargetController _targetController;
////	private ElementController _elementController;
	//private Node2D _enemiesRoot;
	//private PackedScene _enemyPacked;
//
	//private Label _targetLabel;
	//private Label _hpLabel;
	//private Label _lastSpellLabel;
	//private Label _outcomeLabel;
//
	//private Button _btnResetHp;
	//private Button _btnToggleFlying;
	//private Button _btnToggleShield;
	//private Button _btnSpawn1;
	//private Button _btnSpawn2;
	//private Button _btnSpawn3;
	//private Button _btnBack;
//
	//private bool _shieldOn;
//
	//// ✅ alvo “observado” para HP label atualizar até em dano físico
	//private Enemy _watchedTarget;
//
	//public override void _Ready()
	//{
		//_shieldOn = StartWithShieldOn;
//
		//_targetController = GetNodeOrNull<TargetController>(TargetControllerPath);
		//_elementController = GetNodeOrNull<ElementController>(ElementControllerPath);
		//_enemiesRoot = GetNodeOrNull<Node2D>(EnemiesRootPath);
//
		//_targetLabel = GetNodeOrNull<Label>(TargetLabelPath);
		//_hpLabel = GetNodeOrNull<Label>(HpLabelPath);
		//_lastSpellLabel = GetNodeOrNull<Label>(LastSpellLabelPath);
		//_outcomeLabel = GetNodeOrNull<Label>(OutcomeLabelPath);
//
		//_btnResetHp = GetNodeOrNull<Button>(BtnResetHpPath);
		//_btnToggleFlying = GetNodeOrNull<Button>(BtnToggleFlyingPath);
		//_btnToggleShield = GetNodeOrNull<Button>(BtnToggleShieldPath);
		//_btnSpawn1 = GetNodeOrNull<Button>(BtnSpawn1Path);
		//_btnSpawn2 = GetNodeOrNull<Button>(BtnSpawn2Path);
		//_btnSpawn3 = GetNodeOrNull<Button>(BtnSpawn3Path);
		//_btnBack = GetNodeOrNull<Button>(BtnBackPath);
//
		//if (_targetController == null) GD.PushError("TrainingController: TargetControllerPath inválido.");
		//if (_elementController == null) GD.PushError("TrainingController: ElementControllerPath inválido.");
		//if (_enemiesRoot == null) GD.PushError("TrainingController: EnemiesRootPath inválido.");
//
		//if (!string.IsNullOrWhiteSpace(EnemyScenePath))
			//_enemyPacked = GD.Load<PackedScene>(EnemyScenePath);
//
		//if (_enemyPacked == null)
			//GD.PushError($"TrainingController: não consegui carregar EnemyScenePath: '{EnemyScenePath}'");
//
		//WireButtons();
//
		//if (_elementController != null)
			//_elementController.CastResolved += OnCastResolved;
//
		//CallDeferred(nameof(RefreshUi));
	//}
//
	//public override void _ExitTree()
	//{
		//if (_elementController != null)
			//_elementController.CastResolved -= OnCastResolved;
//
		//StopWatchingTarget();
	//}
//
	//// ✅ simples/robusto: verifica troca de alvo sempre
	//public override void _Process(double delta)
	//{
		//EnsureWatchingCurrentTarget();
	//}
//
	//private void WireButtons()
	//{
		//if (_btnResetHp != null) _btnResetHp.Pressed += ResetHp;
		//if (_btnToggleFlying != null) _btnToggleFlying.Pressed += ToggleFlying;
		//if (_btnToggleShield != null) _btnToggleShield.Pressed += ToggleShield;
//
		//if (_btnSpawn1 != null) _btnSpawn1.Pressed += () => SpawnCount(1);
		//if (_btnSpawn2 != null) _btnSpawn2.Pressed += () => SpawnCount(2);
		//if (_btnSpawn3 != null) _btnSpawn3.Pressed += () => SpawnCount(3);
//
		//if (_btnBack != null)
		//{
			//_btnBack.Pressed += () =>
			//{
				//GetTree().ChangeSceneToFile("res://Scenes/UI/MainMenu.tscn");
			//};
		//}
	//}
//
	//private void OnCastResolved(CastOutcome outcome, SpellDefinition spell, Enemy target)
	//{
		//if (_lastSpellLabel != null)
		//{
			//if (spell == null) _lastSpellLabel.Text = "Last: -";
			//else _lastSpellLabel.Text = $"Last: {spell.Id} ({spell.Name})";
		//}
//
		//if (_outcomeLabel != null)
			//_outcomeLabel.Text = $"Outcome: {outcome}";
//
		//// ✅ IMPORTANTE:
		//// NÃO chame shield.NotifySpellResolved aqui,
		//// porque o Enemy.TakeSpellHit já chama por dentro.
		//// (evita duplicar VFX/rotations/logs)
//
		//RefreshUi();
	//}
//
	//// ---------------- HP WATCHER (AQUI ESTÁ O FIX) ----------------
//
	//private void EnsureWatchingCurrentTarget()
	//{
		//var t = _targetController != null ? _targetController.CurrentTarget : null;
//
		//if (t == _watchedTarget) return;
//
		//StopWatchingTarget();
		//_watchedTarget = t;
//
		//if (_watchedTarget != null && GodotObject.IsInstanceValid(_watchedTarget))
		//{
			//_watchedTarget.HpChanged += OnWatchedHpChanged;
//
			//// força refresh imediato do HP label
			//OnWatchedHpChanged(_watchedTarget, _watchedTarget.Hp, _watchedTarget.MaxHp);
		//}
//
		//// atualiza TargetLabel também
		//RefreshUi();
	//}
//
	//private void StopWatchingTarget()
	//{
		//if (_watchedTarget != null && GodotObject.IsInstanceValid(_watchedTarget))
			//_watchedTarget.HpChanged -= OnWatchedHpChanged;
//
		//_watchedTarget = null;
	//}
//
	//private void OnWatchedHpChanged(Enemy who, int hp, int maxHp)
	//{
		//if (who != _watchedTarget) return;
		//UpdateHpLabel(who);
	//}
//
	//// ---------------- UI ----------------
//
	//private void RefreshUi()
	//{
		//var t = _targetController != null ? _targetController.CurrentTarget : null;
//
		//if (_targetLabel != null)
			//_targetLabel.Text = t != null ? $"Target: {t.Name}" : "Target: -";
//
		//// HP agora é atualizado por evento, mas chamamos aqui também por segurança
		//UpdateHpLabel(t);
	//}
//
	//private void UpdateHpLabel(Enemy t)
	//{
		//if (_hpLabel == null) return;
//
		//if (t == null || !GodotObject.IsInstanceValid(t))
		//{
			//_hpLabel.Text = "HP: -";
			//return;
		//}
//
		//string shieldTxt = "-";
		//if (t.Shield != null && t.Shield.Active != null && t.Shield.Active.Count > 0)
			//shieldTxt = string.Join(", ", t.Shield.Active);
//
		//_hpLabel.Text = $"HP: {t.Hp}/{t.MaxHp} | Flying={t.IsFlying} | ShieldOn={_shieldOn} | Shield=[{shieldTxt}]";
	//}
//
	//// ---------------- BUTTON ACTIONS ----------------
//
	//private void ResetHp()
	//{
		//var t = _targetController?.CurrentTarget;
		//if (t == null || !GodotObject.IsInstanceValid(t)) return;
//
		//t.Heal(int.MaxValue);
		//// HpChanged vai atualizar automaticamente
		//RefreshUi();
	//}
//
	//private void ToggleFlying()
	//{
		//var t = _targetController?.CurrentTarget;
		//if (t == null || !GodotObject.IsInstanceValid(t)) return;
//
		//t.IsFlying = !t.IsFlying;
		//RefreshUi();
	//}
//
	//private void ToggleShield()
	//{
		//_shieldOn = !_shieldOn;
//
		//foreach (var e in GetAllEnemies())
		//{
			//var shield = e.Shield;
			//if (shield == null) continue;
//
			//if (_shieldOn)
			//{
				//shield.RefreshRandom();
			//}
			//else
			//{
				//// ⚠️ isso desliga mecanicamente, MAS o visual só some se o seu visual
				//// escutar Changed e tratar lista vazia (precisa de um "emit" do ShieldController).
				//shield.Active.Clear();
			//}
		//}
//
		//RefreshUi();
	//}
//
	//private List<Enemy> GetAllEnemies()
	//{
		//var list = new List<Enemy>();
		//if (_enemiesRoot == null) return list;
//
		//foreach (var child in _enemiesRoot.GetChildren())
		//{
			//if (child is Enemy e && GodotObject.IsInstanceValid(e))
				//list.Add(e);
		//}
//
		//return list;
	//}
//
	//private void SpawnCount(int count)
	//{
		//if (_enemiesRoot == null) return;
		//if (_enemyPacked == null) return;
//
		//StopWatchingTarget(); // ✅ evita ficar inscrito em alvo que vai ser QueueFree
//
		//foreach (var child in _enemiesRoot.GetChildren())
		//{
			//if (child is Node n)
				//n.QueueFree();
		//}
//
		//for (int i = 0; i < count; i++)
		//{
			//var inst = _enemyPacked.Instantiate<Enemy>();
			//inst.Name = $"Dummy_{i + 1}";
			//_enemiesRoot.AddChild(inst);
//
			//float offset = (i - (count - 1) * 0.5f) * SpawnSpacing;
			//inst.GlobalPosition = SpawnCenter + new Vector2(offset, 0);
//
			//inst.MaxHp = DefaultHp;
			//inst.IsFlying = false;
		//}
//
		//CallDeferred(nameof(ApplyShieldStateAfterSpawn));
		//CallDeferred(nameof(RefreshUi));
	//}
//
	//private void ApplyShieldStateAfterSpawn()
	//{
		//foreach (var e in GetAllEnemies())
		//{
			//var shield = e.Shield;
			//if (shield == null) continue;
//
			//if (_shieldOn) shield.RefreshRandom();
			//else shield.Active.Clear();
		//}
	//}
//}
