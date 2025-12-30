using Godot;
using System;

public partial class Mage : CharacterBody2D
{
	[ExportCategory("Refs")]
	[Export] public NodePath VisualPath;             // ex: "Visual" (Node2D)
	[Export] public NodePath AnimPath;               // ex: "Visual/Anim" (AnimatedSprite2D)
	[Export] public NodePath ElementControllerPath;  // ex: "../../HUD/ElementHUD/ElementController"
	[Export] public NodePath CastPointPath;          // ex: "CastPoint" (Marker2D) opcional

	[ExportCategory("Anim Names")]
	[Export] public string IdleAnim = "idle";
	[Export] public string CastingAnim = "casting";
	[Export] public string CastAnim = "cast";

	[ExportCategory("Facing")]
	[Export] public bool FaceRightIsDefault = true; // se sua arte “olha pra direita” por padrão
	[Export] public bool UseFlipH = true;            // true = FlipH no sprite; false = Scale.X negativo

	[ExportCategory("Feedback")]
	[Export] public float CastPulseScale = 1.08f;
	[Export] public float CastPulseTime = 0.08f;

	private Node2D _visual;
	private AnimatedSprite2D _anim;
	private ElementController _elementController;
	private Marker2D _castPoint;

	private bool _hasActiveRunes = false;
	private bool _playingCastOneShot = false;

	public Marker2D CastPoint => _castPoint;

	public override void _Ready()
	{
		_visual = GetNodeOrNull<Node2D>(VisualPath);
		_anim = GetNodeOrNull<AnimatedSprite2D>(AnimPath);
		_castPoint = GetNodeOrNull<Marker2D>(CastPointPath);

		if (_visual == null && !VisualPath.IsEmpty)
			GD.PushWarning("Mage: VisualPath setado mas não encontrei o node (Node2D).");

		if (_anim == null)
			GD.PushError("Mage: AnimPath inválido (não achei AnimatedSprite2D).");

		_elementController = GetNodeOrNull<ElementController>(ElementControllerPath);
		if (_elementController == null && !ElementControllerPath.IsEmpty)
			GD.PushWarning("Mage: ElementControllerPath setado mas não encontrei ElementController.");

		// estado inicial
		PlayIdle();

		// liga eventos do ElementController (runa ativa / limpa / cast)
		if (_elementController != null)
		{
			_elementController.ElementActivated += OnElementActivated;
			_elementController.ElementsCleared += OnElementsCleared;
			_elementController.CastStarted += OnCastStarted;
			_elementController.CastResolved += OnCastResolved;
		}

		if (_anim != null)
			_anim.AnimationFinished += OnAnimFinished;
	}

	public override void _ExitTree()
	{
		if (_elementController != null)
		{
			_elementController.ElementActivated -= OnElementActivated;
			_elementController.ElementsCleared -= OnElementsCleared;
			_elementController.CastStarted -= OnCastStarted;
			_elementController.CastResolved -= OnCastResolved;
		}

		if (_anim != null)
			_anim.AnimationFinished -= OnAnimFinished;
	}

	// ---------------- PUBLIC API (contrato pro CombatController) ----------------

	/// <summary>Vira a Mage para olhar para uma posição no mundo (ex: alvo).</summary>
	public void FaceWorldPosition(Vector2 worldPos)
	{
		// se worldPos.x é maior -> olha pra direita, senão esquerda
		bool wantRight = worldPos.X >= GlobalPosition.X;
		ApplyFacing(wantRight);
	}

	/// <summary>Feedback visual pós-cast (hit/absorbed). Não muda animação de estado.</summary>
	public void PlayCastFeedback()
	{
		if (_visual == null && _anim == null) return;

		// pulso rápido (tween) no Visual se existir, senão no próprio node
		var node = (Node2D)(_visual ?? this);

		var tw = CreateTween();
		tw.SetTrans(Tween.TransitionType.Sine);
		tw.SetEase(Tween.EaseType.Out);

		Vector2 baseScale = node.Scale;
		Vector2 up = baseScale * CastPulseScale;

		////tw.TweenProperty(node, "scale", up, CastPulseTime);
		//tw.TweenProperty(node, "scale", baseScale, CastPulseTime);
	}

	// ---------------- EVENT HANDLERS (animação por estado) ----------------

	private void OnElementActivated(ElementType _)
	{
		_hasActiveRunes = true;
		if (!_playingCastOneShot)
			PlayCasting();
	}

	private void OnElementsCleared()
	{
		_hasActiveRunes = false;
		if (!_playingCastOneShot)
			PlayIdle();
	}

	private void OnCastStarted()
	{
		// toca o cast 1 vez
		PlayCastOnce();
	}

	private void OnCastResolved(CastOutcome outcome, SpellDefinition spell, Enemy target)
	{
		// Aqui fica disponível para você no futuro fazer cast feedback diferente por outcome/spell.
	}

	private void OnAnimFinished()
	{
		if (_anim == null) return;
		if (_anim.Animation != CastAnim) return;

		_playingCastOneShot = false;

		// termina cast -> volta pro estado correto
		if (_hasActiveRunes) PlayCasting();
		else PlayIdle();
	}

	// ---------------- INTERNAL: animações ----------------

	private void PlayIdle()
	{
		if (_anim == null) return;
		_playingCastOneShot = false;

		if (!HasAnim(IdleAnim)) return;
		if (_anim.Animation != IdleAnim)
			_anim.Play(IdleAnim);
	}

	private void PlayCasting()
	{
		if (_anim == null) return;
		if (!HasAnim(CastingAnim)) return;

		if (_anim.Animation != CastingAnim)
			_anim.Play(CastingAnim);
	}

	private void PlayCastOnce()
	{
		if (_anim == null) return;
		if (!HasAnim(CastAnim)) return;

		_playingCastOneShot = true;
		_anim.Play(CastAnim);
	}

	private bool HasAnim(string name)
	{
		if (_anim?.SpriteFrames == null)
		{
			GD.PushWarning("Mage: AnimatedSprite2D sem SpriteFrames setado.");
			return false;
		}

		if (!_anim.SpriteFrames.HasAnimation(name))
		{
			GD.PushWarning($"Mage: animação '{name}' não existe no SpriteFrames.");
			return false;
		}

		return true;
	}

	// ---------------- INTERNAL: facing ----------------

	private void ApplyFacing(bool wantRight)
	{
		// se a arte é “right default”, então wantRight = true deixa normal
		// se a arte é “left default”, inverte
		bool facingRight = FaceRightIsDefault ? wantRight : !wantRight;

		if (_anim == null && _visual == null) return;

		if (UseFlipH && _anim != null)
		{
			// FlipH espelha (em Godot 4 AnimatedSprite2D tem FlipH)
			_anim.FlipH = !facingRight;
		}
		else
		{
			// fallback por escala no visual (ou no próprio node)
			var node = (Node2D)(_visual ?? this);
			var s = node.Scale;
			s.X = Mathf.Abs(s.X) * (facingRight ? 1f : -1f);
			node.Scale = s;
		}
	}
}
