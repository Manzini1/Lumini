using Godot;
using System.Collections.Generic;

public partial class EnemySpawner : Node
{
	[ExportCategory("Refs")]
	[Export] public NodePath EnemiesParentPath;

	[ExportCategory("Spawn Setup")]
	[Export] public PackedScene EnemyScene; // Enemy.tscn
	[Export] public Godot.Collections.Array<NodePath> SlotMarkers = new(); // Marker2D paths
	[Export] public Godot.Collections.Array<EnemyData> EnemiesToSpawn = new(); // lista de .tres

	private Node _enemiesParent;

	public override void _Ready()
	{
		_enemiesParent = GetNodeOrNull<Node>(EnemiesParentPath);
		if (_enemiesParent == null)
		{
			GD.PushError("EnemySpawner: EnemiesParentPath não setado ou inválido.");
			return;
		}

		if (EnemyScene == null)
		{
			GD.PushError("EnemySpawner: EnemyScene não setado.");
			return;
		}

		SpawnAll();
	}

	private void SpawnAll()
	{
		// limpa o que já existe (opcional, mas útil no Training)
		foreach (var c in _enemiesParent.GetChildren())
			if (c is Node child) child.QueueFree();

		int count = Mathf.Min(EnemiesToSpawn.Count, SlotMarkers.Count);

		for (int i = 0; i < count; i++)
		{
			var data = EnemiesToSpawn[i];
			var marker = GetNodeOrNull<Marker2D>(SlotMarkers[i]);

			if (marker == null)
			{
				GD.PushWarning($"EnemySpawner: SlotMarkers[{i}] inválido.");
				continue;
			}

			var enemy = EnemyScene.Instantiate<Enemy>();
			enemy.Data = data;

			_enemiesParent.AddChild(enemy);
			enemy.GlobalPosition = marker.GlobalPosition; // ✅ posição correta do root
		}

		GD.Print($"[Spawner] Spawned {count} enemies.");
	}
}
