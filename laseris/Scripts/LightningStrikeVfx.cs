using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;

namespace Game.Vfx
{
	public partial class LightningStrikeVfx : Node2D
	{
		[ExportGroup("Refs")]
		[Export] public NodePath GlowLinePath = "BoltGlow";
		[Export] public NodePath MidLinePath  = "BoltMid";
		[Export] public NodePath CoreLinePath = "BoltCore";
		[Export] public NodePath ImpactBurstPath = "ImpactBurst";
		[Export] public NodePath TopSparksPath   = "TopSparks";
		[Export] public NodePath FlashPath       = "Flash"; // pode ser Control ou Node2D

		[ExportGroup("Timing")]
		[Export] public float TelegraphSec = 0.14f;
		[Export] public float StrikeHoldSec = 0.05f;
		[Export] public float FadeOutSec = 0.10f;
		[Export] public int FlickerFrames = 3;
		[Export] public float FlickerInterval = 0.018f;
		[Export] public bool QueueFreeOnEnd = true;

		[ExportGroup("Shape")]
		[Export] public int Iterations = 5;     // 4..6
		[Export] public float JitterPx = 120f;  // amplitude inicial
		[Export] public float JitterDecay = 0.55f;
		[Export] public float EndJitterBias = 0.30f; // mais caos perto do alvo
		[Export] public float PerpNoise = 1.0f;

		[ExportGroup("Visual")]
		[Export] public float GlowAlpha = 0.35f;
		[Export] public float MidAlpha  = 0.65f;
		[Export] public float CoreAlpha = 1.00f;

		[ExportGroup("Z Layers")]
		[Export] public bool ForceZLayers = true;
		[Export] public int BoltZIndex = -20;
		[Export] public int ImpactZIndex = 20;

		private Line2D _glow;
		private Line2D _mid;
		private Line2D _core;

		private GpuParticles2D _impact;
		private GpuParticles2D _topSparks;

		private Node _flashNode;
		private CanvasItem _flashItem;

		private Tween _tw;
		private readonly RandomNumberGenerator _rng = new();

		private Vector2 _from;
		private Vector2 _to;

		public override void _Ready()
		{
			_rng.Randomize();

			_glow = GetNodeOrNull<Line2D>(GlowLinePath);
			_mid  = GetNodeOrNull<Line2D>(MidLinePath);
			_core = GetNodeOrNull<Line2D>(CoreLinePath);

			_impact = GetNodeOrNull<GpuParticles2D>(ImpactBurstPath);
			_topSparks = GetNodeOrNull<GpuParticles2D>(TopSparksPath);

			_flashNode = GetNodeOrNull<Node>(FlashPath);
			_flashItem = _flashNode as CanvasItem;

			TopLevel = true;

			ApplyZ();
			SetVisibleAll(false);
		}

		private void ApplyZ()
		{
			if (!ForceZLayers) return;

			ApplyZOne(_glow, BoltZIndex);
			ApplyZOne(_mid,  BoltZIndex);
			ApplyZOne(_core, BoltZIndex);

			ApplyZOne(_impact, ImpactZIndex);
			ApplyZOne(_topSparks, ImpactZIndex);

			// Flash normalmente por cima do impacto
			ApplyZOne(_flashItem, ImpactZIndex + 5);
		}

		private void ApplyZOne(CanvasItem ci, int z)
		{
			if (ci == null) return;
			ci.ZAsRelative = false;
			ci.ZIndex = z;
		}

		public void Play(Vector2 fromGlobal, Vector2 toGlobal)
		{
			_from = fromGlobal;
			_to = toGlobal;

			_tw?.Kill();
			SetVisibleAll(true);

			// 1) Telegraph fraquinho (linha reta)
			DrawTelegraphLine();

			float tele = Mathf.Max(0f, TelegraphSec);
			GetTree().CreateTimer(Mathf.Max(0.001f, tele)).Timeout += () =>
			{
				if (!GodotObject.IsInstanceValid(this)) return;
				StrikeNowWithFlicker();
			};
		}

		private void DrawTelegraphLine()
		{
			var pts = new List<Vector2> { _from, _to };
			ApplyToLines(pts, alphaMul: 0.25f);
			StopParticles();
			SetFlash(0f);
		}

		private void StrikeNowWithFlicker()
		{
			// impacto
			if (_impact != null)
			{
				_impact.GlobalPosition = _to;
				_impact.Emitting = true;
			}

			// sparks no topo
			if (_topSparks != null)
			{
				_topSparks.GlobalPosition = _from;
				_topSparks.Emitting = true;
			}

			// flash curtinho no alvo
			SetFlash(1f);
			GetTree().CreateTimer(0.04f).Timeout += () =>
			{
				if (GodotObject.IsInstanceValid(this)) SetFlash(0f);
			};

			// primeira geração
			var mainPts = BuildBoltPoints(_from, _to);
			ApplyToLines(mainPts, alphaMul: 1.0f);

			// flicker: regenera algumas vezes
			int flick = Mathf.Max(0, FlickerFrames);
			for (int i = 1; i < flick; i++)
			{
				float d = i * Mathf.Max(0.001f, FlickerInterval);
				GetTree().CreateTimer(d).Timeout += () =>
				{
					if (!GodotObject.IsInstanceValid(this)) return;
					var pts = BuildBoltPoints(_from, _to);
					ApplyToLines(pts, alphaMul: 1.0f);
				};
			}

			// hold + fade
			float hold = Mathf.Max(0.0f, StrikeHoldSec);
			GetTree().CreateTimer(hold).Timeout += () =>
			{
				if (!GodotObject.IsInstanceValid(this)) return;
				StartFadeOut();
			};
		}

		private void StartFadeOut()
		{
			_tw?.Kill();
			_tw = CreateTween();

			float fo = Mathf.Max(0.01f, FadeOutSec);

			FadeLine(_glow, fo);
			FadeLine(_mid,  fo);
			FadeLine(_core, fo);

			if (_flashItem != null)
				_tw.Parallel().TweenProperty(_flashItem, "modulate:a", 0f, fo);

			if (QueueFreeOnEnd)
				_tw.Finished += () =>
				{
					if (GodotObject.IsInstanceValid(this)) QueueFree();
				};
		}

		private void FadeLine(Line2D line, float fo)
		{
			if (line == null) return;
			float a0 = line.Modulate.A;
			_tw.Parallel().TweenProperty(line, "modulate:a", 0f, fo).From(a0);
		}

		private void StopParticles()
		{
			if (_impact != null) _impact.Emitting = false;
			if (_topSparks != null) _topSparks.Emitting = false;
		}

		private void SetFlash(float a)
		{
			if (_flashItem == null) return;

			var m = _flashItem.Modulate;
			m.A = Mathf.Clamp(a, 0f, 1f);
			_flashItem.Modulate = m;
			_flashItem.Visible = m.A > 0.001f;

			// ✅ posiciona no alvo de forma compatível com Control ou Node2D
			if (_flashNode is Node2D n2) n2.GlobalPosition = _to;
			else if (_flashNode is Control c) c.GlobalPosition = _to;
		}

		private void SetVisibleAll(bool v)
		{
			if (_glow != null) _glow.Visible = v;
			if (_mid  != null) _mid.Visible  = v;
			if (_core != null) _core.Visible = v;

			if (_impact != null) _impact.Visible = v;
			if (_topSparks != null) _topSparks.Visible = v;

			if (_flashItem != null) _flashItem.Visible = v;
		}

		private void ApplyToLines(List<Vector2> globalPts, float alphaMul)
{
	if (globalPts == null || globalPts.Count < 2) return;

	void SetPoints(Line2D line, float a)
	{
		if (line == null) return;

		line.ClearPoints();
		for (int i = 0; i < globalPts.Count; i++)
			line.AddPoint(line.ToLocal(globalPts[i]));

		var m = line.Modulate;
		m.A = Mathf.Clamp(a, 0f, 1f);
		line.Modulate = m;
	}

	SetPoints(_glow, GlowAlpha * alphaMul);
	SetPoints(_mid,  MidAlpha  * alphaMul);
	SetPoints(_core, CoreAlpha * alphaMul);
}

		private void SetAlpha(CanvasItem ci, float a)
		{
			if (ci == null) return;
			var m = ci.Modulate;
			m.A = Mathf.Clamp(a, 0f, 1f);
			ci.Modulate = m;
		}

		private List<Vector2> BuildBoltPoints(Vector2 a, Vector2 b)
		{
			var pts = new List<Vector2> { a, b };

			Vector2 dir = (b - a);
			float len = Mathf.Max(1f, dir.Length());
			Vector2 n = dir / len;
			Vector2 perp = new Vector2(-n.Y, n.X) * PerpNoise;

			float amp = Mathf.Max(0f, JitterPx);

			int iters = Mathf.Max(1, Iterations);
			for (int it = 0; it < iters; it++)
			{
				var next = new List<Vector2>(pts.Count * 2);

				for (int i = 0; i < pts.Count - 1; i++)
				{
					Vector2 p0 = pts[i];
					Vector2 p1 = pts[i + 1];
					next.Add(p0);

					Vector2 mid = (p0 + p1) * 0.5f;

					// bias pra ficar mais “furioso” perto do alvo
					float t = (i + 0.5f) / Mathf.Max(1f, (pts.Count - 1f));
					float endBias = 1f + EndJitterBias * t;

					float s = _rng.RandfRange(-1f, 1f) * amp * endBias;
					mid += perp * s;

					next.Add(mid);
				}

				next.Add(pts[pts.Count - 1]);
				pts = next;

				amp *= Mathf.Clamp(JitterDecay, 0.1f, 0.95f);
			}

			pts[0] = a;
			pts[pts.Count - 1] = b;
			return pts;
		}
	}
}
