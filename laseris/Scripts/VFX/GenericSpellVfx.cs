using Godot;
using System;

public partial class GenericSpellVfx : Node2D, IVfxPlayable
{
	public event Action Impacted;

	[ExportCategory("Refs")]
	[Export] public NodePath AnimatedSpritePath = "AnimatedSprite2D";

	private AnimatedSprite2D _sprite;

	// suporta Configure antes do Ready
	private SpellVfxEntry _pendingEntry;
	private Node2D _pendingCaster;
	private Node2D _pendingTarget;
	private bool _configured;

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<AnimatedSprite2D>(AnimatedSpritePath);

		if (_sprite == null)
		{
			GD.PushError("[GenericSpellVfx] AnimatedSprite2D não encontrado.");
			QueueFree();
			return;
		}

		if (_pendingEntry != null && !_configured)
		{
			ApplyEntry(_pendingEntry);
			_configured = true;
		}
	}

	public void Configure(SpellVfxEntry entry, Node2D caster, Node2D target)
	{
		_pendingEntry = entry;
		_pendingCaster = caster;
		_pendingTarget = target;

		if (IsInstanceValid(_sprite))
		{
			ApplyEntry(entry);
			_configured = true;
		}
	}

	private void ApplyEntry(SpellVfxEntry entry)
	{
		if (_sprite == null) return;

		if (entry?.Frames != null)
			_sprite.SpriteFrames = entry.Frames;

		if (entry != null)
			_sprite.SpeedScale = entry.SpeedScale;

		if (entry != null)
			ZIndex = entry.ZIndex;

		string anim = (!string.IsNullOrEmpty(entry?.AnimationName))
			? entry.AnimationName
			: "play";

		if (_sprite.SpriteFrames != null && _sprite.SpriteFrames.HasAnimation(anim))
			_sprite.Play(anim);
		else
			_sprite.Play();

		// 🔥 emite Impact no próximo frame (tempo de conectar listener)
		CallDeferred(nameof(EmitImpact));

		_sprite.AnimationFinished += OnAnimFinished;

		// fallback lifetime (caso não haja AnimationFinished)
		if (entry != null && entry.FallbackLifetime > 0f)
		{
			var timer = GetTree().CreateTimer(entry.FallbackLifetime);
			timer.Timeout += () =>
			{
				if (IsInstanceValid(this)) QueueFree();
			};
		}
	}

	private void EmitImpact()
	{
		Impacted?.Invoke();
	}

	private void OnAnimFinished()
	{
		if (IsInstanceValid(_sprite))
			_sprite.AnimationFinished -= OnAnimFinished;

		QueueFree();
	}
}
