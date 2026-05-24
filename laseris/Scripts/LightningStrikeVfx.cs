using Godot;
using System;

namespace Game.Vfx
{
	public partial class LightningStrikeVfx : Node2D
	{
		[ExportGroup("Refs")]
		[Export] public NodePath BoltMeshPath = "BoltMesh";
		[Export] public NodePath ImpactMeshPath = "ImpactFlash";

		[ExportGroup("Timing")]
		[Export] public float RevealSec = 0.05f;
		[Export] public float HoldSec = 0.04f;
		[Export] public float FadeOutSec = 0.10f;
		[Export] public bool QueueFreeOnEnd = true;

		[ExportGroup("Look")]
		[Export] public float BoltWidthPx = 14f;          // espessura base
		[Export] public float WidthMultiplier = 1.0f;     // para deixar mais grosso (barrage usa >1)
		[Export] public float JitterPx = 18f;             // zig-zag lateral
		[Export] public float GlowPx = 40f;               // folga pro glow não cortar
		[Export] public float Softness = 0.08f;           // borda mais dura = mais “raio”
		[Export] public float NoiseScale = 10f;
		[Export] public float NoiseSpeed = 9f;

		[ExportGroup("From Top (force style)")]
		[Export] public bool ForceFromScreenTop = true;
		[Export] public float FromXJitter = 30f;
		[Export] public float SpawnMarginTop = 30f;

		[ExportGroup("Colors")]
		[Export] public Color CoreColor = new(0.95f, 0.98f, 1.00f, 1f);
		[Export] public Color GlowColor = new(0.35f, 0.75f, 1.00f, 1f);

		private MeshInstance2D _boltMesh;
		private MeshInstance2D _impactMesh;
		private QuadMesh _boltQuad;
		private QuadMesh _impactQuad;
		private ShaderMaterial _boltMat;
		private ShaderMaterial _impactMat;
		private Tween _tw;
		private readonly RandomNumberGenerator _rng = new();

		private bool _inited;

		private const string BoltShader = @"
shader_type canvas_item;
render_mode unshaded, blend_add;

uniform float progress = 1.0;
uniform float fade = 1.0;

uniform float half_width = 0.03;      // em UV (setado pelo C#)
uniform float jitter = 0.08;          // em UV (setado pelo C#)
uniform float softness = 0.08;

uniform float noise_scale = 10.0;
uniform float noise_speed = 9.0;

uniform vec4 core_color : source_color = vec4(0.95,0.98,1.0,1.0);
uniform vec4 glow_color : source_color = vec4(0.35,0.75,1.0,1.0);

float hash(vec2 p){ return fract(sin(dot(p, vec2(127.1,311.7))) * 43758.5453123); }
float noise2(vec2 p){
	vec2 i=floor(p), f=fract(p);
	float a=hash(i), b=hash(i+vec2(1,0)), c=hash(i+vec2(0,1)), d=hash(i+vec2(1,1));
	vec2 u=f*f*(3.0-2.0*f);
	return mix(a,b,u.x) + (c-a)*u.y*(1.0-u.x) + (d-b)*u.x*u.y;
}
float fbm(vec2 p){
	float v=0.0; float a=0.55;
	for(int i=0;i<4;i++){ v += a*noise2(p); p*=2.02; a*=0.52; }
	return v;
}

void fragment(){
	vec2 uv = UV;

	// anima revelando (topo -> alvo) ao longo do X
	if (uv.x > progress) discard;

	float t = TIME * noise_speed;

	// linha central “serrilhada”
	float n = fbm(vec2(uv.x*noise_scale, t*0.22));
	float n2 = fbm(vec2(uv.x*noise_scale*2.1, -t*0.18));
	float center = 0.5 + (n - 0.5) * jitter + (n2 - 0.5) * (jitter*0.55);

	float dy = abs(uv.y - center);

	// core e glow
	float core = smoothstep(half_width, half_width - softness, dy);
	float glow = smoothstep(half_width*2.8, half_width*2.8 - softness, dy);

	float alpha = max(core, glow*0.55) * fade;

	// evita “quad fantasma”
	if (alpha < 0.002) discard;

	vec3 col = glow_color.rgb;
	col = mix(col, core_color.rgb, core);

	// flicker sutil
	float flick = 0.88 + 0.12 * sin(TIME*18.0 + uv.x*24.0 + n*8.0);
	col *= flick;

	COLOR = vec4(col, alpha);
}
";

		private const string ImpactShader = @"
shader_type canvas_item;
render_mode unshaded, blend_add;

uniform float fade = 1.0;
uniform vec4 core_color : source_color = vec4(0.95,0.98,1.0,1.0);
uniform vec4 glow_color : source_color = vec4(0.35,0.75,1.0,1.0);

void fragment(){
	vec2 uv = UV * 2.0 - 1.0; // -1..1
	float d = length(uv);

	// radial: nada de quadrado
	float glow = smoothstep(1.0, 0.0, d);
	float core = smoothstep(0.35, 0.0, d);

	float a = (glow*0.65 + core*0.85) * fade;
	if (a < 0.002) discard;

	vec3 col = glow_color.rgb * glow;
	col = mix(col, core_color.rgb, core);

	COLOR = vec4(col, a);
}
";

		public override void _Ready()
		{
			_rng.Randomize();
		}

		// ✅ compat com chamadas diretas
		public void Play(Vector2 from, Vector2 to)
		{
			Play(from, to, RevealSec);
		}

		// ✅ COMPAT com ElementVfxLibrary: Call("Play", from, to, travelSec)
		public void Play(Vector2 from, Vector2 to, float travelSec)
		{
			EnsureInit();

			Vector2 realFrom = from;
			if (ForceFromScreenTop)
			{
				float topY = GetTopOfScreenWorldY() - Mathf.Max(0f, SpawnMarginTop);
				float x = to.X + _rng.RandfRange(-FromXJitter, FromXJitter);
				realFrom = new Vector2(x, topY);
			}

			AlignBoltBetween(realFrom, to);
			PlaceImpactAt(to);

			StartTween(Mathf.Max(0.01f, travelSec), HoldSec, FadeOutSec);
		}

		private void EnsureInit()
		{
			if (_inited) return;

			_boltMesh = GetNodeOrNull<MeshInstance2D>(BoltMeshPath);
			if (_boltMesh == null)
			{
				_boltMesh = new MeshInstance2D { Name = "BoltMesh" };
				AddChild(_boltMesh);
			}

			_impactMesh = GetNodeOrNull<MeshInstance2D>(ImpactMeshPath);
			if (_impactMesh == null)
			{
				_impactMesh = new MeshInstance2D { Name = "ImpactFlash" };
				AddChild(_impactMesh);
			}

			_boltQuad = new QuadMesh();
			_impactQuad = new QuadMesh();

			_boltMesh.Mesh = _boltQuad;
			_impactMesh.Mesh = _impactQuad;

			_boltMat = new ShaderMaterial { Shader = new Shader { Code = BoltShader } };
			_impactMat = new ShaderMaterial { Shader = new Shader { Code = ImpactShader } };

			_boltMesh.Material = _boltMat;
			_impactMesh.Material = _impactMat;

			_boltMat.SetShaderParameter("core_color", CoreColor);
			_boltMat.SetShaderParameter("glow_color", GlowColor);
			_boltMat.SetShaderParameter("noise_scale", NoiseScale);
			_boltMat.SetShaderParameter("noise_speed", NoiseSpeed);
			_boltMat.SetShaderParameter("softness", Softness);

			_impactMat.SetShaderParameter("core_color", CoreColor);
			_impactMat.SetShaderParameter("glow_color", GlowColor);

			// Z default (ajuste se precisar)
			_boltMesh.ZAsRelative = false;
			_boltMesh.ZIndex = 5;
			_impactMesh.ZAsRelative = false;
			_impactMesh.ZIndex = 8;

			_inited = true;
		}

		private void AlignBoltBetween(Vector2 from, Vector2 to)
		{
			Vector2 dir = to - from;
			float len = Mathf.Max(4f, dir.Length());
			Vector2 n = dir / len;

			GlobalPosition = (from + to) * 0.5f;
			GlobalRotation = n.Angle();

			float wPx = Mathf.Max(2f, BoltWidthPx * WidthMultiplier);
			float halfW = wPx * 0.5f;

			// altura do QUAD: largura + jitter + glow (pra nunca cortar)
			float heightPx = (halfW + JitterPx + GlowPx) * 2f;
			_boltQuad.Size = new Vector2(len, heightPx);
			_boltMesh.Position = Vector2.Zero;
			_boltMesh.Rotation = 0f;

			// converte PX -> UV (relativo à altura do quad)
			float halfWidthUv = Mathf.Clamp(halfW / heightPx, 0.002f, 0.18f);
			float jitterUv = Mathf.Clamp(JitterPx / heightPx, 0.0f, 0.35f);

			_boltMat.SetShaderParameter("half_width", halfWidthUv);
			_boltMat.SetShaderParameter("jitter", jitterUv);
		}

		private void PlaceImpactAt(Vector2 atGlobal)
		{
			// impacto “local” dentro do mesmo Node2D (pra não precisar node extra)
			// converte atGlobal -> local do strike (porque o root está no meio do bolt)
			Vector2 local = ToLocal(atGlobal);

			float size = Mathf.Max(24f, (BoltWidthPx * WidthMultiplier) * 6.0f);
			_impactQuad.Size = new Vector2(size, size);

			_impactMesh.Position = local;
			_impactMesh.Rotation = 0f;
		}

		private void StartTween(float reveal, float hold, float fadeOut)
		{
			if (_tw != null && GodotObject.IsInstanceValid(_tw)) _tw.Kill();

			_boltMat.SetShaderParameter("progress", 0f);
			_boltMat.SetShaderParameter("fade", 1f);
			_impactMat.SetShaderParameter("fade", 0f);

			_tw = CreateTween();

			_tw.TweenMethod(Callable.From<float>(v => _boltMat.SetShaderParameter("progress", v)), 0f, 1f, reveal)
				.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

			// flash do impacto curto
			_tw.Parallel().TweenMethod(Callable.From<float>(v => _impactMat.SetShaderParameter("fade", v)), 0f, 1f, Mathf.Min(0.03f, reveal))
				.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

			if (hold > 0.001f)
				_tw.TweenInterval(hold);

			float fo = Mathf.Max(0.01f, fadeOut);

			_tw.Parallel().TweenMethod(Callable.From<float>(v => _boltMat.SetShaderParameter("fade", v)), 1f, 0f, fo)
				.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);

			_tw.Parallel().TweenMethod(Callable.From<float>(v => _impactMat.SetShaderParameter("fade", v)), 1f, 0f, fo)
				.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);

			if (QueueFreeOnEnd)
				_tw.Finished += () => { if (GodotObject.IsInstanceValid(this)) QueueFree(); };
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
	}
}
