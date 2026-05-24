using Godot;

namespace Game.Vfx
{
	public partial class EarthFistVfx : Node2D
	{
		[ExportGroup("Refs")]
		[Export] public NodePath FistPath = "Fist";
		[Export] public NodePath ShadowPath = "Shadow";
		[Export] public NodePath DustImpactPath = "DustImpact";
		[Export] public NodePath DebrisImpactPath = "DebrisImpact";
		[Export] public NodePath SfxWhooshPath = "SfxWhoosh";
		[Export] public NodePath SfxImpactPath = "SfxImpact";

		[ExportGroup("Motion")]
		[Export] public float SpawnHeightPx = 260f;
		[Export] public float AppearSec = 0.08f;
		[Export] public float DropSec = 0.14f;
		[Export] public float LingerSec = 0.04f;
		[Export] public float FadeOutSec = 0.10f;

		[ExportGroup("Impact Feel")]
		[Export] public float PunchDownPx = 18f;
		[Export] public float ReboundUpPx = 10f;
		[Export] public float ReboundSec = 0.10f;

		[ExportGroup("Screen Shake (light)")]
		[Export] public bool DoScreenShake = true;
		[Export] public float ShakeAmpPx = 5f;
		[Export] public float ShakeDurSec = 0.08f;

		[ExportGroup("Z")]
		[Export] public bool ForceZ = true;
		[Export] public int FistZ = 30;
		[Export] public int ShadowZ = -5;
		[Export] public int ImpactZ = 35;

		[ExportGroup("Impact Safety (prewarm fix)")]
		[Export] public bool HideImpactNodesUntilImpact = true;
		[Export] public bool ForcePreprocessZero = true; // ✅ mata “prewarm” no runtime

		private CanvasItem _fist;
		private CanvasItem _shadow;
		private Node _dust;
		private Node _debris;
		private AudioStreamPlayer2D _whoosh;
		private AudioStreamPlayer2D _impact;
		private Tween _tw;

		public override void _EnterTree()
		{
			CacheRefsEarly();
			ApplyZ();
			HardResetImpactVfx(); // ✅ antes do _Ready (evita 1 frame de emissão)
		}

		public override void _Ready()
		{
			CacheRefsEarly();
			ApplyZ();
			HardResetImpactVfx();
		}

		private void CacheRefsEarly()
		{
			_fist ??= GetNodeOrNull<CanvasItem>(FistPath);
			_shadow ??= GetNodeOrNull<CanvasItem>(ShadowPath);
			_dust ??= GetNodeOrNull<Node>(DustImpactPath);
			_debris ??= GetNodeOrNull<Node>(DebrisImpactPath);
			_whoosh ??= GetNodeOrNull<AudioStreamPlayer2D>(SfxWhooshPath);
			_impact ??= GetNodeOrNull<AudioStreamPlayer2D>(SfxImpactPath);
		}

		// Compatível com ElementVfxLibrary
		public void Play(Vector2 from, Vector2 to, float travelSec = 0.06f)
		{
			_tw?.Kill();

			Vector2 target = to;
			Vector2 start = target + new Vector2(0f, -SpawnHeightPx);

			GlobalPosition = start;

			ApplyZ();

			// reset visuals
			SetAlpha(_fist, 0f);
			SetAlpha(_shadow, 0f);

			if (_fist is Node2D fist2D)
				fist2D.Scale = Vector2.One * 0.92f;

			if (_shadow is Node2D shadow2D)
				shadow2D.Scale = new Vector2(0.35f, 0.35f);

			HardResetImpactVfx();

			_whoosh?.Play();

			_tw = CreateTween();
			_tw.SetTrans(Tween.TransitionType.Quad);
			_tw.SetEase(Tween.EaseType.Out);

			// Aparecer + sombra
			_tw.TweenProperty(_fist, "modulate:a", 1f, Mathf.Max(0.01f, AppearSec));
			_tw.Parallel().TweenProperty(_shadow, "modulate:a", 0.55f, Mathf.Max(0.01f, AppearSec));

			if (_shadow is Node2D s2)
				_tw.Parallel().TweenProperty(s2, "scale", new Vector2(1.05f, 1.05f), Mathf.Max(0.01f, DropSec));

			// Cair
			_tw.TweenProperty(this, "global_position", target + new Vector2(0f, PunchDownPx), Mathf.Max(0.01f, DropSec));

			// Impacto
			_tw.TweenCallback(Callable.From(() => OnImpact(target)));

			// Rebound
			_tw.TweenProperty(this, "global_position", target + new Vector2(0f, -ReboundUpPx), Mathf.Max(0.01f, ReboundSec))
			   .SetTrans(Tween.TransitionType.Back)
			   .SetEase(Tween.EaseType.Out);

			_tw.TweenProperty(this, "global_position", target, Mathf.Max(0.01f, ReboundSec * 0.55f))
			   .SetTrans(Tween.TransitionType.Quad)
			   .SetEase(Tween.EaseType.Out);

			if (LingerSec > 0.001f)
				_tw.TweenInterval(LingerSec);

			// Fade out punho/sombra
			_tw.TweenProperty(_fist, "modulate:a", 0f, Mathf.Max(0.01f, FadeOutSec));
			_tw.Parallel().TweenProperty(_shadow, "modulate:a", 0f, Mathf.Max(0.01f, FadeOutSec));

			_tw.Finished += () =>
			{
				if (GodotObject.IsInstanceValid(this))
					QueueFree();
			};
		}

		private void OnImpact(Vector2 at)
		{
			ApplyZ();

			_impact?.Play();

			if (HideImpactNodesUntilImpact)
			{
				SetVisibleRecursive(_dust, true);
				SetVisibleRecursive(_debris, true);
			}

			SetNodeEmitting(_dust, true);
			SetNodeEmitting(_debris, true);

			if (DoScreenShake)
			{
				var cam = GetViewport()?.GetCamera2D();
				if (cam != null && cam.HasMethod("Shake"))
					cam.Call("Shake", ShakeAmpPx, ShakeDurSec);
			}
		}

		private void HardResetImpactVfx()
		{
			SetNodeEmitting(_dust, false);
			SetNodeEmitting(_debris, false);

			ForceParticlesSafeDefaults(_dust);
			ForceParticlesSafeDefaults(_debris);

			if (HideImpactNodesUntilImpact)
			{
				SetVisibleRecursive(_dust, false);
				SetVisibleRecursive(_debris, false);
			}
		}

		private void ForceParticlesSafeDefaults(Node node)
		{
			if (node == null || !GodotObject.IsInstanceValid(node)) return;

			if (node is GpuParticles2D gpu)
			{
				gpu.Emitting = false;

				// ✅ evita prewarm do próprio node
				if (ForcePreprocessZero)
					gpu.Preprocess = 0f;

				// garante burst limpo quando ligar
				gpu.Restart();
				return;
			}

			if (node is CpuParticles2D cpu)
			{
				cpu.Emitting = false;
				cpu.Restart();
				return;
			}

			foreach (var c in node.GetChildren())
				if (c is Node child)
					ForceParticlesSafeDefaults(child);
		}

		private void ApplyZ()
		{
			if (!ForceZ) return;

			if (_fist != null)
			{
				_fist.ZAsRelative = false;
				_fist.ZIndex = FistZ;
			}
			if (_shadow != null)
			{
				_shadow.ZAsRelative = false;
				_shadow.ZIndex = ShadowZ;
			}
			ApplyZRecursive(_dust, ImpactZ);
			ApplyZRecursive(_debris, ImpactZ);
		}

		private static void ApplyZRecursive(Node node, int z)
		{
			if (node == null || !GodotObject.IsInstanceValid(node)) return;

			if (node is CanvasItem ci)
			{
				ci.ZAsRelative = false;
				ci.ZIndex = z;
			}

			foreach (var c in node.GetChildren())
				if (c is Node child)
					ApplyZRecursive(child, z);
		}

		private static void SetAlpha(CanvasItem ci, float a)
		{
			if (ci == null || !GodotObject.IsInstanceValid(ci)) return;
			var m = ci.Modulate;
			m.A = a;
			ci.Modulate = m;
		}

		private static void SetVisibleRecursive(Node node, bool visible)
		{
			if (node == null || !GodotObject.IsInstanceValid(node)) return;

			if (node is CanvasItem ci)
				ci.Visible = visible;

			foreach (var c in node.GetChildren())
				if (c is Node child)
					SetVisibleRecursive(child, visible);
		}

		private static void SetNodeEmitting(Node node, bool on)
		{
			if (node == null || !GodotObject.IsInstanceValid(node)) return;

			if (node is GpuParticles2D gpu)
			{
				if (on) gpu.Restart();
				gpu.Emitting = on;
				return;
			}
			if (node is CpuParticles2D cpu)
			{
				if (on) cpu.Restart();
				cpu.Emitting = on;
				return;
			}

			foreach (var c in node.GetChildren())
				if (c is Node child)
					SetNodeEmitting(child, on);
		}
	}
}
