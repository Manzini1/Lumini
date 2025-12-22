using Godot;

public partial class Enemy : Node2D
{
	[Export] public EnemyData Data;

	// Se por algum motivo seu Sprite não se chama "Sprite",
	// você pode setar isso no Inspector.
	[Export] public NodePath SpritePath = new NodePath("Sprite");
	[Export] public NodePath VfxNodePath = new NodePath("Vfx");
	[Export] public NodePath LightningVfxPath = new NodePath("Vfx/LightningVfx");

	private Sprite2D _sprite;
	private HealthComponent _health;
	private ShieldComponent _shield;

	// VFX (voltamos para Node2D)
	private Node2D _vfxNode;
	private AnimatedSprite2D _lightningVfx;

	private Tween _hitTween;
	private bool _selected = false;

	public override void _Ready()
	{
		if (!IsInGroup("Enemies"))
			AddToGroup("Enemies");

		_sprite = GetNodeOrNull<Sprite2D>(SpritePath);
		_health = GetNodeOrNull<HealthComponent>("Health");
		_shield = GetNodeOrNull<ShieldComponent>("Shield");

		_vfxNode = GetNodeOrNull<Node2D>(VfxNodePath);
		_lightningVfx = GetNodeOrNull<AnimatedSprite2D>(LightningVfxPath);

		if (Data == null)
		{
			GD.PushWarning($"{Name}: EnemyData não atribuído. Usando defaults.");
			Data = new EnemyData();
		}

		if (_sprite == null)
			GD.PushError($"{Name}: Não encontrei Sprite2D em '{SpritePath}'. Corrija o nome do node ou o SpritePath no Inspector.");

		if (_health == null)
			GD.PushError($"{Name}: Não encontrei HealthComponent em 'Health'.");

		// ✅ aplica offset do VFX por tipo de monstro
		if (_vfxNode != null)
			_vfxNode.Position = Data.VfxOffset;

		// ✅ garante VFX escondido no start
		if (_lightningVfx != null)
		{
			_lightningVfx.Stop();
			_lightningVfx.Frame = 0;
			_lightningVfx.Visible = false;

			if (_lightningVfx.SpriteFrames != null && _lightningVfx.SpriteFrames.HasAnimation("play"))
				_lightningVfx.SpriteFrames.SetAnimationLoop("play", false);
		}
		else
		{
			GD.PushWarning($"{Name}: Não encontrei LightningVfx em '{LightningVfxPath}'. O raio não vai aparecer.");
		}

		if (_health != null)
		{
			_health.Init(Data.MaxHp);
			_health.Died += OnDied;
		}

		// SelfModulate é o que vamos usar pro flash vermelho (não conflita com highlight)
		if (_sprite != null)
			_sprite.SelfModulate = Colors.White;

		UpdateSelectedVisual();
		GD.Print($"{Name} spawned with HP {_health?.CurrentHp}/{_health?.MaxHp}");
	}

	private bool IsFlying() => Data != null && Data.IsFlying;

	private bool CanSpellHit(SpellTargeting targeting)
	{
		if (targeting == SpellTargeting.None) return false;

		bool flying = IsFlying();
		return flying
			? (targeting & SpellTargeting.Air) != 0
			: (targeting & SpellTargeting.Ground) != 0;
	}

	// ✅ flash vermelho 2x quando HIT acontece
	private void FlashHitRed()
	{
		if (_sprite == null) return;

		if (_hitTween != null && _hitTween.IsRunning())
			_hitTween.Kill();

		_sprite.SelfModulate = Colors.White;

		_hitTween = CreateTween();
		_hitTween.TweenProperty(_sprite, "self_modulate", new Color(1f, 0.2f, 0.2f, 1f), 0.05);
		_hitTween.TweenProperty(_sprite, "self_modulate", Colors.White, 0.05);
		_hitTween.TweenProperty(_sprite, "self_modulate", new Color(1f, 0.2f, 0.2f, 1f), 0.05);
		_hitTween.TweenProperty(_sprite, "self_modulate", Colors.White, 0.05);
	}

	// ✅ VFX do raio no alvo atual (este Enemy)
	private async void PlayLightningVfx()
	{
		if (_lightningVfx == null) return;

		_lightningVfx.Visible = true;
		_lightningVfx.Frame = 0;
		_lightningVfx.Play("play");

		// Se a animação estiver sem loop, esse sinal funciona.
		// Mesmo assim colocamos um fallback.
		var timer = GetTree().CreateTimer(0.35f);

		// Espera ou acabar a animação OU timeout (o que vier primeiro)
		// (Godot não tem Task.WhenAny aqui, então fazemos assim)
		bool finished = false;

		void OnFinished()
		{
			finished = true;
		}

		_lightningVfx.AnimationFinished += OnFinished;

		while (!finished && timer.TimeLeft > 0)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		_lightningVfx.AnimationFinished -= OnFinished;

		if (GodotObject.IsInstanceValid(_lightningVfx))
			_lightningVfx.Visible = false;
	}

	private void PlayVfx(ElementType element)
	{
		if (element == ElementType.Lightning)
			PlayLightningVfx();
	}

	private float GetMultiplier(ElementType element) =>
		Data != null ? Data.GetElementMultiplier(element) : 1f;

	public void TakeSpellHit(SpellDefinition spell)
	{
		if (!GodotObject.IsInstanceValid(this)) return;
		if (_health == null || !_health.IsAlive()) return;
		if (spell == null || spell.Damage <= 0) return;

		// HIT/MISS chão/ar
		if (!CanSpellHit(spell.Targeting))
		{
			string state = IsFlying() ? "NO AR" : "NO CHÃO";
			GD.Print($"[{Name}] MISS! '{spell.Name}' não acerta alvo {state} (Targeting={spell.Targeting}).");
			return;
		}

		// VFX primeiro (feedback instantâneo)
		PlayVfx(spell.PrimaryElement);

		// Escudo absorve?
		if (_shield != null)
		{
			var absorb = _shield.EvaluateAbsorb(spell.Elements);
			if (absorb != ShieldAbsorbResult.NoAbsorb)
			{
				float healFactor = (absorb == ShieldAbsorbResult.AbsorbHeal100) ? 1.0f : 0.5f;
				int healAmount = Mathf.RoundToInt(_health.MaxHp * _shield.HealPercentOfMaxHp * healFactor);
				healAmount = Mathf.Max(1, healAmount);

				_health.Heal(healAmount);
				GD.Print($"[{Name}] ABSORVIDO ({absorb}) -> curou {healAmount}. HP: {_health.CurrentHp}/{_health.MaxHp}");
				return;
			}
		}

		// Dano normal
		float mult = GetMultiplier(spell.PrimaryElement);
		int finalDamage = Mathf.RoundToInt(spell.Damage * mult);

		if (finalDamage > 0)
		{
			_health.ApplyDamage(finalDamage);
			FlashHitRed();

			GD.Print($"[{Name}] tomou {finalDamage} ({spell.PrimaryElement}, x{mult}). HP: {_health.CurrentHp}/{_health.MaxHp}");

			// Shield reage ao dano (se você estiver usando tipo 2/3)
			if (_shield != null)
				_shield.OnTookDamage(spell.Elements);
		}
	}

	public void SetSelected(bool selected)
	{
		_selected = selected;
		UpdateSelectedVisual();
	}

	private void UpdateSelectedVisual()
	{
		if (_sprite == null) return;

		// Highlight em Modulate
		_sprite.Modulate = _selected ? new Color(1.6f, 1.6f, 1.3f, 1f) : Colors.White;
		// Flash em SelfModulate (não conflita)
	}

	private void OnDied()
	{
		GD.Print($"{Name} morreu!");
		SetSelected(false);
		QueueFree();
	}
}
