using Godot;
using System;

public partial class TargetInstantSpellVfx : Node2D, IVfxPlayable, ISpellVfxConfigurable
{
	public event Action Impacted;

	private AnimatedSprite2D _sprite;
	private bool _hookedFinished;
	private bool _configured;

	private SpellVfxEntry _entry;
	private Node2D _caster;
	private Node2D _target;

	private bool _damageFired;

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (_sprite == null)
		{
			GD.PushError("[TargetInstantSpellVfx] AnimatedSprite2D não encontrado.");
			QueueFree();
			return;
		}

		// Se cair aqui em runtime sem Configure, é bug de pipeline
		if (!Engine.IsEditorHint() && !_configured)
			GD.PushWarning("[TargetInstantSpellVfx] Configure() não foi chamado (ruim em runtime).");

		// ✅ roda em deferred para:
		// - garantir que o node está na tree
		// - garantir que quem chamou já assinou o Impacted
		CallDeferred(nameof(BeginRuntime));
	}

	public void Configure(SpellVfxEntry entry, Node2D caster, Node2D target)
	{
		_configured = true;
		_entry = entry;
		_caster = caster;
		_target = target;

		// Não dá play aqui — damos em BeginRuntime (deferred) por segurança.
	}

	private void BeginRuntime()
	{
		if (_sprite == null || _entry == null)
			return;

		// ✅ aplica tuning do entry
		Scale = _entry.Scale;
		RotationDegrees = _entry.RotationDegrees;
		ZIndex = _entry.ZIndex;

		// ✅ injeta frames (se entry estiver usando cena genérica)
		if (_entry.Frames != null)
			_sprite.SpriteFrames = _entry.Frames;

		_sprite.SpeedScale = Mathf.Max(0.01f, _entry.SpeedScale);

		// ✅ toca animação correta
		string anim = string.IsNullOrWhiteSpace(_entry.AnimationName) ? "play" : _entry.AnimationName;

		if (_sprite.SpriteFrames == null)
		{
			GD.PushWarning("[TargetInstantSpellVfx] SpriteFrames null (sem animação).");
		}
		else if (_sprite.SpriteFrames.HasAnimation(anim))
		{
			_sprite.Play(anim);
		}
		else
		{
			// fallback seguro
			if (_sprite.SpriteFrames.HasAnimation("default"))
				_sprite.Play("default");
			else
				_sprite.Play();
		}

		if (!_hookedFinished)
		{
			_sprite.AnimationFinished += OnSpriteFinished;
			_hookedFinished = true;
		}

		// ✅ agenda dano (por tempo OU no fim)
		ScheduleDamage();

		// ✅ agenda impacto secundário (opcional)
		ScheduleSecondaryImpact();
	}

	private async void ScheduleDamage()
	{
		if (_entry == null) return;

		// -1 => no fim da animação
		if (_entry.DamageDelaySeconds < 0f)
			return;

		float delay = Mathf.Max(0f, _entry.DamageDelaySeconds);

		// garante 1 frame antes de disparar (evita “perder assinatura do evento”)
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		if (delay > 0f)
			await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);

		FireDamageOnce();
	}

	private void OnSpriteFinished()
	{
		// se pediu “dano no fim”
		if (_entry != null && _entry.DamageDelaySeconds < 0f)
			FireDamageOnce();

		if (_entry?.AutoFreeOnFinish ?? true)
			QueueFree();
	}

	private void FireDamageOnce()
	{
		if (_damageFired) return;
		_damageFired = true;

		Impacted?.Invoke();
	}

	private async void ScheduleSecondaryImpact()
	{
		if (_entry == null) return;
		if (!_entry.UseSecondaryImpact) return;
		if (_entry.SecondaryImpactScene == null) return;

		// 1 frame de segurança
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		float delay = Mathf.Max(0f, _entry.SecondaryImpactDelaySeconds);
		if (delay > 0f)
			await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);

		SpawnSecondaryImpact();
	}

	private void SpawnSecondaryImpact()
	{
		if (_entry == null || _entry.SecondaryImpactScene == null) return;

		// parent: VfxRoot (group vfx_root) se existir
		var roots = GetTree().GetNodesInGroup("vfx_root");
		var parent = (roots.Count > 0) ? roots[0] as Node : GetTree().CurrentScene;
		if (parent == null) return;

		var impact = _entry.SecondaryImpactScene.Instantiate<Node2D>();
		parent.AddChild(impact);

		// ✅ base: posição atual do instant (já spawnado no anchor)
		impact.GlobalPosition = GlobalPosition + _entry.SecondaryImpactOffset;
		impact.ZIndex = _entry.SecondaryImpactZIndex;
		impact.Scale = _entry.SecondaryImpactScale;

		// ✅ se for GenericSpellVfx, injeta frames do secondary
		//if (impact is GenericSpellVfx g && _entry.SecondaryImpactFrames != null)
		//{
			//var tmp = new SpellVfxEntry
			//{
				//Frames = _entry.SecondaryImpactFrames,
				//AnimationName = string.IsNullOrWhiteSpace(_entry.SecondaryImpactAnimName) ? "play" : _entry.SecondaryImpactAnimName,
				//SpeedScale = _entry.SecondaryImpactSpeedScale,
				//ZIndex = impact.ZIndex,
				//FallbackLifetime = 1.2f,
				//Scale = Vector2.One,
				//RotationDegrees = 0f,
				//AutoFreeOnFinish = true
			//};
//
			//g.Configure(tmp, _caster, _target);
		//}
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
