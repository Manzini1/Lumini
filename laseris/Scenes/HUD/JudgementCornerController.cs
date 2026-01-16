using Godot;

namespace Game.UI;

public partial class JudgementCornerController : TextureRect
{
	[ExportGroup("Textures")]
	[Export] public Texture2D PerfectTexture;
	[Export] public Texture2D GoodTexture;
	[Export] public Texture2D MissTexture;

	[ExportGroup("Anim")]
	[Export] public float PopScale = 1.10f;
	[Export] public float ShowSeconds = 0.10f;
	[Export] public float FadeSeconds = 0.22f;

	private Tween _tween;

	public void Show(JudgementGrade grade)
	{
		// troca textura
		Texture = grade switch
		{
			JudgementGrade.Perfect => PerfectTexture,
			JudgementGrade.Good => GoodTexture,
			_ => MissTexture
		};

		// substitui o último: mata tween anterior
		_tween?.Kill();
		_tween = null;

		Visible = true;
		Modulate = new Color(1, 1, 1, 1);

		// popzinho
		Scale = Vector2.One * 0.95f;

		_tween = CreateTween();
		_tween.TweenProperty(this, "scale", Vector2.One * PopScale, 0.06f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
		_tween.TweenProperty(this, "scale", Vector2.One, 0.08f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);

		// segura um tiquinho e some
		_tween.TweenInterval(ShowSeconds);
		_tween.TweenProperty(this, "modulate:a", 0f, FadeSeconds).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);

		_tween.Finished += () =>
		{
			Visible = false;
			Modulate = new Color(1, 1, 1, 1);
		};
	}
}
