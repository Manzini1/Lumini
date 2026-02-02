using Godot;
using System;

namespace Game.UI;

public partial class TurnBanner : Control
{
	[ExportGroup("Refs")]
	[Export] public NodePath BannerSpritePath = "BannerSprite";

	[ExportGroup("Textures")]
	[Export] public Texture2D AttackTexture;
	[Export] public Texture2D DefendTexture;

	[ExportGroup("Timing")]
	[Export] public float HoldSeconds = 0.55f;

	[ExportGroup("Motion")]
	[Export] public float Y = 90f;          // posição final no topo
	[Export] public float EnterY = -60f;    // vem de fora da tela
	[Export] public float ExitY = -60f;

	[Export] public float EnterTime = 0.18f;
	[Export] public float PopScale = 1.10f;
	[Export] public float PopTime = 0.10f;
	[Export] public float ExitTime = 0.18f;

	private Sprite2D _sprite;
	private Tween _tw;

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<Sprite2D>(BannerSpritePath);
		if (_sprite == null)
		{
			GD.PushWarning("[TurnBanner] BannerSprite não encontrado. Confira BannerSpritePath.");
			return;
		}

		Visible = false;
		_sprite.Modulate = new Color(1, 1, 1, 0);

		CenterSpriteX();
		GetViewport().SizeChanged += CenterSpriteX;
	}

	private void CenterSpriteX()
	{
		if (_sprite == null) return;
		var vp = GetViewportRect().Size;
		_sprite.GlobalPosition = new Vector2(vp.X * 0.5f, Y);
	}

	/// <param name="sideId">1 = Attack (player), outro = Defend (enemy)</param>
	public void ShowTurn(int sideId)
	{
		if (_sprite == null) return;

		bool isAttack = sideId == 1;
		_sprite.Texture = isAttack ? AttackTexture : DefendTexture;

		if (_tw != null && GodotObject.IsInstanceValid(_tw)) _tw.Kill();
		_tw = CreateTween();

		Visible = true;

		// estado inicial
		var vp = GetViewportRect().Size;
		_sprite.GlobalPosition = new Vector2(vp.X * 0.5f, EnterY);
		_sprite.Scale = Vector2.One;
		_sprite.Modulate = new Color(1, 1, 1, 0);

		// entra
		_tw.TweenProperty(_sprite, "modulate:a", 1.0f, EnterTime)
			.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

		_tw.TweenProperty(_sprite, "global_position:y", Y, EnterTime)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

		// pop
		_tw.TweenProperty(_sprite, "scale", Vector2.One * PopScale, PopTime)
			.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		_tw.TweenProperty(_sprite, "scale", Vector2.One, PopTime)
			.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);

		// segura
		_tw.TweenInterval(Mathf.Max(0.05f, HoldSeconds));

		// sai
		_tw.TweenProperty(_sprite, "modulate:a", 0.0f, ExitTime)
			.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);

		_tw.TweenProperty(_sprite, "global_position:y", ExitY, ExitTime)
			.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);

		_tw.TweenCallback(Callable.From(() => Visible = false));
	}
}
