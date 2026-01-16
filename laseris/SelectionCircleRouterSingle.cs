using Godot;
using System;
using System.Collections.Generic;

public partial class SelectionCircleRouterSingle : Node
{
	[ExportCategory("Refs")]
	[Export] public NodePath EnemyPath = "../Enemy"; // ajuste
	[Export] public NodePath ShieldControllerPath = "ShieldController"; // relativo ao Enemy
	[Export] public NodePath GroundMarkerPath = "GroundMarker"; // relativo ao Enemy

	[Export] public NodePath CircleRootPath = "../SelectionCircle"; // Node2D
	[Export] public NodePath CircleManagerPath = "../SelectionCircle"; // onde está SelectionCircleManager

	[ExportCategory("Config")]
	[Export] public Vector2 ExtraOffset = Vector2.Zero;
	[Export] public bool HideWhenNoShield = false;
	[Export] public bool DebugLog = false;

	private Enemy _enemy;
	private ShieldController _shield;
	private Marker2D _ground;
	private Node2D _circleRoot;
	private SelectionCircleManager _circleManager;

	public override void _Ready()
	{
		_enemy = GetNodeOrNull<Enemy>(EnemyPath);
		if (_enemy == null)
		{
			GD.PushError("[SelectionCircleRouterSingle] EnemyPath inválido.");
			return;
		}

		_shield = _enemy.GetNodeOrNull<ShieldController>(ShieldControllerPath);
		if (_shield == null)
		{
			GD.PushError("[SelectionCircleRouterSingle] ShieldControllerPath inválido (não achei no Enemy).");
			return;
		}

		_ground = _enemy.GetNodeOrNull<Marker2D>(GroundMarkerPath);
		if (_ground == null)
		{
			GD.PushWarning("[SelectionCircleRouterSingle] GroundMarker não encontrado. Vou usar Enemy.GlobalPosition + Y.");
		}

		_circleRoot = GetNodeOrNull<Node2D>(CircleRootPath);
		if (_circleRoot == null)
		{
			GD.PushError("[SelectionCircleRouterSingle] CircleRootPath inválido (precisa ser Node2D).");
			return;
		}

		_circleManager = GetNodeOrNull<SelectionCircleManager>(CircleManagerPath);
		if (_circleManager == null)
			GD.PushWarning("[SelectionCircleRouterSingle] CircleManagerPath não achou SelectionCircleManager.");

		_shield.Changed += OnShieldChanged;

		// aplicar estado inicial
		OnShieldChanged(new List<ElementType>(_shield.Active));

		// posiciona de cara
		UpdateCirclePosition();
	}

	public override void _ExitTree()
	{
		if (_shield != null) _shield.Changed -= OnShieldChanged;
	}

	public override void _Process(double delta)
	{
		if (_enemy == null || !GodotObject.IsInstanceValid(_enemy)) return;
		UpdateCirclePosition();
	}

	private void UpdateCirclePosition()
	{
		Vector2 pos;

		if (_ground != null && GodotObject.IsInstanceValid(_ground))
			pos = _ground.GlobalPosition;
		else
			pos = _enemy.GlobalPosition + new Vector2(0, 40);

		pos += ExtraOffset;
		_circleRoot.GlobalPosition = pos;
	}

	private void OnShieldChanged(IReadOnlyList<ElementType> active)
	{
		if (_circleManager == null) return;

		if (active == null || active.Count == 0)
		{
			if (HideWhenNoShield) _circleManager.HideCircle();
			return;
		}

		_circleManager.ShowCircle();

		// escolhe o primeiro do snapshot (ordem estável do evento)
		var element = active[0];
		_circleManager.SetElement(element);

		if (DebugLog)
			GD.Print($"[CircleRouter] ShieldChanged count={active.Count} elementShown={element}");
	}
}
