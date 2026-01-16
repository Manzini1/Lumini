//using Godot;
//using System;
//
//public partial class TugStunRouter : Node
//{
	//[ExportCategory("Lookup (recommended: groups)")]
	//[Export] public string TugManagerGroup = "tug_manager";
	//[Export] public string TargetControllerGroup = "target_controller";
	//[Export] public string MageGroup = "mage";
//
	//[ExportCategory("Stun")]
	//[Export] public float PlayerStunSeconds = 2.0f;
	//[Export] public float EnemyStunSeconds = 1.2f;
//
	//[ExportCategory("Debug")]
	//[Export] public bool VerboseLogs = false;
//
	//private TugManager _tug;
	//private TargetController _targetController;
	//private Mage _mage;
//
	//public override void _Ready()
	//{
		//ResolveRefs();
//
		//if (_tug == null)
		//{
			//GD.PushWarning("[TugStunRouter] TugManager não encontrado. Coloque TugManager no group 'tug_manager'.");
			//return;
		//}
//
		//// ✅ Conecta UMA vez
		//_tug.Broken -= OnBroken; // evita duplicar
		//_tug.Broken += OnBroken;
//
		//if (VerboseLogs)
			//GD.Print("[TugStunRouter] Ready. Listening to TugManager.Broken");
	//}
//
	//public override void _Process(double delta)
	//{
		//// Autoload pode iniciar antes da cena montar.
		//// Então tenta “pegar” refs que faltam sem spammar warning.
		//if (_targetController == null || !GodotObject.IsInstanceValid(_targetController))
			//_targetController = GetTree().GetFirstNodeInGroup(TargetControllerGroup) as TargetController;
//
		//if (_mage == null || !GodotObject.IsInstanceValid(_mage))
			//_mage = GetTree().GetFirstNodeInGroup(MageGroup) as Mage;
	//}
//
	//private void ResolveRefs()
	//{
		//_tug = GetTree().GetFirstNodeInGroup(TugManagerGroup) as TugManager;
		//_targetController = GetTree().GetFirstNodeInGroup(TargetControllerGroup) as TargetController;
		//_mage = GetTree().GetFirstNodeInGroup(MageGroup) as Mage;
//
		//// warning só informativo (não aborta)
		//if (_targetController == null)
			//GD.PushWarning("[TugStunRouter] TargetController não encontrado (group 'target_controller'). Vou tentar resolver depois.");
//
		//if (_mage == null)
			//GD.PushWarning("[TugStunRouter] Mage não encontrado (group 'mage'). Vou tentar resolver depois.");
	//}
//
	//private void OnBroken(TugManager.TugBreak b)
	//{
		//if (VerboseLogs)
			//GD.Print($"[TugStunRouter] BROKEN loser={b.Loser} value={0} reason={b.Reason}");
//
		//// player perdeu -> stun no player
		//if (b.Loser == TugManager.TugLoser.Player)
		//{
			//if (_mage == null || !GodotObject.IsInstanceValid(_mage))
			//{
				//GD.PushWarning("[TugStunRouter] Não consegui aplicar stun no player: Mage null.");
				//return;
			//}
//
			//_mage.ForceStun(PlayerStunSeconds, $"tug lost ({b.Reason})");
			//return;
		//}
//
		//// enemy perdeu -> tenta stun no alvo atual (se existir)
		//var enemy = _targetController?.CurrentTarget;
		//if (enemy == null || !GodotObject.IsInstanceValid(enemy))
		//{
			//GD.PushWarning("[TugStunRouter] Enemy stun: não há alvo atual válido.");
			//return;
		//}
//
		//// ✅ Se você ainda não tem stun no Enemy, não quebra: só loga.
		//// Se você criar Enemy.ForceStun depois, é só descomentar.
		////
		//// enemy.ForceStun(EnemyStunSeconds, $"tug lost ({b.Reason})");
//
		//GD.Print("[TugStunRouter] Enemy perdeu o tug, mas Enemy ainda não tem ForceStun() implementado.");
	//}
//}
