using Godot;
using System;
using System.Collections.Generic;

namespace Game.Vfx
{
	public partial class WindBarrageController : Node2D
	{
		[Signal] public delegate void SlashHitEventHandler(int slashIndex, Vector2 atGlobal);

		[ExportGroup("Scenes")]
		[Export] public PackedScene SlashScene;          // AirSlashProjectileVfx.tscn
		[Export] public PackedScene FinalBurstScene;     // opcional (impacto final)

		[ExportGroup("Count / Timing")]
		[Export] public int SlashCount = 8;
		[Export] public float FirstDelaySec = 0.05f;
		[Export] public float TotalDurationSec = 0.55f;
		[Export] public float SlashTravelSec = 0.16f;
		[Export] public float FinalBurstDelaySec = 0.62f;

		[ExportGroup("Spawn Shape")]
		[Export] public float RadiusX = 140f;
		[Export] public float RadiusY = 90f;
		[Export] public float OvershootPx = 80f;   // passa do centro e vai além
		[Export] public float AngleJitterDeg = 10f;

		[ExportGroup("Feel")]
		[Export] public bool AlternateClockwise = true;
		[Export] public bool DebugLogs = false;

		private readonly RandomNumberGenerator _rng = new();

		public override void _EnterTree()
		{
			AddToGroup("vfx_wind_barrage");
		}

		public override void _Ready()
		{
			_rng.Randomize();
		}

		// Compatível com ElementVfxLibrary
		public void Play(Vector2 from, Vector2 to, float travelSec = -1f)
		{
			if (SlashScene == null)
				return;

			int count = Mathf.Max(1, SlashCount);
			float total = Mathf.Max(0.01f, TotalDurationSec);
			float every = total / count;
			float slashTravel = travelSec > 0f ? travelSec : SlashTravelSec;

			for (int i = 0; i < count; i++)
			{
				int localIndex = i;
				float delay = Mathf.Max(0f, FirstDelaySec + i * every);

				GetTree().CreateTimer(delay).Timeout += () =>
				{
					if (!GodotObject.IsInstanceValid(this))
						return;

					SpawnOneSlash(localIndex, count, to, slashTravel);
				};
			}

			// burst final
			if (FinalBurstScene != null)
			{
				GetTree().CreateTimer(Mathf.Max(FinalBurstDelaySec, FirstDelaySec + total)).Timeout += () =>
				{
					if (!GodotObject.IsInstanceValid(this))
						return;

					var raw = FinalBurstScene.Instantiate();
					if (raw is not Node node)
					{
						raw.QueueFree();
						return;
					}

					AddChild(node);

					if (node is Node2D n2)
						n2.GlobalPosition = to;

					TryAutoPlayInHierarchy(node);
				};
			}

			// cleanup
			GetTree().CreateTimer(Mathf.Max(1.2f, FinalBurstDelaySec + 0.6f)).Timeout += () =>
			{
				if (GodotObject.IsInstanceValid(this))
					QueueFree();
			};
		}

		private void SpawnOneSlash(int index, int total, Vector2 center, float travelSec)
		{
			var raw = SlashScene.Instantiate();
			if (raw is not Node node)
			{
				raw.QueueFree();
				return;
			}

			AddChild(node);

			float t = total == 1 ? 0f : (index / (float)total) * Mathf.Tau;

			if (AlternateClockwise && (index % 2 == 1))
				t = Mathf.Tau - t;

			t += Mathf.DegToRad(_rng.RandfRange(-AngleJitterDeg, AngleJitterDeg));

			Vector2 ring = new Vector2(Mathf.Cos(t) * RadiusX, Mathf.Sin(t) * RadiusY);

			Vector2 from = center + ring;
			Vector2 dir = (center - from).Normalized();
			Vector2 to = center + dir * OvershootPx;

			// tenta chamar Play(from,to,travel)
			if (node.HasMethod("Play"))
			{
				node.Call("Play", from, to, travelSec);
			}
			else
			{
				// fallback simples
				if (node is Node2D n2)
					n2.GlobalPosition = from;
			}

			EmitSignal(SignalName.SlashHit, index, center);

			if (DebugLogs)
				GD.Print($"[WindBarrage] slash {index} from={from} to={to}");
		}

		private static bool TryAutoPlayInHierarchy(Node node)
		{
			if (node == null) return false;

			if (node is AnimatedSprite2D asp)
			{
				if (!asp.IsPlaying()) asp.Play();
				return true;
			}

			if (node is GpuParticles2D gpu)
			{
				gpu.Restart();
				gpu.Emitting = true;
				return true;
			}

			if (node is CpuParticles2D cpu)
			{
				cpu.Emitting = true;
				return true;
			}

			if (node is AnimationPlayer ap)
			{
				var list = ap.GetAnimationList();
				if (list != null && list.Length > 0)
				{
					ap.Play(list[0]);
					return true;
				}
			}

			foreach (var c in node.GetChildren())
			{
				if (c is Node child && TryAutoPlayInHierarchy(child))
					return true;
			}

			return false;
		}
	}
}
