using Godot;
using System.Collections.Generic;

public partial class TargetController : Node
{
	private List<Enemy> _enemies = new();
	private int _currentIndex = 0;

	public Enemy CurrentTarget { get; private set; }

	// Ajuste no Inspector se seu grupo for "enemies" ou "Enemies"
	[Export] public string EnemyGroupName = "enemies";

	public override void _Ready()
	{
		GD.Print("TargetController iniciado");
		RebuildEnemyList();

		if (_enemies.Count > 0)
			SelectEnemy(0);
		else
			GD.Print($"Nenhum inimigo encontrado no grupo '{EnemyGroupName}'.");
	}

	public override void _Process(double delta)
	{
		// ✅ Se o alvo morreu/foi removido, seleciona automaticamente outro
		if (!IsAlive(CurrentTarget))
		{
			RebuildEnemyList();

			if (_enemies.Count > 0)
			{
				// tenta manter o índice, mas garante que está dentro do range
				_currentIndex = Mathf.Clamp(_currentIndex, 0, _enemies.Count - 1);
				SelectEnemy(_currentIndex);
			}
			else
			{
				CurrentTarget = null;
			}
		}
	}

	public override void _Input(InputEvent e)
	{
		if (e is InputEventKey key && key.Pressed && !key.Echo)
		{
			if (key.Keycode == Key.Tab)
				SelectNext();
		}
	}

	private void SelectNext()
	{
		RebuildEnemyList();
		if (_enemies.Count == 0)
		{
			CurrentTarget = null;
			return;
		}

		// se o índice atual está fora, corrige
		if (_currentIndex < 0 || _currentIndex >= _enemies.Count)
			_currentIndex = 0;

		int next = (_currentIndex + 1) % _enemies.Count;
		SelectEnemy(next);
	}

	private void SelectEnemy(int index)
	{
		RebuildEnemyList();
		if (_enemies.Count == 0)
		{
			CurrentTarget = null;
			return;
		}

		index = Mathf.Clamp(index, 0, _enemies.Count - 1);

		// desmarca todos válidos
		foreach (var enemy in _enemies)
		{
			if (IsAlive(enemy))
				enemy.SetSelected(false);
		}

		_currentIndex = index;
		CurrentTarget = _enemies[_currentIndex];

		if (IsAlive(CurrentTarget))
			CurrentTarget.SetSelected(true);

		GD.Print($"Alvo atual: {CurrentTarget?.Name}");
	}

	private void RebuildEnemyList()
	{
		_enemies.Clear();

		// Busca no grupo configurado
		var nodes = GetTree().GetNodesInGroup(EnemyGroupName);

		// Fallback automático caso alguém tenha criado o grupo com caixa diferente
		if (nodes.Count == 0)
		{
			var alt = (EnemyGroupName == "enemies") ? "Enemies" : "enemies";
			var altNodes = GetTree().GetNodesInGroup(alt);

			if (altNodes.Count > 0)
			{
				GD.Print($"Grupo '{EnemyGroupName}' vazio, mas encontrei inimigos em '{alt}'. Vou usar '{alt}'.");
				EnemyGroupName = alt;
				nodes = altNodes;
			}
		}

		foreach (var node in nodes)
		{
			if (node is Enemy enemy && IsAlive(enemy))
				_enemies.Add(enemy);
		}

		// Se o alvo atual não está mais na lista, zera seleção
		if (CurrentTarget != null && !_enemies.Contains(CurrentTarget))
			CurrentTarget = null;
	}

	private bool IsAlive(Enemy e)
	{
		if (e == null) return false;
		if (!GodotObject.IsInstanceValid(e)) return false;
		if (e.IsQueuedForDeletion()) return false;
		return true;
	}
}
