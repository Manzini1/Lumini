using Godot;
using System;

namespace Game.UI
{
	public partial class TurnOverlayController : ColorRect
	{
		public enum TurnSide
		{
			Defense = 0,
			Attack = 1
		}

		[ExportGroup("Colors")]
		[Export] public Color AttackColor = new Color(1f, 0.20f, 0.20f, 0.10f);   // vermelho com alpha
		[Export] public Color DefenseColor = new Color(0.25f, 0.45f, 1f, 0.10f);  // azul com alpha
		[Export] public Color NeutralColor = new Color(1f, 1f, 1f, 0f);

		[ExportGroup("Tween")]
		[Export] public float TweenSeconds = 0.18f;
		[Export] public float QuickSeconds = 0.10f;

		private Tween _tw;

		public override void _Ready()
		{
			// garante overlay não bloqueia input
			MouseFilter = MouseFilterEnum.Ignore;

			// começa neutro
			Color = NeutralColor;
		}

		public void SetTurn(int sideId, bool quick = false)
		{
			var target = sideId == (int)TurnSide.Attack ? AttackColor
						: sideId == (int)TurnSide.Defense ? DefenseColor
						: NeutralColor;

			Apply(target, quick ? QuickSeconds : TweenSeconds);
		}

		public void SetNeutral(bool quick = false)
		{
			Apply(NeutralColor, quick ? QuickSeconds : TweenSeconds);
		}

		private void Apply(Color target, float seconds)
		{
			if (_tw != null && GodotObject.IsInstanceValid(_tw))
				_tw.Kill();

			_tw = CreateTween();
			_tw.TweenProperty(this, "color", target, Mathf.Max(0.01f, seconds));
		}
	}
}
