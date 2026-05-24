using Godot;
using System;

namespace Game.Vfx
{
	public partial class LightningBarrageController : Node2D
	{
		[ExportGroup("Scenes")]
		[Export] public PackedScene StrikeScene; // LightningStrikeVfx.tscn

		[Signal] public delegate void StrikeHitEventHandler(int strikeIndex, Vector2 atGlobal);

		[ExportGroup("Counts/Timing")]
		[Export] public int Strikes = 16;
		[Export] public float DurationSec = 0.80f;
		[Export] public float FirstDelaySec = 0.10f;

		// tempo do strike até “bater” (se Play(..., travelSec) vier do VfxLibrary, ele vai sobrescrever)
		[Export] public float StrikeTravelSec = 0.06f;

		[ExportGroup("Look")]
		[Export] public float StrikeWidthMultiplier = 1.0f;

		[ExportGroup("Spread")]
		[Export] public float SpreadX = 140f;     // espalha na horizontal ao redor do alvo
		[Export] public float TargetRadius = 40f; // jitter no alvo
		[Export] public float SpawnMarginTop = 30f;

		[ExportGroup("Debug")]
		[Export] public bool DebugLogs = false;

		private readonly RandomNumberGenerator _rng = new();

		public override void _EnterTree()
		{
			AddToGroup("vfx_lightning_barrage");
		}

		public override void _Ready()
		{
			_rng.Randomize();
		}

		// ✅ assinatura compatível com ElementVfxLibrary.SpawnPlayerCast (CALL "Play", from, to, travelSec)
		public void Play(Vector2 from, Vector2 to, float travelSec)
		{
			// o barrage não usa o "from" (vem do topo da tela), mas usamos o "to"
			StrikeTravelSec = Mathf.Max(0.01f, travelSec);
			Play(to);
		}

		// ✅ fallback (se você chamar direto de algum lugar)
		public void Play(Vector2 targetGlobal)
		{
			if (StrikeScene == null) return;

			int n = Mathf.Max(1, Strikes);
			float dur = Mathf.Max(0.01f, DurationSec);
			float every = dur / n;

			float topY = GetTopOfScreenWorldY() - Mathf.Max(0f, SpawnMarginTop);
			float travel = Mathf.Max(0.01f, StrikeTravelSec);

			for (int i = 0; i < n; i++)
			{
				int idx = i;
				float delay = Mathf.Max(0f, FirstDelaySec + idx * every);

				GetTree().CreateTimer(delay).Timeout += () =>
				{
					if (!GodotObject.IsInstanceValid(this)) return;

					float x = targetGlobal.X + _rng.RandfRange(-SpreadX, SpreadX);
					Vector2 strikeFrom = new Vector2(x, topY);
					Vector2 strikeTo = targetGlobal + RandomInCircle(TargetRadius);

					var raw = StrikeScene.Instantiate();
					if (raw is not LightningStrikeVfx strike)
					{
						raw.QueueFree();
						return;
					}

					AddChild(strike);

					// se seu LightningStrikeVfx tiver esse campo (recomendado)
					strike.WidthMultiplier = StrikeWidthMultiplier;

					// ⚠️ IMPORTANTE: seu LightningStrikeVfx também precisa ter Play(from,to,travelSec)
					strike.Play(strikeFrom, strikeTo, travel);

					// evento de “hit” sincronizado com o travel
					GetTree().CreateTimer(travel).Timeout += () =>
					{
						if (!GodotObject.IsInstanceValid(this)) return;
						EmitSignal(SignalName.StrikeHit, idx, strikeTo);
					};

					if (DebugLogs)
						GD.Print($"[LightningBarrage] #{idx} {strikeFrom} -> {strikeTo}");
				};
			}

			// autocleanup
			GetTree().CreateTimer(FirstDelaySec + dur + travel + 0.5f).Timeout += () =>
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
