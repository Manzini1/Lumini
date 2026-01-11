using Godot;
using System.Collections.Generic;

public partial class SelectionCircleManager : Node2D
{
	[ExportCategory("Refs")]
	[Export] public NodePath CircleSpritePath = "Circle";

	[ExportCategory("Visual")]
	[Export] public SpriteFrames Frames;               // ShieldCircles.tres (8 anims)
	[Export] public float SpeedScale = 1.0f;
	[Export] public bool VisibleWhenActive = true;
	[ExportCategory("Visual")]
	[Export] public float CircleScale = 1.0f;
	[ExportCategory("Fallback")]
	[Export] public string DefaultAnim = "fire";       // se vier vazio / null

	private AnimatedSprite2D _sprite;

	public override void _Ready()
	{
		GD.Print($"[SelectionCircleManager] _sprite={_sprite?.Name} path={_sprite?.GetPath()} parent={_sprite?.GetParent()?.GetPath()} owner={_sprite?.Owner?.GetPath()}");
		Scale = Vector2.One * CircleScale; // escala o Node2D inteiro

		_sprite = GetNodeOrNull<AnimatedSprite2D>(CircleSpritePath);
		if (_sprite == null)
		{
			GD.PushError("[SelectionCircleManager] AnimatedSprite2D não encontrado em CircleSpritePath.");
			return;
		}

		if (Frames != null)
			_sprite.SpriteFrames = Frames;

		_sprite.SpeedScale = SpeedScale;
		_sprite.Visible = false;
	}

	public void Hide()
	{
		GD.Print($"[SelectionCircleManager] Hide() hiding sprite path={_sprite?.GetPath()}");

		if (_sprite == null) return;
		_sprite.Stop();
		_sprite.Visible = false;
	}

	public void ShowForElements(IReadOnlyList<ElementType> active)
	{
		if (_sprite == null) return;

		// Se não tiver elementos -> esconde
		if (active == null || active.Count == 0)
		{
			Hide();
			return;
		}

		var anim = AnimFromElement(active[0]);
		PlayAnim(anim);
	}

	public void PlayAnim(string animName)
	{
		if (_sprite == null) return;

		if (_sprite.SpriteFrames == null)
		{
			GD.PushWarning("[SelectionCircleManager] SpriteFrames null (não tem animações).");
			return;
		}

		if (string.IsNullOrWhiteSpace(animName))
			animName = DefaultAnim;

		if (!_sprite.SpriteFrames.HasAnimation(animName))
		{
			GD.PushWarning($"[SelectionCircleManager] Não existe animação '{animName}'. Usando DefaultAnim='{DefaultAnim}'.");
			animName = DefaultAnim;
		}

		if (!_sprite.SpriteFrames.HasAnimation(animName))
		{
			GD.PushWarning($"[SelectionCircleManager] DefaultAnim '{DefaultAnim}' também não existe. Nada a tocar.");
			return;
		}

		_sprite.SpeedScale = SpeedScale;
		_sprite.Visible = VisibleWhenActive;
		_sprite.Play(animName);
	}

	private static string AnimFromElement(ElementType e)
	{
		// ✅ nomes das animações no seu SpriteFrames (ShieldCircles.tres)
		return e switch
		{
			ElementType.Fire => "fire",
			ElementType.Ice => "ice",
			ElementType.Lightning => "lightning",
			ElementType.Poison => "poison",
			ElementType.Earth => "earth",
			ElementType.Air => "air",
			ElementType.Light => "light",
			ElementType.Shadow => "shadow",
			_ => "fire"
		};
	}
}
