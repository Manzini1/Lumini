using Godot;
using System;

namespace Game.UI
{
	public partial class TurnBanner : Control
	{
		[ExportGroup("Refs")]
		[Export] public NodePath LabelPath = "Label";
		[Export] public NodePath BackdropPath = "Backdrop"; // ColorRect opcional

		[ExportGroup("Layout")]
		[Export] public float TargetTopMargin = 24f; // onde o banner “para”
		[Export] public float StartOffscreenY = -90f;

		[ExportGroup("Timing")]
		[Export] public float InTime = 0.12f;
		[Export] public float HoldTime = 0.35f;
		[Export] public float OutTime = 0.14f;

		[ExportGroup("Punch")]
		[Export] public float InScaleFrom = 0.92f;
		[Export] public float InScaleTo = 1.06f;
		[Export] public float SettleScale = 1.00f;

		[ExportGroup("Colors")]
		[Export] public Color AttackColor = new Color(1f, 0.35f, 0.20f, 1f);
		[Export] public Color DefendColor = new Color(0.25f, 0.75f, 1f, 1f);

		private Label _label;
		private CanvasItem _backdrop;
		private Tween _tw;

		public override void _Ready()
		{
			_label = GetNodeOrNull<Label>(LabelPath);
			_backdrop = GetNodeOrNull<CanvasItem>(BackdropPath);

			Visible = false;
			Modulate = new Color(1, 1, 1, 0);
		}

		/// <summary>
		/// sideId: 1=Attack, 0=Defend (mesma convenção do seu ElementBar)
		/// </summary>
		public void ShowTurn(int sideId)
		{
			string text = (sideId == 1) ? "ATTACK!" : "DEFEND!";
			Color c = (sideId == 1) ? AttackColor : DefendColor;

			if (_label != null)
			{
				_label.Text = text;
				_label.Modulate = c;
			}

			if (_backdrop != null)
			{
				// fundo com alpha mais baixo
				var bc = c;
				bc.A = 0.22f;
				_backdrop.Modulate = bc;
			}

			// reset visual
			if (_tw != null && GodotObject.IsInstanceValid(_tw)) _tw.Kill();
			Visible = true;
			Modulate = new Color(1, 1, 1, 0);

			// posiciona no topo (centrado)
			var vpW = GetViewportRect().Size.X;
			var size = Size;
			Position = new Vector2((vpW - size.X) * 0.5f, StartOffscreenY);
			Scale = Vector2.One * InScaleFrom;

			_tw = CreateTween();

			// IN: slide + fade + punch
			_tw.TweenProperty(this, "position:y", TargetTopMargin, InTime)
				.SetTrans(Tween.TransitionType.Back)
				.SetEase(Tween.EaseType.Out);

			_tw.Parallel().TweenProperty(this, "modulate:a", 1.0f, InTime);

			_tw.Parallel().TweenProperty(this, "scale", Vector2.One * InScaleTo, InTime)
				.SetTrans(Tween.TransitionType.Back)
				.SetEase(Tween.EaseType.Out);

			// settle rápido pra 1.0
			_tw.TweenProperty(this, "scale", Vector2.One * SettleScale, 0.06f);

			// hold
			_tw.TweenInterval(Mathf.Max(0.01f, HoldTime));

			// OUT: sobe levinho e some
			_tw.TweenProperty(this, "position:y", TargetTopMargin - 14f, OutTime)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.In);

			_tw.Parallel().TweenProperty(this, "modulate:a", 0.0f, OutTime);

			_tw.TweenCallback(Callable.From(() =>
			{
				Visible = false;
			}));
		}
	}
}
