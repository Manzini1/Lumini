//using Godot;
//using System.Collections.Generic;
//
//public partial class EnemySpawner : Node
//{
	//[ExportCategory("Refs")]
	//[Export] public NodePath EnemiesParentPath;
//
	//[ExportCategory("Spawn Setup")]
	//[Export] public PackedScene EnemyScene; // Enemy.tscn
	//[Export] public Godot.Collections.Array<NodePath> SlotMarkers = new(); // Marker2D paths
	//[Export] public Godot.Collections.Array<EnemyData> EnemiesToSpawn = new(); // lista de .tres
//
	//private Node _enemiesParent;
//
	//public override void _Ready()
	//{
		//_enemiesParent = GetNodeOrNull<Node>(EnemiesParentPath);
		//if (_enemiesParent == null)
		//{
			//GD.PushError("EnemySpawner: EnemiesParentPath não setado ou inválido.");
			//return;
		//}
//
		//if (EnemyScene == null)
		//{
			//GD.PushError("EnemySpawner: EnemyScene não setado.");
			//return;
		//}
//
		//SpawnAll();
	//}
//
	//private void SpawnAll()
	//{
		//// limpa o que já existe (útil no Training)
		//foreach (var c in _enemiesParent.GetChildren())
			//if (c is Node child) child.QueueFree();
//
		//// 1) resolve markers e ordena por X (esquerda -> direita)
		//var markers = new List<Marker2D>();
		//for (int i = 0; i < SlotMarkers.Count; i++)
		//{
			//var m = GetNodeOrNull<Marker2D>(SlotMarkers[i]);
			//if (m != null) markers.Add(m);
			//else GD.PushWarning($"EnemySpawner: SlotMarkers[{i}] inválido.");
		//}
//
		//markers.Sort((a, b) => a.GlobalPosition.X.CompareTo(b.GlobalPosition.X));
//
		//int count = Mathf.Min(EnemiesToSpawn.Count, markers.Count);
//
		//for (int slot = 0; slot < count; slot++)
		//{
			//var data = EnemiesToSpawn[slot];
			//var marker = markers[slot];
//
			//var enemy = EnemyScene.Instantiate<Enemy>();
			//enemy.Name = $"Enemy_{slot}";
			//enemy.Data = data;
//
			//// ✅ slot coerente com esquerda->direita
			//enemy.SlotIndex = slot;
//
			//_enemiesParent.AddChild(enemy);
			//enemy.GlobalPosition = marker.GlobalPosition;
//
			//GD.Print($"[Spawner] Spawn {enemy.Name} slot={enemy.SlotIndex} atX={enemy.GlobalPosition.X:0}");
		//}
//
		//GD.Print($"[Spawner] Spawned {count} enemies.");
	//}
//}
