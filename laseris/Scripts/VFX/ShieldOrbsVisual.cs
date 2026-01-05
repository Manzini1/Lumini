using Godot;
using System;
using System.Collections.Generic;

public partial class ShieldOrbsVisual : Node2D
{
	[ExportCategory("Refs")]
	[Export] public NodePath ShieldControllerPath = "../ShieldController";
	[Export] public NodePath AnchorPath = "../ShieldAnchor"; // Marker2D
	[Export] public PackedScene OrbScene;                    // ElementOrbVfx.tscn

	[ExportCategory("Data")]
	[Export] public ShieldOrbBank Bank; // ElementType -> Frames/AnimName/Speed

	[ExportCategory("Orbit (Fake 3D)")]
	[Export] public float RadiusX = 84f;     // raio horizontal
	[Export] public float RadiusY = 6f;     // raio vertical (menor = mais "3D fake")
	[Export] public float AngularSpeed = 2.8f; // rad/s

	[Export] public float ScaleBack = 0.75f;
	[Export] public float ScaleFront = 1.10f;
	[Export] public float AlphaBack = 0.35f;
	[Export] public float AlphaFront = 1.0f;

	[Export] public int ZRange = 30;         // quanto vai pra trás/frente
	[Export] public int ZOffset = 0;         // offset geral (se quiser tudo acima do inimigo)

	private ShieldController _shield;
	private Marker2D _anchor;

	private readonly List<ElementOrbVfx> _orbs = new();
	private float _t;

	private int _baseEnemyZ;

	public override void _Ready()
	{
		GD.Print($"[ShieldOrbsVisual] Ready on {GetParent()?.Name} OrbScene={(OrbScene!=null)} Bank={(Bank!=null)}");

		_shield = GetNodeOrNull<ShieldController>(ShieldControllerPath);
		_anchor = GetNodeOrNull<Marker2D>(AnchorPath);

		GD.Print($"[ShieldOrbsVisual] ShieldController resolved? {_shield!=null} ActiveCount={_shield?.Active?.Count}");

		if (_shield == null)
		{
			GD.PushError($"{Name}: ShieldOrbsVisual não achou ShieldController em '{ShieldControllerPath}'.");
			return;
		}
		if (_anchor == null)
		{
			GD.PushError($"{Name}: ShieldOrbsVisual não achou ShieldAnchor em '{AnchorPath}'.");
			return;
		}
		if (OrbScene == null)
		{
			GD.PushError($"{Name}: OrbScene não setado no Inspector.");
			return;
		}

		_baseEnemyZ = ResolveEnemyZIndex();
		// GD.Print($"[ShieldOrbsVisual] baseEnemyZ={_baseEnemyZ}");

		_shield.Changed += OnShieldChanged;

		// aplica agora
		OnShieldChanged(new List<ElementType>(_shield.Active));
	}

	public override void _ExitTree()
	{
		if (_shield != null) _shield.Changed -= OnShieldChanged;
	}

	public override void _Process(double delta)
	{
		if (_orbs.Count == 0) return;

		float dt = (float)delta;
		_t += dt;

		for (int i = 0; i < _orbs.Count; i++)
		{
			var orb = _orbs[i];
			if (orb == null || !GodotObject.IsInstanceValid(orb)) continue;

			float angle = _t * AngularSpeed + (Mathf.Tau * i / _orbs.Count);

			float cos = Mathf.Cos(angle);
			float sin = Mathf.Sin(angle);

			// órbita elíptica (fake 3D)
			orb.Position = new Vector2(cos * RadiusX, sin * RadiusY);

			// depth: sin=-1 atrás, sin=+1 frente
			float depth01 = (sin + 1f) * 0.5f;

			// escala e alpha
			float s = Mathf.Lerp(ScaleBack, ScaleFront, depth01);
			orb.Scale = new Vector2(s, s);

			float a = Mathf.Lerp(AlphaBack, AlphaFront, depth01);
			var m = orb.Modulate;
			m.A = a;
			orb.Modulate = m;

			// Z: atrás fica menor, frente maior
			int dz = Mathf.RoundToInt(Mathf.Lerp(-ZRange, +ZRange, depth01));
			orb.ZIndex = _baseEnemyZ + ZOffset + dz;
		}
	}

	private void OnShieldChanged(IReadOnlyList<ElementType> active)
	{
		GD.Print($"[ShieldOrbsVisual] OnShieldChanged count={active?.Count ?? 0}");

		// limpa orbes antigos
		for (int i = 0; i < _orbs.Count; i++)
			if (_orbs[i] != null && GodotObject.IsInstanceValid(_orbs[i]))
				_orbs[i].QueueFree();
		_orbs.Clear();

		if (active == null || active.Count == 0) return;

		for (int i = 0; i < active.Count; i++)
		{
			var element = active[i];

			var orb = OrbScene.Instantiate<ElementOrbVfx>();
			_anchor.AddChild(orb); // filho do anchor

			orb.Visible = true;
			orb.ZAsRelative = true; // garante que ZIndex funciona como esperado

			// configura visual
			if (Bank != null)
			{
				var entry = Bank.Get(element);
				if (entry != null)
					orb.Configure(element, entry.Frames, entry.AnimationName, entry.SpeedScale);
				else
					GD.PushWarning($"[ShieldOrbsVisual] Bank sem entry para {element}.");
			}
			else
			{
				GD.PushWarning("[ShieldOrbsVisual] Bank null — orbes vão existir mas sem animação.");
			}

			_orbs.Add(orb);
		}

		_t = 0f;
	}

	private int ResolveEnemyZIndex()
	{
		// tenta pegar Z do visual do inimigo, se existir
		var enemy = GetParent() as Node;
		if (enemy == null) return 0;

		// caso comum: node "Visual" (Node2D/Sprite2D)
		var visualNode2D = enemy.GetNodeOrNull<Node2D>("Visual");
		if (visualNode2D != null) return visualNode2D.ZIndex;

		var sprite = enemy.GetNodeOrNull<Sprite2D>("Visual/Sprite2D");
		if (sprite != null) return sprite.ZIndex;

		// fallback: pega o primeiro Sprite2D que achar
		var anySprite = FindFirstChildOfType<Sprite2D>(enemy);
		if (anySprite != null) return anySprite.ZIndex;

		return 0;
	}

	private static T FindFirstChildOfType<T>(Node root) where T : Node
	{
		foreach (var childObj in root.GetChildren())
		{
			if (childObj is Node child)
			{
				if (child is T typed) return typed;
				var deeper = FindFirstChildOfType<T>(child);
				if (deeper != null) return deeper;
			}
		}
		return null;
	}
}
