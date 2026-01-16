using Godot;
using System;

public partial class SelectionCircleManager : Node2D
{
	[ExportCategory("Refs")]
	[Export] public NodePath CircleSpritePath = "CircleSprite"; // AnimatedSprite2D

	[ExportCategory("Data")]
	[Export] public SelectionCircleBank Bank;
	[Export] public bool DebugLog = false;

	private AnimatedSprite2D _sprite;
	private ElementType _current;

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<AnimatedSprite2D>(CircleSpritePath);
		if (_sprite == null)
		{
			GD.PushError("[SelectionCircleManager] AnimatedSprite2D não encontrado em CircleSpritePath.");
			return;
		}

		// começa escondido
		HideCircle();
	}

	public void HideCircle()
	{
		if (_sprite != null) _sprite.Visible = false;
		Visible = false;
	}

	public void ShowCircle()
	{
		if (_sprite != null) _sprite.Visible = true;
		Visible = true;
	}

	public void SetElement(ElementType element)
	{
		_current = element;

		if (_sprite == null) return;
		if (Bank == null)
		{
			GD.PushWarning("[SelectionCircleManager] Bank = null (sem dados de spriteframes).");
			return;
		}

		var entry = Bank.Get(element);
		if (entry == null || entry.Frames == null)
		{
			GD.PushWarning($"[SelectionCircleManager] Bank sem entry/frames para {element}.");
			return;
		}

		_sprite.SpriteFrames = entry.Frames;

		string anim = string.IsNullOrWhiteSpace(entry.AnimationName) ? "loop" : entry.AnimationName;
		if (!_sprite.SpriteFrames.HasAnimation(anim))
		{
			// fallback: tenta qualquer animação existente
			anim = _sprite.SpriteFrames.GetAnimationNames().Length > 0
				? _sprite.SpriteFrames.GetAnimationNames()[0]
				: anim;
		}

		_sprite.SpeedScale = Mathf.Max(0.01f, entry.SpeedScale);
		_sprite.Play(anim);

		if (DebugLog)
			GD.Print($"[Circle] SetElement {element} anim={anim} speed={_sprite.SpeedScale:0.00}");
	}
}
