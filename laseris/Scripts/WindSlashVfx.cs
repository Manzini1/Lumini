using Godot;

namespace Game.Vfx
{
	public partial class WindSlashVfx : Node2D
	{
		[ExportGroup("Refs")]
		[Export] public NodePath DistortMeshPath = "DistortMesh";
		[Export] public NodePath GlowMeshPath = "GlowMesh";
		[Export] public NodePath DebrisPath = "Debris";
		[Export] public NodePath SfxWhooshPath = "SfxWhoosh";
		[Export] public NodePath SfxImpactPath = "SfxImpact";

		[ExportGroup("Sizing (px)")]
		[Export] public float HeightPx = 140f;           // “altura” do quad
		[Export] public float LengthPxBase = 220f;       // comprimento visual do slash
		[Export] public float LengthPxPer100px = 55f;    // cresce um pouco com a distância
		[Export] public float ExtraLengthPx = 0f;

		[ExportGroup("Motion")]
		[Export] public float DefaultTravelSec = 0.10f;
		[Export] public float ArcHeightPx = 24f;         // arco do caminho (trajetória)
		[Export] public bool RotateToDirection = true;

		[ExportGroup("Reveal/End")]
		[Export] public float RevealSec = 0.06f;         // “desenha” o slash (progress 0->1)
		[Export] public float LingerSec = 0.03f;
		[Export] public float FadeOutSec = 0.10f;
		[Export] public bool QueueFreeOnEnd = true;

		[ExportGroup("Shader params (tweak)")]
		[Export] public float DistortStrength = 0.010f;
		[Export] public float NoiseScale = 10f;
		[Export] public float NoiseSpeed = 3.5f;
		[Export] public float ArcCenterX = -0.20f;
		[Export] public float OuterR = 1.05f;
		[Export] public float Thickness = 0.28f;
		[Export] public float EdgeSoft = 0.10f;
		[Export] public float Curve = 0.18f;

		[ExportGroup("Z")]
		[Export] public bool ForceZ = true;
		[Export] public int DistortZ = 10;
		[Export] public int GlowZ = 11;
		[Export] public int DebrisZ = 12;

		private MeshInstance2D _distortMesh;
		private MeshInstance2D _glowMesh;
		private QuadMesh _quadD;
		private QuadMesh _quadG;
		private ShaderMaterial _matD;
		private ShaderMaterial _matG;

		private GpuParticles2D _debris;
		private AudioStreamPlayer2D _whoosh;
		private AudioStreamPlayer2D _impact;

		private Tween _tw;

		private Vector2 _p0, _p1, _p2;
		private float _travel;

		public override void _Ready()
		{
			_distortMesh = GetNodeOrNull<MeshInstance2D>(DistortMeshPath);
			_glowMesh = GetNodeOrNull<MeshInstance2D>(GlowMeshPath);
			_debris = GetNodeOrNull<GpuParticles2D>(DebrisPath);
			_whoosh = GetNodeOrNull<AudioStreamPlayer2D>(SfxWhooshPath);
			_impact = GetNodeOrNull<AudioStreamPlayer2D>(SfxImpactPath);

			EnsureInit();
			ApplyZ();
		}

		// ElementVfxLibrary chama Play(from,to,sec)
		public void Play(Vector2 from, Vector2 to, float travelSec = -1f)
		{
			EnsureInit();
			ApplyZ();

			_tw?.Kill();

			_travel = (travelSec > 0f) ? travelSec : DefaultTravelSec;

			// define tamanho baseado na distância (fica “lendo” melhor)
			float dist = from.DistanceTo(to);
			float lenPx = LengthPxBase + (dist / 100f) * LengthPxPer100px + ExtraLengthPx;
			lenPx = Mathf.Max(80f, lenPx);

			SetQuadSize(lenPx, Mathf.Max(30f, HeightPx));

			// trajetória em arco
			_p0 = from;
			_p2 = to;

			Vector2 dir = (to - from);
			if (dir.Length() < 0.001f) dir = Vector2.Right;
			Vector2 n = dir.Normalized();

			Vector2 perp = new Vector2(-n.Y, n.X);
			_p1 = (from + to) * 0.5f + perp * ArcHeightPx;

			GlobalPosition = from;
			if (RotateToDirection)
				GlobalRotation = n.Angle();

			// reset shader
			SetParam(_matD, "progress", 0f);
			SetParam(_matG, "progress", 0f);
			SetParam(_matD, "fade", 1f);
			SetParam(_matG, "fade", 1f);

			// debris off until impact
			if (_debris != null)
			{
				_debris.Emitting = false;
			}

			_whoosh?.Play();

			_tw = CreateTween();
			_tw.SetTrans(Tween.TransitionType.Quad);
			_tw.SetEase(Tween.EaseType.Out);

			// reveal rápido
			float rsec = Mathf.Max(0.01f, RevealSec);
			_tw.TweenMethod(Callable.From<float>((v) =>
			{
				SetParam(_matD, "progress", v);
				SetParam(_matG, "progress", v);
			}), 0f, 1f, rsec);

			// move do from->to (Bezier)
			_tw.Parallel().TweenMethod(Callable.From<float>(SetBezierPos), 0f, 1f, Mathf.Max(0.01f, _travel));

			// impacto (no “chegar”)
			_tw.TweenCallback(Callable.From(OnImpact));

			if (LingerSec > 0.001f)
				_tw.TweenInterval(LingerSec);

			float fsec = Mathf.Max(0.01f, FadeOutSec);
			_tw.TweenMethod(Callable.From<float>((v) =>
			{
				SetParam(_matD, "fade", v);
				SetParam(_matG, "fade", v);
			}), 1f, 0f, fsec);

			if (QueueFreeOnEnd)
				_tw.Finished += () =>
				{
					if (GodotObject.IsInstanceValid(this)) QueueFree();
				};
		}

		// fallback caso tua lib chame Launch
		public void Launch(Vector2 from, Vector2 to, float travelSec = -1f) => Play(from, to, travelSec);

		private void SetBezierPos(float t)
		{
			Vector2 a = _p0.Lerp(_p1, t);
			Vector2 b = _p1.Lerp(_p2, t);
			GlobalPosition = a.Lerp(b, t);
		}

		private void OnImpact()
		{
			_impact?.Play();

			if (_debris != null)
			{
				_debris.GlobalPosition = _p2;
				_debris.Restart();
				_debris.Emitting = true;
			}
		}

		private void EnsureInit()
		{
			// cria nodes se não existirem
			if (_distortMesh == null)
			{
				_distortMesh = new MeshInstance2D { Name = "DistortMesh" };
				AddChild(_distortMesh);
			}
			if (_glowMesh == null)
			{
				_glowMesh = new MeshInstance2D { Name = "GlowMesh" };
				AddChild(_glowMesh);
			}

			// QuadMesh (duplicado pra não compartilhar estado)
			_quadD = (_distortMesh.Mesh as QuadMesh)?.Duplicate(true) as QuadMesh ?? new QuadMesh();
			_quadG = (_glowMesh.Mesh as QuadMesh)?.Duplicate(true) as QuadMesh ?? new QuadMesh();

			_distortMesh.Mesh = _quadD;
			_glowMesh.Mesh = _quadG;

			// Materiais (duplicados)
			_matD = (_distortMesh.Material as ShaderMaterial)?.Duplicate(true) as ShaderMaterial ?? new ShaderMaterial();
			_matG = (_glowMesh.Material as ShaderMaterial)?.Duplicate(true) as ShaderMaterial ?? new ShaderMaterial();

			// carrega shaders dos arquivos (melhor que string hardcoded)
			if (_matD.Shader == null)
				_matD.Shader = GD.Load<Shader>("res://Shaders/wind_slash_distort.gdshader");
			if (_matG.Shader == null)
				_matG.Shader = GD.Load<Shader>("res://Shaders/wind_slash_glow.gdshader");

			_distortMesh.Material = _matD;
			_glowMesh.Material = _matG;

			// aplica parâmetros base
			ApplyShaderParams();
		}

		private void ApplyShaderParams()
		{
			SetParam(_matD, "distort_strength", DistortStrength);
			SetParam(_matD, "noise_scale", NoiseScale);
			SetParam(_matD, "noise_speed", NoiseSpeed);

			SetParam(_matD, "arc_center_x", ArcCenterX);
			SetParam(_matD, "outer_r", OuterR);
			SetParam(_matD, "thickness", Thickness);
			SetParam(_matD, "edge_soft", EdgeSoft);
			SetParam(_matD, "curve", Curve);

			SetParam(_matG, "arc_center_x", ArcCenterX);
			SetParam(_matG, "outer_r", OuterR);
			SetParam(_matG, "thickness", Mathf.Max(0.10f, Thickness * 0.78f));
			SetParam(_matG, "edge_soft", EdgeSoft);
			SetParam(_matG, "curve", Curve);
		}

		private void SetQuadSize(float lenPx, float heightPx)
		{
			_quadD.Size = new Vector2(lenPx, heightPx);
			_quadG.Size = new Vector2(lenPx, heightPx);

			// centraliza os quads no root
			_distortMesh.Position = Vector2.Zero;
			_glowMesh.Position = Vector2.Zero;
		}

		private void ApplyZ()
		{
			if (!ForceZ) return;

			_distortMesh.ZAsRelative = false;
			_glowMesh.ZAsRelative = false;
			_distortMesh.ZIndex = DistortZ;
			_glowMesh.ZIndex = GlowZ;

			if (_debris != null)
			{
				_debris.ZAsRelative = false;
				_debris.ZIndex = DebrisZ;
			}
		}

		private static void SetParam(ShaderMaterial mat, string name, Variant v)
		{
			if (mat == null) return;
			mat.SetShaderParameter(name, v);
		}
	}
}
