using Godot;
using System.Collections.Generic;

namespace Game.UI;

public partial class ElementBarController : Control
{
	[Signal] public delegate void ElementSelectedEventHandler(int elementId);

	public int SelectedElementId { get; private set; } = 1;

	private readonly List<AnimatedSprite2D> _elems = new();

	public override void _Ready()
	{
		// nomes fixos (como combinamos): Elem1..Elem4
		_elems.Clear();
		_elems.Add(GetNode<AnimatedSprite2D>("Elem1"));
		_elems.Add(GetNode<AnimatedSprite2D>("Elem2"));
		_elems.Add(GetNode<AnimatedSprite2D>("Elem3"));
		_elems.Add(GetNode<AnimatedSprite2D>("Elem4"));

		ApplyVisuals();
	}

	public override void _UnhandledInput(InputEvent e)
	{
		if (e.IsActionPressed("Fire")) Select(1);
		else if (e.IsActionPressed("Ice")) Select(2);
		else if (e.IsActionPressed("Earth")) Select(3);
		else if (e.IsActionPressed("Wind")) Select(4);
	}

	public void Select(int elementId)
	{
		elementId = Mathf.Clamp(elementId, 1, _elems.Count);
		if (SelectedElementId == elementId) return;

		SelectedElementId = elementId;
		ApplyVisuals();
		EmitSignal(SignalName.ElementSelected, SelectedElementId);
	}

	private void ApplyVisuals()
	{
		for (int i = 0; i < _elems.Count; i++)
		{
			int id = i + 1;
			var spr = _elems[i];

			if (id == SelectedElementId)
			{
				if (spr.SpriteFrames != null && spr.SpriteFrames.HasAnimation("activate"))
					spr.Play("activate");
				else if (spr.SpriteFrames != null && spr.SpriteFrames.HasAnimation("idle"))
					spr.Play("idle");

				spr.Scale = new Vector2(1.15f, 1.15f);
			}
			else
			{
				if (spr.SpriteFrames != null && spr.SpriteFrames.HasAnimation("idle"))
					spr.Play("idle");

				spr.Scale = Vector2.One;
			}
		}
	}
}
