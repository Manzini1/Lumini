using Godot;
using System;

namespace Game.Combat;

public partial class ProjectileVfx : Node2D
{
	[ExportGroup("Motion")]
	[Export] public bool RotateToDirection = true;
	[Export] public bool StretchToTravel = false;     // útil se for um "streak"
	[Export] public float StretchPixelsPerUnit = 1.0f; // ajuste se usar StretchToTravel

	[ExportGroup("Life")]
	[Export] public float EndDelay = 0.0f;            // delay antes de sumir depois de chegar
	[Export] public bool AutoFree = true;

	private Tween _tween;

	/// <summary>
	/// Chamado pelo ElementVfxLibrary via node.Call("Play", from, to, travelSec)
	/// </summary>
	public void Play(Vector2 from, Vector2 to, float travelSec = 0.08f)
	{
		GlobalPosition = from;

		var dir = (to - from);
		var dist = dir.Length();

		if (RotateToDirection && dist > 0.001f)
			Rotation = dir.Angle();

		if (StretchToTravel)
		{
			// Ex: se seu sprite estiver “apontando pra direita”, você pode usar Scale.X como comprimento.
			// Ajuste conforme o seu asset.
			var s = Scale;
			s.X = Mathf.Max(0.001f, dist * StretchPixelsPerUnit);
			Scale = s;
		}

		_tween?.Kill();
		_tween = CreateTween();
		_tween.SetTrans(Tween.TransitionType.Quad);
		_tween.SetEase(Tween.EaseType.Out);

		// vai pro alvo rapidão
		_tween.TweenProperty(this, "global_position", to, Mathf.Max(0.01f, travelSec));

		// ao terminar, opcionalmente some
		_tween.TweenCallback(Callable.From(() =>
		{
			if (EndDelay > 0f)
			{
				var t2 = CreateTween();
				t2.TweenInterval(EndDelay);
				t2.TweenCallback(Callable.From(() =>
				{
					if (AutoFree) QueueFree();
				}));
			}
			else
			{
				if (AutoFree) QueueFree();
			}
		}));
	}
}
