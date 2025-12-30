using Godot;
using System;
using System.Collections.Generic;

public partial class ShieldVisual : Node2D
{
	[ExportCategory("Refs")]
	[Export] public NodePath ShieldControllerPath;   // ex: "../../ShieldController"
	[Export] public NodePath AnimPath = "Anim";

	[ExportCategory("Data")]
	[Export] public ShieldVisualBank Bank;

	private ShieldController _shield;
	private AnimatedSprite2D _anim;

	public override void _Ready()
	{
		_anim = GetNodeOrNull<AnimatedSprite2D>(AnimPath);
		if (_anim == null)
		{
			GD.PushError($"{Name}: ShieldVisual não encontrou AnimatedSprite2D em '{AnimPath}'.");
			return;
		}

		_shield = ResolveShieldController();
		if (_shield == null)
		{
			GD.PushError($"{Name}: ShieldVisual não encontrou ShieldController. Ajuste ShieldControllerPath.");
			return;
		}

		if (Bank == null)
			GD.PushWarning($"{Name}: ShieldVisual Bank não setado. Escudo não vai trocar animação.");

		// garante que começa escondido/visível como você preferir:
		_anim.Visible = true;

		// subscribe
		_shield.Changed += OnShieldChanged;

		// aplica o estado atual imediatamente
		OnShieldChanged(new List<ElementType>(_shield.Active));
	}

	public override void _ExitTree()
	{
		if (_shield != null)
			_shield.Changed -= OnShieldChanged;
	}

	private ShieldController ResolveShieldController()
	{
		// 1) via NodePath (recomendado)
		if (ShieldControllerPath != null && !ShieldControllerPath.IsEmpty)
		{
			var byPath = GetNodeOrNull<ShieldController>(ShieldControllerPath);
			if (byPath != null) return byPath;
		}

		// 2) fallback: procura subindo a árvore (robusto)
		Node n = this;
		for (int i = 0; i < 6 && n != null; i++)
		{
			var parent = n.GetParent();
			if (parent == null) break;

			var s = parent.GetNodeOrNull<ShieldController>("ShieldController");
			if (s != null) return s;

			n = parent;
		}
		return null;
	}

	private void OnShieldChanged(IReadOnlyList<ElementType> active)
	{
		string key = BuildKey(active);

		if (Bank == null)
		{
			GD.Print($"[SHIELD-VFX] key={key} (sem bank)");
			return;
		}

		// tenta key exata (fire_lightning)
		var entry = Bank.Get(key);

		// fallback: se não achar combo, tenta single do primeiro
		if (entry == null && active != null && active.Count > 0)
			entry = Bank.Get(ToId(active[0]));

		// fallback final
		if (entry == null)
		{
			GD.Print($"[SHIELD-VFX] Sem entry para key='{key}'.");
			return;
		}

		if (entry.Frames == null)
		{
			GD.Print($"[SHIELD-VFX] Entry '{entry.Key}' sem Frames.");
			return;
		}

		_anim.SpriteFrames = entry.Frames;

		// se não existir animação, cai no "default"
		string animName = string.IsNullOrWhiteSpace(entry.AnimationName) ? "default" : entry.AnimationName;
		if (!_anim.SpriteFrames.HasAnimation(animName))
		{
			animName = _anim.SpriteFrames.HasAnimation("default") ? "default" : _anim.SpriteFrames.GetAnimationNames()[0];
		}

		_anim.Animation = animName;
		_anim.Play();

		// debug opcional
		// GD.Print($"[SHIELD-VFX] key={key} -> frames={entry.Frames.ResourcePath} anim={animName}");
	}

	private static string BuildKey(IReadOnlyList<ElementType> active)
	{
		if (active == null || active.Count == 0) return "none";
		if (active.Count == 1) return ToId(active[0]);

		// pega até 2, ordena para key estável
		var a = active[0];
		var b = active[1];
		if ((int)a > (int)b) (a, b) = (b, a);

		return $"{ToId(a)}_{ToId(b)}";
	}

	private static string ToId(ElementType e)
	{
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
			_ => "unknown"
		};
	}
}
