using Godot;
using System.Collections.Generic;

public partial class TargetController : Node
{
	[Export] public string EnemyGroupName = "Enemies";
	[Export] public string NextTargetAction = "next_target";

	private readonly List<Enemy> _enemies = new();
	private int _currentIndex = -1;

	private bool _tabDownLastFrame = false;

	public Enemy CurrentTarget =>
		(_currentIndex >= 0 && _currentIndex < _enemies.Count)
			? _enemies[_currentIndex]
			: null;

	public override void _Ready()
	{
		GD.Print("TargetController iniciado");

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
	}

	public override void _Process(double delta)
	{
		// TAB: just pressed manual
		bool tabDown = Input.IsActionPressed(NextTargetAction);
		if (tabDown && !_tabDownLastFrame)
			SelectNext();
		_tabDownLastFrame = tabDown;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mb) return;
		if (!mb.Pressed) return;
		if (mb.ButtonIndex != MouseButton.Left) return;

		TryPickEnemyUnderMouse();
	}

	// --------- Scan inicial ---------

	private void InitialScan()
	{
		_enemies.Clear();
		_currentIndex = -1;

		foreach (Node n in GetTree().GetNodesInGroup(EnemyGroupName))
		{
			if (n is Enemy e && IsValidEnemy(e))
				AddEnemy(e);
		}

		GD.Print($"Inimigos encontrados: {_enemies.Count}");

		if (_enemies.Count > 0)
			SelectIndex(0);
	}

	// --------- Spawn/Despawn ---------

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

		AddEnemy(e);

		if (CurrentTarget == null)
			SelectIndex(0);
	}

	private void OnNodeRemoved(Node n)
	{
		if (n is Enemy e)
			RemoveEnemy(e);
	}

	private void AddEnemy(Enemy e)
	{
		_enemies.Add(e);
		SubscribeEnemySignals(e);

		GD.Print($"Enemy registrado: {e.Name} (total: {_enemies.Count})");
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
			return;
		}

		if (idx < _currentIndex)
			_currentIndex--;

		if (wasSelected)
		{
			_currentIndex = Mathf.Clamp(_currentIndex, 0, _enemies.Count - 1);
			SelectIndex(_currentIndex);
		}
	}

	// --------- Mouse picking (raycast point) ---------

	private void TryPickEnemyUnderMouse()
	{
		// posição do mouse na viewport (screen)
		Vector2 mousePos = GetViewport().GetMousePosition();

		// converte para world (2D) usando o canvas transform
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

			// ✅ Godot 4: valores do Dictionary são Variant
			Variant colliderVar = (Variant)hit["collider"];
			GodotObject colliderObj = colliderVar.AsGodotObject();

			if (colliderObj is Area2D area)
			{
				// padrão: Enemy -> ClickArea (Area2D)
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

	// --------- Seleção / Highlight ---------

	private void SelectNext()
	{
		if (_enemies.Count == 0)
		{
			GD.Print("Nenhum inimigo encontrado!");
			_currentIndex = -1;
			return;
		}

		int nextIndex = _currentIndex + 1;
		if (nextIndex >= _enemies.Count) nextIndex = 0;

		SelectIndex(nextIndex);
	}

	private void SelectIndex(int index)
	{
		if (_enemies.Count == 0)
		{
			_currentIndex = -1;
			return;
		}

		index = Mathf.Clamp(index, 0, _enemies.Count - 1);

		// desmarca anterior antes de mudar índice
		var previous = CurrentTarget;
		if (previous != null && IsValidEnemy(previous))
			previous.SetSelected(false);

		_currentIndex = index;

		var current = CurrentTarget;
		if (current != null && IsValidEnemy(current))
		{
			current.SetSelected(true);
			GD.Print($"Alvo atual: {current.Name}");
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

	// --------- Eventos do Enemy ---------

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

	// --------- Validação ---------

	private bool IsValidEnemy(Enemy e)
	{
		if (e == null) return false;
		if (!GodotObject.IsInstanceValid(e)) return false;
		if (e.IsQueuedForDeletion()) return false;
		if (e.IsDead) return false;
		return true;
	}
}
