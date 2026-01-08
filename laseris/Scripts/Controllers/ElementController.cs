using Godot;
using System;
using System.Collections.Generic;

public partial class ElementController : Node
{
	[ExportCategory("Refs")]
	[Export] public NodePath TargetControllerPath;
	[Export] public NodePath SfxPlayerPath;
	[Export] public NodePath VfxPlayerPath;

	public event Action<ElementType> ElementActivated;
	public event Action ElementsCleared;
	public event Action CastStarted;
	public event Action<CastOutcome, SpellDefinition, Enemy> CastResolved;

	[ExportCategory("Config")]
	[Export] public int MaxElements = 2;

	private TargetController _targetController;
	private SfxPlayer _sfxPlayer;
	private VfxPlayer _vfxPlayer;

	// ✅ para bloquear input enquanto arma está voando
	private Mage _mage;

	private bool _inputEnabled = true;
	private readonly List<ElementIcon> _activeElements = new();

	public override void _Ready()
	{
		_targetController = GetNodeOrNull<TargetController>(TargetControllerPath);
		_sfxPlayer = GetNodeOrNull<SfxPlayer>(SfxPlayerPath);
		_vfxPlayer = GetNodeOrNull<VfxPlayer>(VfxPlayerPath);

		if (_targetController == null) GD.PushWarning("ElementController: TargetControllerPath inválido.");
		if (_sfxPlayer == null) GD.PushWarning("ElementController: SfxPlayerPath inválido.");
		if (_vfxPlayer == null) GD.PushWarning("ElementController: VfxPlayerPath inválido.");

		// ✅ pega Mage via VfxPlayer e trava input quando arma está voando
		_mage = _vfxPlayer?.Mage;
		if (_mage != null)
		{
			_mage.WeaponFlightChanged += OnWeaponFlightChanged;

			// aplica estado inicial (se já estiver voando por algum motivo)
			OnWeaponFlightChanged(_mage.WeaponInFlight);
		}
	}

	public override void _ExitTree()
	{
		if (_mage != null)
			_mage.WeaponFlightChanged -= OnWeaponFlightChanged;
	}

	private void OnWeaponFlightChanged(bool inFlight)
	{
		SetInputEnabled(!inFlight);
		GD.Print($"[ElementController] Input {(inFlight ? "DESLIGADO" : "LIGADO")} (weapon in flight={inFlight})");
	}

	private DamagePopupManager GetDamagePopupManager()
	{
		return GetTree().GetFirstNodeInGroup("damage_popup_manager") as DamagePopupManager;
	}

	public void SetInputEnabled(bool enabled)
	{
		_inputEnabled = enabled;
		if (!enabled) ResetActiveElements();
	}

	public bool CanActivate() => _inputEnabled && _activeElements.Count < MaxElements;

	public void ActivateElement(ElementIcon element)
	{
		if (!_inputEnabled) return;
		if (element == null) return;
		if (_activeElements.Contains(element)) return;
		if (_activeElements.Count >= MaxElements) return;

		_activeElements.Add(element);
		element.SetActive(true);

		GD.Print($"[ElementController] Ativado: {element.Name} ({element.ElementType})");
		ElementActivated?.Invoke(element.ElementType);
	}

	public void Cast()
	{
		// ✅ trava: não pode castar enquanto arma está voando
		if (_mage != null && _mage.WeaponInFlight)
		{
			GD.Print("[ElementController] Cast BLOQUEADO: arma ainda não voltou.");
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

		// ✅ agora sim: o cast realmente começou (aqui dispara animação do mage)
		CastStarted?.Invoke();

		var castElements = new List<ElementType>();
		foreach (var icon in _activeElements)
			castElements.Add(icon.ElementType);

		var spell = SpellResolver.Resolve(castElements);

		_sfxPlayer?.PlaySpell(spell);

		var vfx = _vfxPlayer?.PlaySpell(spell);

		ResetActiveElements();

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
				var dmgMgr = GetDamagePopupManager();
				dmgMgr?.ShowFromOutcome(castTarget, castSpell, outcome);
				EmitResolved(outcome, castSpell, castTarget);
			};

			GD.Print("[ElementController] Cast aguardando Impacted do VFX para aplicar dano.");
			return;
		}

		// instantâneo
		var instantOutcome = target.TakeSpellHit(spell);
		GetDamagePopupManager()?.ShowFromOutcome(target, spell, instantOutcome);
		EmitResolved(instantOutcome, spell, target);
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
