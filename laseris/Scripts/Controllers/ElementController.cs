using Godot;
using System;
using System.Collections.Generic;

public partial class ElementController : Node
{
	[Export] public NodePath TugManagerPath;                // opcional: setar direto na cena
[Export] public string TugManagerGroup = "tug_manager"; // alternativa por group


[ExportCategory("Tug Tuning (points)")]
[Export] public float TugOnSpellHit = +1f;
[Export] public float TugOnSpellMiss = -1f;
[Export] public float TugOnShieldAbsorb = -2f;   // punição maior
[Export] public float TugOnInterrupted = -3f;    // punição maior

	[ExportCategory("Refs")]
	[Export] public NodePath TargetControllerPath;
	[Export] public NodePath SfxPlayerPath;
	[Export] public NodePath VfxPlayerPath;

	[ExportCategory("Mage")]
	[Export] public string MageGroupName = "mage";

	[ExportCategory("Config")]
	[Export] public int MaxElements = 2;

	[ExportCategory("Dual Cast (2 elementos)")]
	[Export] public float DualCastTimeSeconds = 2.0f;

	[ExportCategory("Pressure")]
	[Export] public float PressureOnInterrupted = 0;   // apanhar conjurando
	[Export] public float PressureOnShieldAbsorb = 0;  // shield absorveu (Absorbed50/100)

	// -------- UI / Events --------
	public event Action<ElementType> ElementActivated;
	public event Action ElementsCleared;
	private TugManager _tug;
	/// <summary>Momento em que o cast foi "liberado" (release). Bom pra tocar animação one-shot.</summary>
	public event Action CastStarted;

	public event Action<CastOutcome, SpellDefinition, Enemy> CastResolved;

	/// <summary>Channeling começou (duration).</summary>
	public event Action<float> DualChannelStarted;

	/// <summary>Channeling progresso (elapsed, duration).</summary>
	public event Action<float, float> DualChannelProgress;

	/// <summary>Channeling cancelado (hit / stun / input off).</summary>
	public event Action DualChannelCancelled;

	/// <summary>Channeling terminou e soltou o cast.</summary>
	public event Action DualChannelReleased;

	private TargetController _targetController;
	private SfxPlayer _sfxPlayer;
	private VfxPlayer _vfxPlayer;
	private Mage _mage;

	private bool _inputEnabled = true;

	// -------- channel state --------
	private bool _channeling = false;
	private float _channelElapsed = 0f;
	private float _channelDuration = 0f;

	private Enemy _channelTarget;
	private SpellDefinition _channelSpell;
	private List<ElementType> _channelElements;

	private readonly List<ElementIcon> _activeElements = new();

	public override void _Ready()
	{
		_targetController = GetNodeOrNull<TargetController>(TargetControllerPath);
		_sfxPlayer = GetNodeOrNull<SfxPlayer>(SfxPlayerPath);
		_vfxPlayer = GetNodeOrNull<VfxPlayer>(VfxPlayerPath);

		_mage = GetTree().GetFirstNodeInGroup(MageGroupName) as Mage;
		
		_tug = !TugManagerPath.IsEmpty
	? GetNodeOrNull<TugManager>(TugManagerPath)
	: GetTree().GetFirstNodeInGroup(TugManagerGroup) as TugManager;

		if (_tug == null)
			GD.PushWarning("[ElementController] TugManager não encontrado (set TugManagerPath ou coloque no group 'tug_manager').");
		if (_mage != null)
			_mage.TookHit += OnMageTookHit;

		if (_targetController == null) GD.PushWarning("ElementController: TargetControllerPath inválido.");
		if (_sfxPlayer == null) GD.PushWarning("ElementController: SfxPlayerPath inválido.");
		if (_vfxPlayer == null) GD.PushWarning("ElementController: VfxPlayerPath inválido.");
		if (_mage == null) GD.PushWarning("ElementController: não achei Mage no group 'mage'.");
	}

	public override void _ExitTree()
	{
		if (_mage != null)
			_mage.TookHit -= OnMageTookHit;
	}

	public override void _Process(double delta)
	{
		if (!_channeling) return;

		// travas
		if (!_inputEnabled || (_mage != null && _mage.IsStunned))
		{
			CancelChannel();
			return;
		}

		_channelElapsed += (float)delta;
		DualChannelProgress?.Invoke(_channelElapsed, _channelDuration);

		if (_channelElapsed < _channelDuration)
			return;

		// terminou -> solta o cast
		_channeling = false;
		DualChannelProgress?.Invoke(_channelDuration, _channelDuration);
		DualChannelReleased?.Invoke();

		ReleaseSpell(_channelSpell, _channelTarget, _channelElements);

		// limpa
		_channelSpell = null;
		_channelTarget = null;
		_channelElements = null;
	}

	private DamagePopupManager GetDamagePopupManager()
	{
		return GetTree().GetFirstNodeInGroup("damage_popup_manager") as DamagePopupManager;
	}

	public void SetInputEnabled(bool enabled)
	{
		_inputEnabled = enabled;

		if (!enabled)
		{
			CancelChannel();
			ResetActiveElements();
		}
	}

	public bool CanActivate()
	{
		if (!_inputEnabled) return false;
		if (_channeling) return false;
		if (_mage != null && _mage.IsStunned) return false;
		return _activeElements.Count < MaxElements;
	}

	public void ActivateElement(ElementIcon element)
	{
		if (!CanActivate()) return;
		if (element == null) return;
		if (_activeElements.Contains(element)) return;

		_activeElements.Add(element);
		element.SetActive(true);

		GD.Print($"[ElementController] Ativado: {element.Name} ({element.ElementType})");
		ElementActivated?.Invoke(element.ElementType);
	}

	public void Cast()
	{
		// já canalizando? ignora
		if (_channeling) return;

		if (_mage != null && _mage.IsStunned)
		{
			EmitResolved(CastOutcome.CancelledInputDisabled, null, null);
			return;
		}

		if (!_inputEnabled)
		{
			EmitResolved(CastOutcome.CancelledInputDisabled, null, null);
			return;
		}

		if (_activeElements.Count == 0)
		{
			EmitResolved(CastOutcome.CancelledNoElements, null, null);
			return;
		}

		var target = _targetController?.CurrentTarget;
		if (target == null || !GodotObject.IsInstanceValid(target))
		{
			EmitResolved(CastOutcome.CancelledNoTarget, null, null);
			ResetActiveElements();
			return;
		}

		var castElements = new List<ElementType>();
		foreach (var icon in _activeElements)
			castElements.Add(icon.ElementType);

		var spell = SpellResolver.Resolve(castElements);

		bool isDual = castElements.Count >= 2;

		if (!isDual)
		{
			// -------- SINGLE (instant) --------
			CastStarted?.Invoke();
			_sfxPlayer?.PlaySpell(spell);

			var vfx = _vfxPlayer?.PlaySpell(spell);

			// limpa seleção agora
			ResetActiveElements();

			ResolveSpellDamageWithVfxOrInstant(target, spell, vfx);
			return;
		}

		// -------- DUAL (channel) --------
		StartDualChannel(target, spell, castElements);
	}

	private void StartDualChannel(Enemy target, SpellDefinition spell, List<ElementType> elements)
	{
		_channeling = true;
		_channelElapsed = 0f;
		_channelDuration = Mathf.Max(0.05f, DualCastTimeSeconds);

		_channelTarget = target;
		_channelSpell = spell;
		_channelElements = elements;

		GD.Print($"[ElementController] DualCast channeling {_channelDuration:0.00}s (spell={spell?.Id})");
		DualChannelStarted?.Invoke(_channelDuration);
		DualChannelProgress?.Invoke(0f, _channelDuration);
	}

	private void CancelChannel()
	{
		if (!_channeling) return;

		_channeling = false;
		DualChannelCancelled?.Invoke();

		_channelSpell = null;
		_channelTarget = null;
		_channelElements = null;
		_channelElapsed = 0f;
		_channelDuration = 0f;
	}

	private void ReleaseSpell(SpellDefinition spell, Enemy target, List<ElementType> castElements)
	{
		if (spell == null)
		{
			EmitResolved(CastOutcome.CancelledNoElements, null, target);
			return;
		}

		if (target == null || !GodotObject.IsInstanceValid(target))
		{
			EmitResolved(CastOutcome.CancelledNoTarget, spell, null);
			ResetActiveElements();
			return;
		}

		// momento do release -> animação cast one-shot
		CastStarted?.Invoke();

		_sfxPlayer?.PlaySpell(spell);
		var vfx = _vfxPlayer?.PlaySpell(spell);

		// limpa runas APÓS soltar
		ResetActiveElements();

		ResolveSpellDamageWithVfxOrInstant(target, spell, vfx);
	}

	private void ResolveSpellDamageWithVfxOrInstant(Enemy target, SpellDefinition spell, IVfxPlayable vfx)
	{
		if (vfx != null)
		{
			bool applied = false;
			Enemy castTarget = target;
			SpellDefinition castSpell = spell;

			vfx.Impacted += () =>
			{
				if (applied) return;
				applied = true;

				if (castTarget == null || !GodotObject.IsInstanceValid(castTarget))
				{
					EmitResolved(CastOutcome.CancelledNoTarget, castSpell, null);
					return;
				}

				var outcome = castTarget.TakeSpellHit(castSpell);
				if (_tug != null)
				{
					switch (outcome)
					{
						case CastOutcome.Hit:
							_tug.Push(TugOnSpellHit, $"hit {castSpell.Id}");
							break;

						case CastOutcome.Miss:
							_tug.Push(TugOnSpellMiss, $"miss {castSpell.Id}");
							break;

						case CastOutcome.Absorbed50:
						case CastOutcome.Absorbed100:
							_tug.Push(TugOnShieldAbsorb, $"absorbed {castSpell.Id}");
							break;

						// Blocked / Cancelled etc: você decide se quer empurrar ou ignorar
					}
				}	
				// Pressure: shield absorveu
				if (_mage != null && (outcome == CastOutcome.Absorbed50 || outcome == CastOutcome.Absorbed100))
					//_mage.AddPressure(PressureOnShieldAbsorb, "shield absorbed spell");

				GetDamagePopupManager()?.ShowFromOutcome(castTarget, castSpell, outcome);
				EmitResolved(outcome, castSpell, castTarget);
			};

			GD.Print("[ElementController] Cast aguardando Impacted do VFX para aplicar dano.");
			return;
		}

		// instantâneo (sem timing de impacto)
		var instantOutcome = target.TakeSpellHit(spell);
		if (_tug != null)
			{
				switch (instantOutcome)
				{
					case CastOutcome.Hit:
						_tug.Push(TugOnSpellHit, $"hit {spell.Id}");
						break;

					case CastOutcome.Miss:
						_tug.Push(TugOnSpellMiss, $"miss {spell.Id}");
						break;

					case CastOutcome.Absorbed50:
					case CastOutcome.Absorbed100:
						_tug.Push(TugOnShieldAbsorb, $"absorbed {spell.Id}");
						break;

					// Blocked / Cancelled etc: você decide se quer empurrar ou ignorar
				}
			}
		if (_mage != null && (instantOutcome == CastOutcome.Absorbed50 || instantOutcome == CastOutcome.Absorbed100))
			//_mage.AddPressure(PressureOnShieldAbsorb, "shield absorbed spell");

		EmitResolved(instantOutcome, spell, target);
	}

	private void OnMageTookHit(int damage)
	{
		// só importa se estiver canalizando
		if (!_channeling) return;

		GD.Print($"[ElementController] INTERRUPTED by hit ({damage}). Dual cast fails -> pressure++");

		CancelChannel();
		_tug?.Push(TugOnInterrupted, "hit while channeling");
		if (_mage != null)
			//_mage.AddPressure(PressureOnInterrupted, "hit while channeling");

		// punição: perde runas
		ResetActiveElements();

		EmitResolved(CastOutcome.CancelledInputDisabled, null, null);
	}

	private void ResetActiveElements()
	{
		foreach (var element in _activeElements)
			element.ResetElement();
		_activeElements.Clear();

		ElementsCleared?.Invoke();
	}

	private void EmitResolved(CastOutcome outcome, SpellDefinition spell, Enemy target)
	{
		CastResolved?.Invoke(outcome, spell, target);
	}
}
