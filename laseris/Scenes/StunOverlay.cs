using Godot;
using System;

public partial class StunOverlay : Control
{
	[ExportCategory("Refs")]
	[Export] public NodePath VignettePath = "Vignette";
	[Export] public NodePath FlashPath = "Flash";

	[ExportCategory("Tuning")]
	[Export] public float FlashIn = 0.05f;
	[Export] public float FlashOut = 0.14f;
	[Export] public float FlashAlpha = 0.75f;

	[Export] public float VignetteIn = 0.12f;
	[Export] public float VignetteOut = 0.18f;
	[Export] public float VignetteHoldAlpha = 0.30f;

	private ColorRect _vignette;
	private ColorRect _flash;

	public override void _Ready()
	{
		_vignette = GetNodeOrNull<ColorRect>(VignettePath);
		_flash = GetNodeOrNull<ColorRect>(FlashPath);

		if (_vignette == null) GD.PushError("[StunOverlay] VignettePath inválido.");
		if (_flash == null) GD.PushError("[StunOverlay] FlashPath inválido.");

		SetFlash(0f);
		SetVignette(0f);
	}

	public void PulseStart()
	{
		// Flash
		SetFlash(0f);
		var t1 = CreateTween();
		t1.TweenCallback(Callable.From(() => SetFlash(FlashAlpha)));
		t1.TweenInterval(FlashIn);
		t1.TweenCallback(Callable.From(() => SetFlash(0f)));
		t1.TweenInterval(FlashOut);

		// Vignette: sobe e fica
		SetVignette(0f);
		var t2 = CreateTween();
		t2.TweenCallback(Callable.From(() => SetVignette(VignetteHoldAlpha)));
		t2.TweenInterval(VignetteIn);
	}

	public void PulseEnd()
	{
		// Vignette: some suave
		var t = CreateTween();
		t.TweenCallback(Callable.From(() => SetVignette(0f)));
		t.TweenInterval(VignetteOut);

		// Flash pequeno no final opcional
		var t2 = CreateTween();
		t2.TweenCallback(Callable.From(() => SetFlash(0.25f)));
		t2.TweenInterval(0.03f);
		t2.TweenCallback(Callable.From(() => SetFlash(0f)));
		t2.TweenInterval(0.10f);
	}

	private void SetFlash(float a)
	{
		if (_flash == null) return;
		var c = _flash.Color;
		_flash.Color = new Color(c.R, c.G, c.B, Mathf.Clamp(a, 0f, 1f));
	}

	private void SetVignette(float a)
	{
		if (_vignette == null) return;
		var c = _vignette.Color;
		_vignette.Color = new Color(c.R, c.G, c.B, Mathf.Clamp(a, 0f, 1f));
	}
}
