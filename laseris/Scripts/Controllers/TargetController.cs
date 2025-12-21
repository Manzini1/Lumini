using Godot;
using System.Collections.Generic;

public partial class TargetController : Node
{
	private readonly List<Enemy> _enemies = new();
	private int _currentIndex = -1;

	public Enemy CurrentTarget =>
		(_currentIndex >= 0 && _currentIndex < _enemies.Count)
		? _enemies[_currentIndex]
		: null;

	public override void _Ready()
	{
		GD.Print("TargetController iniciado");

		// ✅ Rastreia inimigos que entram/saem da árvore (spawn/despawn)
		GetTree().NodeAdded += OnNodeAdded;
		GetTree().NodeRemoved += OnNodeRemoved;

		// ✅ Pega os que já existem (depois de 1 frame)
		CallDeferred(nameof(InitialScan));
	}

	private void InitialScan()
	{
		_enemies.Clear();

		foreach (Node n in GetTree().GetNodesInGroup("Enemies"))
		{
			if (n is Enemy e && IsAlive(e))
				_enemies.Add(e);
		}

		GD.Print($"Inimigos encontrados: {_enemies.Count}");

		if (_enemies.Count > 0 && CurrentTarget == null)
			SelectIndex(0);
	}

	public override void _ExitTree()
	{
		// limpa handlers ao sair
		if (GetTree() != null)
		{
			GetTree().NodeAdded -= OnNodeAdded;
			GetTree().NodeRemoved -= OnNodeRemoved;
		}
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("next_target"))
		{
			if (_enemies.Count == 0)
			{
				GD.Print("Nenhum inimigo encontrado!");
				return;
			}

			SelectNext();
		}

		// ✅ Se o alvo morreu/foi removido, auto-seleciona outro
		if (CurrentTarget != null && !IsAlive(CurrentTarget))
		{
			RemoveInvalids();
			if (_enemies.Count > 0)
				SelectIndex(Mathf.Clamp(_currentIndex, 0, _enemies.Count - 1));
			else
				_currentIndex = -1;
		}
	}

	private void OnNodeAdded(Node n)
	{
		// O NodeAdded dispara antes do _Ready do node às vezes.
		// Então fazemos a inserção “deferred” para garantir que o Enemy já se colocou no grupo.
		if (n is Enemy)
			CallDeferred(nameof(TryAddEnemy), n);
	}

	private void TryAddEnemy(Node n)
	{
		if (n is not Enemy e) return;
		if (!IsAlive(e)) return;

		// Só pega os que estão no grupo "Enemies" (e o Enemy.cs garante isso)
		if (!e.IsInGroup("Enemies")) return;

		if (_enemies.Contains(e)) return;

		_enemies.Add(e);
		GD.Print($"Enemy registrado: {e.Name} (total: {_enemies.Count})");

		// Se não tem alvo, seleciona o primeiro
		if (CurrentTarget == null)
			SelectIndex(0);
	}

	private void OnNodeRemoved(Node n)
	{
		if (n is Enemy e)
		{
			int idx = _enemies.IndexOf(e);
			if (idx >= 0)
			{
				bool wasSelected = (idx == _currentIndex);
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
		}
	}

	private void SelectNext()
	{
		ClearHighlight();

		_currentIndex++;
		if (_currentIndex >= _enemies.Count)
			_currentIndex = 0;

		ApplyHighlight();
	}

	private void SelectIndex(int index)
	{
		if (_enemies.Count == 0) return;

		ClearHighlight();

		_currentIndex = Mathf.Clamp(index, 0, _enemies.Count - 1);
		ApplyHighlight();
	}

	private void ApplyHighlight()
	{
		var t = CurrentTarget;
		if (t != null && IsAlive(t))
		{
			t.SetSelected(true);
			GD.Print($"Alvo atual: {t.Name}");
		}
	}

	private void ClearHighlight()
	{
		var t = CurrentTarget;
		if (t != null && IsAlive(t))
			t.SetSelected(false);
	}

	private void RemoveInvalids()
	{
		for (int i = _enemies.Count - 1; i >= 0; i--)
		{
			if (!IsAlive(_enemies[i]))
				_enemies.RemoveAt(i);
		}
	}

	private bool IsAlive(Enemy e)
	{
		if (e == null) return false;
		if (!GodotObject.IsInstanceValid(e)) return false;
		if (e.IsQueuedForDeletion()) return false;
		return true;
	}
}
