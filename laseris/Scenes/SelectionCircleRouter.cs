using Godot;
using System.Collections.Generic;

public partial class SelectionCircleRouter : Node
{
	[ExportCategory("Refs")]
	[Export] public NodePath TargetControllerPath;

	[ExportCategory("Circle")]
	[Export] public PackedScene SelectionCircleScene; // SelectionCircle.tscn
	[Export] public Vector2 ExtraOffset = Vector2.Zero;
	[Export] public int ZBelowEnemy = -10;
	private readonly List<Marker2D> _circleMarkersSorted = new();
	[ExportCategory("Slot markers (scene-aligned)")]
	[Export] public Godot.Collections.Array<NodePath> SlotCircleMarkers = new(); // Marker2D paths

	private TargetController _targetController;

	private Node2D _circleInstance;
	private SelectionCircleManager _circle;

	private Enemy _currentTarget;
	private ShieldController _currentShield;

	public override void _Ready()
	{
		_targetController = GetNodeOrNull<TargetController>(TargetControllerPath);
		if (_targetController == null)
		{
			GD.PushError("[SelectionCircleRouter] TargetControllerPath inválido.");
			return;
		}

		if (SelectionCircleScene == null)
		{
			GD.PushError("[SelectionCircleRouter] SelectionCircleScene não setado.");
			return;
		}
		_circleMarkersSorted.Clear();
		for (int i = 0; i < SlotCircleMarkers.Count; i++)
		{
			var m = GetNodeOrNull<Marker2D>(SlotCircleMarkers[i]);
			if (m != null) _circleMarkersSorted.Add(m);
			else GD.PushWarning($"[SelectionCircleRouter] SlotCircleMarkers[{i}] inválido.");
		}

		_circleMarkersSorted.Sort((a, b) => a.GlobalPosition.X.CompareTo(b.GlobalPosition.X));

		_circleInstance = SelectionCircleScene.Instantiate<Node2D>();
		AddChild(_circleInstance);

		_circle = _circleInstance as SelectionCircleManager;
		if (_circle == null)
		{
			GD.PushError("[SelectionCircleRouter] SelectionCircle.tscn root precisa ter SelectionCircleManager.cs no root Node2D.");
			return;
		}

		_circle.Hide();

		_targetController.TargetChanged += OnTargetChanged;

		// se já existe target no start
		if (_targetController.CurrentTarget != null && GodotObject.IsInstanceValid(_targetController.CurrentTarget))
			OnTargetChanged(_targetController.CurrentTarget);
	}

	public override void _ExitTree()
	{
		if (_targetController != null)
			_targetController.TargetChanged -= OnTargetChanged;

		UnhookShield();
	}

	private void OnTargetChanged(Enemy newTarget)
	{
		if (_currentTarget == newTarget) return;

		_currentTarget = newTarget;

		UnhookShield();

		if (_currentTarget == null || !GodotObject.IsInstanceValid(_currentTarget))
		{
			_circle?.Hide();
			return;
		}

		// pega shield do enemy
		_currentShield = _currentTarget.Shield;
		if (_currentShield != null)
		{
			_currentShield.Changed += OnShieldChanged;
			OnShieldChanged(new List<ElementType>(_currentShield.Active));
		}
		else
		{
			// sem shield -> ainda mostra círculo default
			_circle?.PlayAnim(_circle.DefaultAnim);
		}

		MoveCircleToTargetSlot(_currentTarget);
	}

	private void OnShieldChanged(IReadOnlyList<ElementType> active)
	{
		_circle?.ShowForElements(active);

		// também reposiciona (se trocou target/slots e você quer garantir)
		if (_currentTarget != null && GodotObject.IsInstanceValid(_currentTarget))
			MoveCircleToTargetSlot(_currentTarget);
	}

	private void MoveCircleToTargetSlot(Enemy target)
	{
		if (_circleInstance == null) return;
		if (target == null || !GodotObject.IsInstanceValid(target)) return;

		Vector2 pos = ResolveSlotMarkerPos(target);
		_circleInstance.GlobalPosition = pos + ExtraOffset;

		// z: abaixo do inimigo
		_circleInstance.ZIndex = target.ZIndex + ZBelowEnemy;
		_circleInstance.Visible = true;
		GD.Print($"[Circle] target={target.Name} slot={target.SlotIndex} enemyX={target.GlobalPosition.X:0} markerX={pos.X:0}");

	}

	private Vector2 ResolveSlotMarkerPos(Enemy target)
{
	int slot = target.SlotIndex;

	if (slot >= 0 && slot < _circleMarkersSorted.Count)
		return _circleMarkersSorted[slot].GlobalPosition;

	// fallback: tenta marker no enemy
	var ground = target.GetNodeOrNull<Marker2D>("VfxGround");
	if (ground != null) return ground.GlobalPosition;

	return target.GlobalPosition;
}


	private void UnhookShield()
	{
		if (_currentShield != null)
		{
			_currentShield.Changed -= OnShieldChanged;
			_currentShield = null;
		}
	}
}
