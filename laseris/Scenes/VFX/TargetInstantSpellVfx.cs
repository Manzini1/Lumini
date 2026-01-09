using Godot;
using System;

public partial class TargetInstantSpellVfx : Node2D, IVfxPlayable, ISpellVfxConfigurable
{
	public event Action Impacted;

	[ExportCategory("Optional local tweak (only this scene)")]
	[Export] public Vector2 ExtraLocalOffset = Vector2.Zero;

	private AnimatedSprite2D _sprite;
	private bool _hookedFinished;
	private bool _configured;

	private bool _damageFired;

	private SpellVfxEntry _entry;
	private Node2D _caster;
	private Node2D _target;

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (_sprite == null)
		{
			GD.PushError("[TargetInstantSpellVfx] AnimatedSprite2D não encontrado.");
			QueueFree();
			return;
		}

		// Se cair aqui em runtime sem Configure, é bug de pipeline (você já viu esse warning)
		if (!Engine.IsEditorHint() && !_configured)
			GD.PushWarning("[TargetInstantSpellVfx] Configure() não foi chamado (ruim em runtime).");
	}

	public void Configure(SpellVfxEntry entry, Node2D caster, Node2D target)
	{
		_configured = true;
		_entry = entry;
		_caster = caster;
		_target = target;

		_damageFired = false;

		// ✅ aplica ajustes visuais configuráveis por entry
		if (_entry != null)
		{
			ZIndex = _entry.ZIndex;
			Scale = _entry.Scale;
			RotationDegrees = _entry.RotationDegrees;
		}

		// ✅ offset local extra (se você quiser empurrar só essa cena além do Offset do entry)
		Position += ExtraLocalOffset;

		// ✅ injeta frames se vierem do banco
		if (_sprite != null && _entry?.Frames != null)
		{
			_sprite.SpriteFrames = _entry.Frames;

			if (!string.IsNullOrWhiteSpace(_entry.AnimationName) && _sprite.SpriteFrames.HasAnimation(_entry.AnimationName))
				_sprite.Play(_entry.AnimationName);
			else if (_sprite.SpriteFrames.GetAnimationNames().Length > 0)
				_sprite.Play(_sprite.SpriteFrames.GetAnimationNames()[0]);
			else
				GD.PushWarning("[TargetInstantSpellVfx] SpriteFrames sem animações.");
		}
		else
		{
			// se a cena já tem SpriteFrames setado no editor
			_sprite?.Play();
		}

		// ✅ conecta finish só uma vez (pra auto-free)
		if (!_hookedFinished && _sprite != null)
		{
			_sprite.AnimationFinished += OnSpriteFinished;
			_hookedFinished = true;
		}

		// ✅ DANO POR TEMPO (manual)
		float delay = _entry?.DamageDelaySeconds ?? 0f;

		if (delay <= 0f)
		{
			// default: dá dano imediatamente (bom pra “frame 0”)
			FireDamageOnce();
		}
		else
		{
			// dá dano depois do delay configurado no entry
			_ = FireDamageAfterDelay(delay);
		}
	}

	private async System.Threading.Tasks.Task FireDamageAfterDelay(float delay)
	{
		await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);
		FireDamageOnce();
	}

	private void FireDamageOnce()
	{
		if (_damageFired) return;
		_damageFired = true;
		Impacted?.Invoke();
	}

	private void OnSpriteFinished()
	{
		// se você preferir dano no final, seria aqui.
		// do jeito que está: dano pode ser antes, e o finish só “some” o VFX.
		if (_entry?.AutoFreeOnFinish ?? true)
			QueueFree();
	}

	public override void _ExitTree()
	{
		if (_hookedFinished && _sprite != null && GodotObject.IsInstanceValid(_sprite))
		{
			_sprite.AnimationFinished -= OnSpriteFinished;
			_hookedFinished = false;
		}
	}
}
