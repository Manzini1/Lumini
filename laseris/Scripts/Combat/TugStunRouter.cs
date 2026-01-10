using Godot;
using System;

public partial class TugStunRouter : Node
{
	[ExportCategory("Refs")]
	[Export] public NodePath TugPath = "/root/Tug"; // seu autoload

	[ExportCategory("Find Mage/Targets")]
	[Export] public string MageGroup = "mage";
	[Export] public string EnemiesGroup = "Enemies"; // seus enemies já entram nisso no Enemy._Ready()

	[ExportCategory("Behavior")]
	[Export] public bool FreezeTugDuringStun = true;
	[Export] public float FreezeExtraSeconds = 0.15f; // margem p/ projéteis "em voo"

	private TugManager _tug;

	public override void _Ready()
	{
		_tug = GetNodeOrNull<TugManager>(TugPath);
		if (_tug == null)
		{
			GD.PushError($"[TugStunRouter] Não achei TugManager em '{TugPath}'. (Autoload?)");
			return;
		}

		_tug.Broken += OnTugBroken;
		GD.Print("[TugStunRouter] Listening Tug.Broken");
	}

	public override void _ExitTree()
	{
		if (_tug != null)
			_tug.Broken -= OnTugBroken;
	}

	private void OnTugBroken(TugManager.TugBreak brk)
	{
		// Quem perde?
		bool playerLost = brk.Loser == TugManager.TugLoser.Player;

		var mage = GetTree().GetFirstNodeInGroup(MageGroup) as Mage;

		if (playerLost)
		{
			if (mage != null)
				mage.ForceStun(mage.StunSeconds, $"tug break: {brk.Reason}");
		}
		else
		{
			// inimigo perdeu -> você escolhe: target atual ou todos.
			// Como o Tug é global e você tem múltiplos enemies, o default seguro é: stun em TODOS.
			var enemies = GetTree().GetNodesInGroup(EnemiesGroup);
			foreach (var n in enemies)
			{
				if (n is Enemy e && GodotObject.IsInstanceValid(e))
					e.ForceStun(1.5f, "tug break"); // ajuste o tempo que você quer pro inimigo
			}
		}

		// ✅ Congela tug pra projéteis atrasados não empurrarem a bolinha depois do reset
		if (FreezeTugDuringStun && _tug != null)
		{
			float freeze = playerLost
				? (mage != null ? mage.StunSeconds : 2f)
				: 1.5f;

			_tug.Freeze(freeze + FreezeExtraSeconds, "freeze during stun");
		}
	}
}
