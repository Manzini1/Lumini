using Godot;
using System;
using System.Collections.Generic;

namespace Game.Vfx
{
	public partial class LightningBarrageController : Node2D
	{
		[ExportGroup("Scenes")]
		[Export] public PackedScene StrikeScene; // LightningStrikeVfx.tscn

		[ExportGroup("Counts/Timing")]
		[Export] public int Strikes = 16;
		[Export] public float DurationSec = 0.80f;
		[Export] public float FirstDelaySec = 0.10f;

		[ExportGroup("Spread")]
		[Export] public float SpreadX = 140f;     // espalha na horizontal ao redor do alvo
		[Export] public float TargetRadius = 40f; // jitter no alvo
		[Export] public float SpawnMarginTop = 30f;

		[ExportGroup("Debug")]
		[Export] public bool DebugLogs = false;

		private readonly RandomNumberGenerator _rng = new();

		public override void _Ready()
		{
			_rng.Randomize();
		}

		public void Play(Vector2 targetGlobal)
		{
			if (StrikeScene == null) return;

			int n = Mathf.Max(1, Strikes);
			float dur = Mathf.Max(0.01f, DurationSec);
			float every = dur / n;

			float topY = GetTopOfScreenWorldY() - Mathf.Max(0f, SpawnMarginTop);

			for (int i = 0; i < n; i++)
			{
				float delay = Mathf.Max(0f, FirstDelaySec + i * every);

				GetTree().CreateTimer(delay).Timeout += () =>
				{
					if (!GodotObject.IsInstanceValid(this)) return;

					float x = targetGlobal.X + _rng.RandfRange(-SpreadX, SpreadX);
					Vector2 from = new Vector2(x, topY);

					Vector2 to = targetGlobal + RandomInCircle(TargetRadius);

					var raw = StrikeScene.Instantiate();
					if (raw is not LightningStrikeVfx strike)
					{
						raw.QueueFree();
						return;
					}

					AddChild(strike);
					strike.Play(from, to);

					if (DebugLogs)
						GD.Print($"[LightningBarrage] {from} -> {to}");
				};
			}

			// autocleanup
			GetTree().CreateTimer(FirstDelaySec + dur + 0.5f).Timeout += () =>
			{
				if (GodotObject.IsInstanceValid(this)) QueueFree();
			};
		}

		private float GetTopOfScreenWorldY()
		{
			var vp = GetViewport();
			if (vp == null) return -1000f;

			Rect2 rectPx = vp.GetVisibleRect();
			Transform2D inv = vp.GetCanvasTransform().AffineInverse();

			Vector2 w0 = inv * rectPx.Position;
			Vector2 w1 = inv * (rectPx.Position + rectPx.Size);

			return Mathf.Min(w0.Y, w1.Y);
		}

		private Vector2 RandomInCircle(float r)
		{
			if (r <= 0f) return Vector2.Zero;
			float a = _rng.RandfRange(0f, Mathf.Tau);
			float m = Mathf.Sqrt(_rng.Randf()) * r;
			return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * m;
		}
	}
}
