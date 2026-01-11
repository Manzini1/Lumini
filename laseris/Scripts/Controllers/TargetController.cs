using Godot;
using System;
using System.Collections.Generic;

public partial class TargetController : Node
{
	[Export] public string EnemyGroupName = "Enemies";
	[Export] public string NextTargetAction = "next_target";

	private readonly List<Enemy> _enemies = new();
	private int _currentIndex = -1;

	public event Action<Enemy> TargetChanged;

	public Enemy CurrentTarget =>
		(_currentIndex >= 0 && _currentIndex < _enemies.Count)
			? _enemies[_currentIndex]
			: null;

	public override void _Ready()
	{
		GD.Print("TargetController iniciado");
		AddToGroup("target_controller");

		GetTree().NodeAdded += OnNodeAdded;
		GetTree().NodeRemoved += OnNodeRemoved;

		CallDeferred(nameof(InitialScan));
	}

	public override void _ExitTree()
	{
		if (GetTree() != null)
		{
			GetTree().NodeAdded -= OnNodeAdded;
			GetTree().NodeRemoved -= OnNodeRemoved;
		}

		for (int i = 0; i < _enemies.Count; i++)
			UnsubscribeEnemySignals(_enemies[i]);

		_enemies.Clear();
		_currentIndex = -1;
		TargetChanged?.Invoke(null);
	}

	public override void _Process(double delta)
	{
		// mais confiável que “tabDownLastFrame”
		if (Input.IsActionJustPressed(NextTargetAction))
			SelectNext();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mb) return;
		if (!mb.Pressed) return;
		if (mb.ButtonIndex != MouseButton.Left) return;

		TryPickEnemyUnderMouse();
	}

	// -------------------- Scan inicial --------------------

	private void InitialScan()
	{
		_enemies.Clear();
		_currentIndex = -1;

		foreach (Node n in GetTree().GetNodesInGroup(EnemyGroupName))
		{
			if (n is Enemy e && IsValidEnemy(e))
			{
				_enemies.Add(e);
				SubscribeEnemySignals(e);
			}
		}

		SortEnemiesKeepCurrent();

		GD.Print($"Inimigos encontrados: {_enemies.Count}");

		if (_enemies.Count > 0)
			SelectIndex(0);
		else
			TargetChanged?.Invoke(null);
	}

	// -------------------- Spawn/Despawn --------------------

	private void OnNodeAdded(Node n)
	{
		if (n is Enemy)
			CallDeferred(nameof(TryAddEnemyDeferred), n);
	}

	private void TryAddEnemyDeferred(Node n)
	{
		if (n is not Enemy e) return;
		if (!e.IsInGroup(EnemyGroupName)) return;
		if (!IsValidEnemy(e)) return;
		if (_enemies.Contains(e)) return;

		_enemies.Add(e);
		SubscribeEnemySignals(e);

		SortEnemiesKeepCurrent();

		GD.Print($"Enemy registrado: {e.Name} (total: {_enemies.Count})");

		if (CurrentTarget == null)
			SelectIndex(0); // SelectIndex dispara TargetChanged
	}

	private void OnNodeRemoved(Node n)
	{
		if (n is Enemy e)
			RemoveEnemy(e);
	}

	private void RemoveEnemy(Enemy e)
	{
		int idx = _enemies.IndexOf(e);
		if (idx < 0) return;

		bool wasSelected = (idx == _currentIndex);

		if (wasSelected)
			e.SetSelected(false);

		UnsubscribeEnemySignals(e);
		_enemies.RemoveAt(idx);

		if (_enemies.Count == 0)
		{
			_currentIndex = -1;
			TargetChanged?.Invoke(null);
			return;
		}

		// ajusta índice se removeu antes do atual
		if (idx < _currentIndex)
			_currentIndex--;

		SortEnemiesKeepCurrent();

		// se removeu o selecionado, escolhe o “mesmo índice” (clamp)
		if (wasSelected)
		{
			_currentIndex = Mathf.Clamp(_currentIndex, 0, _enemies.Count - 1);
			SelectIndex(_currentIndex);
		}
	}

	// -------------------- Ordenação determinística --------------------

	private void SortEnemiesKeepCurrent()
	{
		var current = CurrentTarget;

		_enemies.Sort((a, b) =>
		{
			if (a == null || b == null) return 0;

			// ordem determinística: esquerda -> direita
			// (se quiser “direita -> esquerda”, inverte CompareTo)
			return a.GlobalPosition.X.CompareTo(b.GlobalPosition.X);
		});

		_currentIndex = current != null ? _enemies.IndexOf(current) : (_enemies.Count > 0 ? 0 : -1);
	}

	// -------------------- Mouse picking --------------------

	private void TryPickEnemyUnderMouse()
	{
		Vector2 mousePos = GetViewport().GetMousePosition();
		Vector2 worldPos = GetViewport().GetCanvasTransform().AffineInverse() * mousePos;

		var world2D = GetViewport().World2D;
		if (world2D == null) return;

		var space = world2D.DirectSpaceState;

		var query = new PhysicsPointQueryParameters2D
		{
			Position = worldPos,
			CollideWithAreas = true,
			CollideWithBodies = false
		};

		var results = space.IntersectPoint(query, 16);

		foreach (Godot.Collections.Dictionary hit in results)
		{
			if (!hit.ContainsKey("collider"))
				continue;

			Variant colliderVar = (Variant)hit["collider"];
			GodotObject colliderObj = colliderVar.AsGodotObject();

			if (colliderObj is Area2D area)
			{
				Enemy enemy = area.GetParent() as Enemy;

				if (enemy != null && IsValidEnemy(enemy))
				{
					SelectEnemy(enemy);
					return;
				}
			}
		}
	}

	private void SelectEnemy(Enemy enemy)
	{
		int idx = _enemies.IndexOf(enemy);
		if (idx < 0) return;
		SelectIndex(idx);
	}

	// -------------------- Seleção --------------------

	private void SelectNext()
	{
		if (_enemies.Count == 0)
		{
			GD.Print("Nenhum inimigo encontrado!");
			_currentIndex = -1;
			TargetChanged?.Invoke(null);
			return;
		}

		// garante ordem atualizada antes de avançar
		SortEnemiesKeepCurrent();

		int nextIndex = _currentIndex + 1;
		if (nextIndex >= _enemies.Count) nextIndex = 0;

		SelectIndex(nextIndex);
	}

	private void SelectIndex(int index)
	{
		if (_enemies.Count == 0)
		{
			_currentIndex = -1;
			TargetChanged?.Invoke(null);
			return;
		}

		index = Mathf.Clamp(index, 0, _enemies.Count - 1);

		var previous = CurrentTarget;
		if (previous != null && IsValidEnemy(previous))
			previous.SetSelected(false);

		_currentIndex = index;

		var current = CurrentTarget;
		if (current != null && IsValidEnemy(current))
		{
			current.SetSelected(true);
			GD.Print($"Alvo atual: {current.Name}");
			TargetChanged?.Invoke(current);
		}
		else
		{
			RepairSelection();
		}
	}

	private void RepairSelection()
	{
		RemoveInvalids();

		if (_enemies.Count == 0)
		{
			_currentIndex = -1;
			TargetChanged?.Invoke(null);
			return;
		}

		_currentIndex = Mathf.Clamp(_currentIndex, 0, _enemies.Count - 1);
		SelectIndex(_currentIndex);
	}

	private void RemoveInvalids()
	{
		for (int i = _enemies.Count - 1; i >= 0; i--)
		{
			if (!IsValidEnemy(_enemies[i]))
			{
				UnsubscribeEnemySignals(_enemies[i]);
				_enemies.RemoveAt(i);
			}
		}
	}

	// -------------------- Eventos do Enemy --------------------

	private void SubscribeEnemySignals(Enemy e)
	{
		if (e == null) return;

		if (!e.IsConnected(Enemy.SignalName.Died, Callable.From<Enemy>(OnEnemyDied)))
			e.Connect(Enemy.SignalName.Died, Callable.From<Enemy>(OnEnemyDied));
	}

	private void UnsubscribeEnemySignals(Enemy e)
	{
		if (e == null) return;
		if (!GodotObject.IsInstanceValid(e)) return;

		if (e.IsConnected(Enemy.SignalName.Died, Callable.From<Enemy>(OnEnemyDied)))
			e.Disconnect(Enemy.SignalName.Died, Callable.From<Enemy>(OnEnemyDied));
	}

	private void OnEnemyDied(Enemy who)
	{
		if (who == null) return;

		int deadIndex = _enemies.IndexOf(who);
		if (deadIndex < 0) return;

		bool wasSelected = (deadIndex == _currentIndex);

		RemoveEnemy(who);

		if (!wasSelected) return;

		if (_enemies.Count > 0)
		{
			_currentIndex = Mathf.Clamp(_currentIndex, 0, _enemies.Count - 1);
			SelectIndex(_currentIndex);
		}
	}

	// -------------------- Validação --------------------

	private bool IsValidEnemy(Enemy e)
	{
		if (e == null) return false;
		if (!GodotObject.IsInstanceValid(e)) return false;
		if (e.IsQueuedForDeletion()) return false;
		if (e.IsDead) return false;
		return true;
	}
}
