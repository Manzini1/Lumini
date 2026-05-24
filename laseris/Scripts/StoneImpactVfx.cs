using Godot;
using System;

namespace Game.Vfx
{
	public partial class StoneImpactVfx : Node2D
	{
		[ExportGroup("Refs")]
		[Export] public NodePath GroundDustPath = "GroundDust";
		[Export] public NodePath DebrisPath = "Debris";
		[Export] public NodePath ShockRingPath = "ShockRing";
		[Export] public NodePath FlashPath = "Flash"; // opcional
		[Export] public NodePath SfxPath = "Sfx";     // opcional AudioStreamPlayer2D

		[ExportGroup("Timing")]
		[Export] public float RingDurationSec = 0.12f;
		[Export] public float FlashDurationSec = 0.06f;
		[Export] public float AutoFreeSec = 1.2f;

		[ExportGroup("Ring")]
		[Export] public Vector2 RingStartScale = new Vector2(0.35f, 0.35f);
		[Export] public Vector2 RingEndScale = new Vector2(1.35f, 1.35f);

		private Node _groundDust;
		private Node _debris;
		private CanvasItem _shockRing;
		private CanvasItem _flash;
		private AudioStreamPlayer2D _sfx;

		public override void _Ready()
		{
			_groundDust = GetNodeOrNull<Node>(GroundDustPath);
			_debris = GetNodeOrNull<Node>(DebrisPath);
			_shockRing = GetNodeOrNull<CanvasItem>(ShockRingPath);
			_flash = GetNodeOrNull<CanvasItem>(FlashPath);
			_sfx = GetNodeOrNull<AudioStreamPlayer2D>(SfxPath);
		}

		// Compatível com chamadas via Call("Play") sem args
		public void Play()
		{
			// Partículas (one-shot)
			SetNodeEmitting(_groundDust, true);
			SetNodeEmitting(_debris, true);

			// SFX opcional
			_sfx?.Play();

			// Shock ring
			if (_shockRing is Node2D ring2D)
			{
				ring2D.Visible = true;
				ring2D.Scale = RingStartScale;

				Color c = ring2D.Modulate;
				c.A = 1f;
				ring2D.Modulate = c;

				var tw = CreateTween();
				tw.SetParallel(true);
				tw.TweenProperty(ring2D, "scale", RingEndScale, Mathf.Max(0.01f, RingDurationSec))
				  .SetTrans(Tween.TransitionType.Quad)
				  .SetEase(Tween.EaseType.Out);
				tw.TweenProperty(ring2D, "modulate:a", 0.0f, Mathf.Max(0.01f, RingDurationSec))
				  .SetTrans(Tween.TransitionType.Quad)
				  .SetEase(Tween.EaseType.Out);
			}
			else if (_shockRing != null)
			{
				_shockRing.Visible = true;
				Color c = _shockRing.Modulate;
				c.A = 1f;
				_shockRing.Modulate = c;

				var tw = CreateTween();
				tw.TweenProperty(_shockRing, "modulate:a", 0.0f, Mathf.Max(0.01f, RingDurationSec));
			}

			// Flash curto opcional
			if (_flash != null)
			{
				_flash.Visible = true;
				Color fc = _flash.Modulate;
				fc.A = 1f;
				_flash.Modulate = fc;

				var twf = CreateTween();
				twf.TweenProperty(_flash, "modulate:a", 0.0f, Mathf.Max(0.01f, FlashDurationSec))
				   .SetTrans(Tween.TransitionType.Quad)
				   .SetEase(Tween.EaseType.Out);
			}

			// cleanup
			GetTree().CreateTimer(Mathf.Max(0.05f, AutoFreeSec)).Timeout += () =>
			{
				if (GodotObject.IsInstanceValid(this))
					QueueFree();
			};
		}

		private static void SetNodeEmitting(Node node, bool on)
		{
			if (node == null || !GodotObject.IsInstanceValid(node)) return;

			if (node is GpuParticles2D gpu)
			{
				// restart ajuda a garantir burst visível quando reutiliza cena
				if (on)
				{
					gpu.Restart();
					gpu.Emitting = true;
				}
				else gpu.Emitting = false;
				return;
			}

			if (node is CpuParticles2D cpu)
			{
				cpu.Emitting = on;
				return;
			}

			foreach (var c in node.GetChildren())
				if (c is Node child)
					SetNodeEmitting(child, on);
		}
	}
}
